// Assets/Scripts/Data/Expression.cs
//
// HypnicEmpire — Expression engine (engine-free core)
// ---------------------------------------------------
// A tiny, safe, calculator-grade expression language used inside ComputedValue
// (see ComputedValue.cs). It has NO UnityEngine / UnityEditor dependency so the
// runtime, the data validator, and the Incremental Path Visualizer can all share
// one implementation, and so it can be ported to a non-Unity system by
// translating this one file plus the grammar in the design doc.
//
// Grammar (precedence low -> high):
//   expr    := ternary
//   ternary := or ( "?" expr ":" expr )?
//   or      := and ( "||" and )*
//   and     := cmp ( "&&" cmp )*
//   cmp     := sum ( ("=="|"!="|"<"|"<="|">"|">=") sum )*
//   sum     := product ( ("+"|"-") product )*
//   product := unary ( ("*"|"/"|"%") unary )*
//   unary   := ("-"|"!") unary | power
//   power   := primary ( "^" unary )?        // right-associative; ^ binds tighter than unary minus
//   primary := number | ident | call | accessor | "(" expr ")"
//   call    := ident "(" ( expr ("," expr)* )? ")"
//   accessor:= ("av"|"res"|"owned") "(" (ident|string) ")"
//
// Booleans are doubles: comparisons/logical ops yield 1.0 (true) or 0.0 (false);
// any non-zero operand is "true". Evaluation is deterministic: invariant number
// parsing, IEEE double math, no random/time/IO/side-effects.
//
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HypnicEmpire
{
    /// <summary>Read-only evaluation context: supplies variables and state accessors.</summary>
    public interface IExpressionContext
    {
        bool TryGetVariable(string name, out double value);
        // kind is "av" | "res" | "owned"; name is the identifier inside the accessor call.
        double ResolveAccessor(string kind, string name);
    }

    /// <summary>Convenience context: a variable dictionary plus optional accessor delegates.</summary>
    public sealed class ExpressionContext : IExpressionContext
    {
        public readonly Dictionary<string, double> Variables = new();
        public Func<string, double> Av;      // alterable value by name
        public Func<string, double> Res;     // current resource amount by name
        public Func<string, double> Owned;   // building/entity count by id

        public ExpressionContext() { }
        public ExpressionContext(params (string, double)[] vars)
        { foreach (var (k, v) in vars) Variables[k] = v; }

        public ExpressionContext Set(string name, double value) { Variables[name] = value; return this; }

        public bool TryGetVariable(string name, out double value) => Variables.TryGetValue(name, out value);

        public double ResolveAccessor(string kind, string name)
        {
            switch (kind)
            {
                case "av":    if (Av    == null) throw new ExpressionException($"av() accessor not provided (needed for '{name}')");    return Av(name);
                case "res":   if (Res   == null) throw new ExpressionException($"res() accessor not provided (needed for '{name}')");   return Res(name);
                case "owned": if (Owned == null) throw new ExpressionException($"owned() accessor not provided (needed for '{name}')"); return Owned(name);
                default: throw new ExpressionException($"Unknown accessor '{kind}'");
            }
        }
    }

    public sealed class ExpressionException : Exception
    {
        public ExpressionException(string message) : base(message) { }
    }

    /// <summary>A parsed, reusable expression. Parse once; evaluate many times.</summary>
    public sealed class Expression
    {
        private readonly Node _root;
        public string Source { get; }

        private Expression(Node root, string source) { _root = root; Source = source; }

        public static Expression Parse(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ExpressionException("Empty expression.");
            var tokens = Tokenizer.Tokenize(source);
            var parser = new Parser(tokens, source);
            var root = parser.ParseAll();
            return new Expression(root, source);
        }

        public double Evaluate(IExpressionContext context)
        {
            if (context == null) throw new ExpressionException("No evaluation context supplied.");
            return _root.Eval(context);
        }

        /// <summary>All variable identifiers this expression reads (for validation).</summary>
        public IReadOnlyCollection<string> VariableReferences()
        { var s = new HashSet<string>(); _root.CollectVariables(s); return s; }

        /// <summary>All (kind,name) state accessors this expression reads (for cycle-checking).</summary>
        public IReadOnlyCollection<(string kind, string name)> AccessorReferences()
        { var s = new HashSet<(string, string)>(); _root.CollectAccessors(s); return s; }

        public override string ToString() => Source;

        // =================================================================
        // Tokenizer
        // =================================================================
        private enum TokType { Number, Ident, String, Op, LParen, RParen, Comma, Question, Colon, End }

        private readonly struct Token
        {
            public readonly TokType Type; public readonly string Text; public readonly double Num; public readonly int Pos;
            public Token(TokType t, string text, int pos, double num = 0) { Type = t; Text = text; Pos = pos; Num = num; }
            public override string ToString() => $"{Type}:'{Text}'@{Pos}";
        }

        private static class Tokenizer
        {
            public static List<Token> Tokenize(string s)
            {
                var toks = new List<Token>();
                int i = 0, n = s.Length;
                while (i < n)
                {
                    char c = s[i];
                    if (char.IsWhiteSpace(c)) { i++; continue; }

                    // number: digits with optional single '.' and optional decimals
                    if (char.IsDigit(c) || (c == '.' && i + 1 < n && char.IsDigit(s[i + 1])))
                    {
                        int start = i; bool dot = false;
                        while (i < n && (char.IsDigit(s[i]) || (s[i] == '.' && !dot)))
                        { if (s[i] == '.') dot = true; i++; }
                        string num = s.Substring(start, i - start);
                        if (!double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                            throw new ExpressionException($"Bad number '{num}' at {start}.");
                        toks.Add(new Token(TokType.Number, num, start, d));
                        continue;
                    }

                    // identifier
                    if (char.IsLetter(c) || c == '_')
                    {
                        int start = i;
                        while (i < n && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                        toks.Add(new Token(TokType.Ident, s.Substring(start, i - start), start));
                        continue;
                    }

                    // string literal (used only as an accessor name argument)
                    if (c == '"' || c == '\'')
                    {
                        char quote = c; int start = i; i++; var sb = new StringBuilder();
                        while (i < n && s[i] != quote)
                        {
                            if (s[i] == '\\' && i + 1 < n) { i++; sb.Append(s[i]); }
                            else sb.Append(s[i]);
                            i++;
                        }
                        if (i >= n) throw new ExpressionException($"Unterminated string at {start}.");
                        i++; // closing quote
                        toks.Add(new Token(TokType.String, sb.ToString(), start));
                        continue;
                    }

                    switch (c)
                    {
                        case '(': toks.Add(new Token(TokType.LParen, "(", i)); i++; continue;
                        case ')': toks.Add(new Token(TokType.RParen, ")", i)); i++; continue;
                        case ',': toks.Add(new Token(TokType.Comma, ",", i)); i++; continue;
                        case '?': toks.Add(new Token(TokType.Question, "?", i)); i++; continue;
                        case ':': toks.Add(new Token(TokType.Colon, ":", i)); i++; continue;
                    }

                    // operators (two-char first)
                    string two = i + 1 < n ? s.Substring(i, 2) : null;
                    if (two == "==" || two == "!=" || two == "<=" || two == ">=" || two == "&&" || two == "||")
                    { toks.Add(new Token(TokType.Op, two, i)); i += 2; continue; }

                    if ("+-*/%^<>!".IndexOf(c) >= 0)
                    { toks.Add(new Token(TokType.Op, c.ToString(), i)); i++; continue; }

                    throw new ExpressionException($"Unexpected character '{c}' at {i}.");
                }
                toks.Add(new Token(TokType.End, "", s.Length));
                return toks;
            }
        }

        // =================================================================
        // Parser (recursive descent)
        // =================================================================
        private sealed class Parser
        {
            private readonly List<Token> _t; private readonly string _src; private int _p;
            public Parser(List<Token> toks, string src) { _t = toks; _src = src; }

            private Token Cur => _t[_p];
            private Token Next() => _t[_p++];
            private bool IsOp(string op) => Cur.Type == TokType.Op && Cur.Text == op;
            private ExpressionException Err(string m) => new ExpressionException($"{m} (at {Cur.Pos} in \"{_src}\")");

            public Node ParseAll()
            {
                var node = ParseTernary();
                if (Cur.Type != TokType.End) throw Err($"Unexpected '{Cur.Text}'");
                return node;
            }

            private Node ParseTernary()
            {
                var cond = ParseOr();
                if (Cur.Type == TokType.Question)
                {
                    Next();
                    var a = ParseTernary();
                    if (Cur.Type != TokType.Colon) throw Err("Expected ':' in ternary");
                    Next();
                    var b = ParseTernary();
                    return new TernaryNode(cond, a, b);
                }
                return cond;
            }

            private Node ParseOr()
            {
                var left = ParseAnd();
                while (IsOp("||")) { Next(); left = new BinaryNode("||", left, ParseAnd()); }
                return left;
            }

            private Node ParseAnd()
            {
                var left = ParseCmp();
                while (IsOp("&&")) { Next(); left = new BinaryNode("&&", left, ParseCmp()); }
                return left;
            }

            private Node ParseCmp()
            {
                var left = ParseSum();
                while (IsOp("==") || IsOp("!=") || IsOp("<") || IsOp("<=") || IsOp(">") || IsOp(">="))
                { var op = Next().Text; left = new BinaryNode(op, left, ParseSum()); }
                return left;
            }

            private Node ParseSum()
            {
                var left = ParseProduct();
                while (IsOp("+") || IsOp("-")) { var op = Next().Text; left = new BinaryNode(op, left, ParseProduct()); }
                return left;
            }

            private Node ParseProduct()
            {
                var left = ParseUnary();
                while (IsOp("*") || IsOp("/") || IsOp("%")) { var op = Next().Text; left = new BinaryNode(op, left, ParseUnary()); }
                return left;
            }

            private Node ParseUnary()
            {
                if (IsOp("-") || IsOp("!")) { var op = Next().Text; return new UnaryNode(op, ParseUnary()); }
                return ParsePower();
            }

            private Node ParsePower()
            {
                var baseNode = ParsePrimary();
                if (IsOp("^")) { Next(); var exp = ParseUnary(); return new BinaryNode("^", baseNode, exp); }
                return baseNode;
            }

            private static readonly HashSet<string> Accessors = new() { "av", "res", "owned" };

            private Node ParsePrimary()
            {
                var t = Cur;
                switch (t.Type)
                {
                    case TokType.Number: Next(); return new NumberNode(t.Num);

                    case TokType.LParen:
                    {
                        Next();
                        var inner = ParseTernary();
                        if (Cur.Type != TokType.RParen) throw Err("Expected ')'");
                        Next();
                        return inner;
                    }

                    case TokType.Ident:
                    {
                        Next();
                        if (Cur.Type == TokType.LParen) return ParseCallOrAccessor(t.Text);
                        return new VarNode(t.Text);
                    }

                    default:
                        throw Err($"Unexpected '{t.Text}'");
                }
            }

            private Node ParseCallOrAccessor(string name)
            {
                Next(); // consume '('

                if (Accessors.Contains(name))
                {
                    // single name argument: an identifier or a string literal
                    if (Cur.Type != TokType.Ident && Cur.Type != TokType.String)
                        throw Err($"{name}() expects a name (identifier or string)");
                    string argName = Next().Text;
                    if (Cur.Type != TokType.RParen) throw Err($"{name}() takes exactly one name argument");
                    Next();
                    return new AccessorNode(name, argName);
                }

                var args = new List<Node>();
                if (Cur.Type != TokType.RParen)
                {
                    args.Add(ParseTernary());
                    while (Cur.Type == TokType.Comma) { Next(); args.Add(ParseTernary()); }
                }
                if (Cur.Type != TokType.RParen) throw Err($"Expected ')' after arguments to {name}()");
                Next();

                if (!Functions.IsKnown(name)) throw Err($"Unknown function '{name}'");
                Functions.CheckArity(name, args.Count, this);
                return new CallNode(name, args);
            }

            public ExpressionException ArityError(string fn, int got, string expected)
                => Err($"{fn}() expects {expected} argument(s) but got {got}");
        }

        // =================================================================
        // Function whitelist
        // =================================================================
        private static class Functions
        {
            public static bool IsKnown(string n) => n switch
            {
                "min" or "max" or "clamp" or "floor" or "ceil" or "round" or "abs" or "sign"
                or "pow" or "sqrt" or "log" or "exp" or "step" or "lerp" => true,
                _ => false
            };

            public static void CheckArity(string n, int c, Parser p)
            {
                void Req(bool ok, string exp) { if (!ok) throw p.ArityError(n, c, exp); }
                switch (n)
                {
                    case "min": case "max": Req(c >= 2, "2 or more"); break;
                    case "clamp": case "lerp": Req(c == 3, "3"); break;
                    case "pow": case "step": Req(c == 2, "2"); break;
                    case "log": Req(c == 1 || c == 2, "1 or 2"); break;
                    default: Req(c == 1, "1"); break; // floor, ceil, round, abs, sign, sqrt, exp
                }
            }

            public static double Call(string n, double[] a)
            {
                switch (n)
                {
                    case "min": { double m = a[0]; for (int k = 1; k < a.Length; k++) m = Math.Min(m, a[k]); return m; }
                    case "max": { double m = a[0]; for (int k = 1; k < a.Length; k++) m = Math.Max(m, a[k]); return m; }
                    case "clamp": return Math.Min(Math.Max(a[0], a[1]), a[2]);
                    case "floor": return Math.Floor(a[0]);
                    case "ceil": return Math.Ceiling(a[0]);
                    case "round": return Math.Round(a[0], MidpointRounding.AwayFromZero);
                    case "abs": return Math.Abs(a[0]);
                    case "sign": return Math.Sign(a[0]);
                    case "pow": return Math.Pow(a[0], a[1]);
                    case "sqrt": return Math.Sqrt(a[0]);
                    case "log": return a.Length == 2 ? Math.Log(a[0], a[1]) : Math.Log(a[0]);
                    case "exp": return Math.Exp(a[0]);
                    case "step": return a[1] < a[0] ? 0.0 : 1.0;       // step(edge, x)
                    case "lerp": return a[0] + (a[1] - a[0]) * a[2];   // lerp(a, b, t)
                    default: throw new ExpressionException($"Unknown function '{n}'");
                }
            }
        }

        // =================================================================
        // AST
        // =================================================================
        private abstract class Node
        {
            public abstract double Eval(IExpressionContext ctx);
            public virtual void CollectVariables(HashSet<string> s) { }
            public virtual void CollectAccessors(HashSet<(string, string)> s) { }
        }

        private sealed class NumberNode : Node
        {
            private readonly double _v; public NumberNode(double v) { _v = v; }
            public override double Eval(IExpressionContext ctx) => _v;
        }

        private sealed class VarNode : Node
        {
            private readonly string _name; public VarNode(string name) { _name = name; }
            public override double Eval(IExpressionContext ctx)
            {
                if (ctx.TryGetVariable(_name, out var v)) return v;
                throw new ExpressionException($"Unknown variable '{_name}'.");
            }
            public override void CollectVariables(HashSet<string> s) => s.Add(_name);
        }

        private sealed class AccessorNode : Node
        {
            private readonly string _kind, _name;
            public AccessorNode(string kind, string name) { _kind = kind; _name = name; }
            public override double Eval(IExpressionContext ctx) => ctx.ResolveAccessor(_kind, _name);
            public override void CollectAccessors(HashSet<(string, string)> s) => s.Add((_kind, _name));
        }

        private sealed class UnaryNode : Node
        {
            private readonly string _op; private readonly Node _c;
            public UnaryNode(string op, Node c) { _op = op; _c = c; }
            public override double Eval(IExpressionContext ctx)
            {
                double v = _c.Eval(ctx);
                return _op == "-" ? -v : (v != 0 ? 0.0 : 1.0); // "!"
            }
            public override void CollectVariables(HashSet<string> s) => _c.CollectVariables(s);
            public override void CollectAccessors(HashSet<(string, string)> s) => _c.CollectAccessors(s);
        }

        private sealed class BinaryNode : Node
        {
            private readonly string _op; private readonly Node _l, _r;
            public BinaryNode(string op, Node l, Node r) { _op = op; _l = l; _r = r; }

            public override double Eval(IExpressionContext ctx)
            {
                // Short-circuit logical operators.
                if (_op == "&&") return (_l.Eval(ctx) != 0 && _r.Eval(ctx) != 0) ? 1.0 : 0.0;
                if (_op == "||") return (_l.Eval(ctx) != 0 || _r.Eval(ctx) != 0) ? 1.0 : 0.0;

                double a = _l.Eval(ctx), b = _r.Eval(ctx);
                switch (_op)
                {
                    case "+": return a + b;
                    case "-": return a - b;
                    case "*": return a * b;
                    case "/": return a / b;
                    case "%": return a % b;
                    case "^": return Math.Pow(a, b);
                    case "==": return a == b ? 1.0 : 0.0;
                    case "!=": return a != b ? 1.0 : 0.0;
                    case "<": return a < b ? 1.0 : 0.0;
                    case "<=": return a <= b ? 1.0 : 0.0;
                    case ">": return a > b ? 1.0 : 0.0;
                    case ">=": return a >= b ? 1.0 : 0.0;
                    default: throw new ExpressionException($"Unknown operator '{_op}'.");
                }
            }
            public override void CollectVariables(HashSet<string> s) { _l.CollectVariables(s); _r.CollectVariables(s); }
            public override void CollectAccessors(HashSet<(string, string)> s) { _l.CollectAccessors(s); _r.CollectAccessors(s); }
        }

        private sealed class TernaryNode : Node
        {
            private readonly Node _cond, _a, _b;
            public TernaryNode(Node cond, Node a, Node b) { _cond = cond; _a = a; _b = b; }
            public override double Eval(IExpressionContext ctx) => _cond.Eval(ctx) != 0 ? _a.Eval(ctx) : _b.Eval(ctx);
            public override void CollectVariables(HashSet<string> s) { _cond.CollectVariables(s); _a.CollectVariables(s); _b.CollectVariables(s); }
            public override void CollectAccessors(HashSet<(string, string)> s) { _cond.CollectAccessors(s); _a.CollectAccessors(s); _b.CollectAccessors(s); }
        }

        private sealed class CallNode : Node
        {
            private readonly string _fn; private readonly Node[] _args;
            public CallNode(string fn, List<Node> args) { _fn = fn; _args = args.ToArray(); }
            public override double Eval(IExpressionContext ctx)
            {
                var vals = new double[_args.Length];
                for (int k = 0; k < _args.Length; k++) vals[k] = _args[k].Eval(ctx);
                return Functions.Call(_fn, vals);
            }
            public override void CollectVariables(HashSet<string> s) { foreach (var a in _args) a.CollectVariables(s); }
            public override void CollectAccessors(HashSet<(string, string)> s) { foreach (var a in _args) a.CollectAccessors(s); }
        }
    }
}
