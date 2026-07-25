// Assets/Scripts/Data/ComputedValue.cs
//
// HypnicEmpire — ComputedValue (engine-free core)
// -----------------------------------------------
// A single JSON value that may be any of four shapes, all evaluated uniformly
// through Evaluate(context):
//
//   Literal:  15
//   Formula:  { "Formula": "3 * 1.12 ^ count" }
//   Curve:    { "Curve": "linear", "Input": "x", "Keys": [ {"x":0,"y":300}, {"x":100,"y":20} ] }
//   Table:    { "Table": [3,4,4,5,5,6], "Index": "count" }
//
// A bare number stays valid everywhere a ComputedValue is expected, so existing
// data keeps loading unchanged as fields migrate to this type.
//
// Numeric policy (per confirmed decisions):
//   - Evaluate(...) returns a double at full precision. Non-integer targets
//     (resource deltas like +0.2 Food, speed multipliers, etc.) use this directly.
//   - Integer targets (AlterableValues are int) call EvaluateInt(...), which
//     rounds half-away-from-zero at assignment. Rounding lives at the assignment
//     boundary, never inside intermediate math.
//
// No UnityEngine / UnityEditor dependency (Newtonsoft only), so runtime,
// validator, and visualizer share it and it ports to non-Unity systems.
//
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HypnicEmpire
{
    public enum ComputedValueKind { Literal, Formula, Curve, Table }
    public enum CurveMode { Linear, Step }

    [JsonConverter(typeof(ComputedValueConverter))]
    public sealed class ComputedValue
    {
        public ComputedValueKind Kind { get; private set; }

        // Literal
        private double _literal;

        // Formula
        public string FormulaSource { get; private set; }
        private Expression _formula;

        // Curve
        private CurveMode _curveMode;
        private string _curveInput;            // variable that drives the curve (default "x")
        private double[] _curveX;              // sorted ascending
        private double[] _curveY;

        // Table
        private double[] _table;
        private string _tableIndex;            // variable used as the index (default "count")

        // ---- factories ---------------------------------------------------
        public static ComputedValue FromLiteral(double v) => new() { Kind = ComputedValueKind.Literal, _literal = v };

        public static ComputedValue FromFormula(string source)
        {
            var cv = new ComputedValue { Kind = ComputedValueKind.Formula, FormulaSource = source };
            cv._formula = Expression.Parse(source); // throws ExpressionException on bad syntax
            return cv;
        }

        public static ComputedValue FromCurve(CurveMode mode, string input, IList<(double x, double y)> keys)
        {
            if (keys == null || keys.Count == 0) throw new ArgumentException("Curve needs at least one key.");
            var sorted = new List<(double x, double y)>(keys);
            sorted.Sort((p, q) => p.x.CompareTo(q.x));
            var cv = new ComputedValue
            {
                Kind = ComputedValueKind.Curve,
                _curveMode = mode,
                _curveInput = string.IsNullOrEmpty(input) ? "x" : input,
                _curveX = new double[sorted.Count],
                _curveY = new double[sorted.Count]
            };
            for (int i = 0; i < sorted.Count; i++) { cv._curveX[i] = sorted[i].x; cv._curveY[i] = sorted[i].y; }
            return cv;
        }

        public static ComputedValue FromTable(IList<double> table, string index)
        {
            if (table == null || table.Count == 0) throw new ArgumentException("Table needs at least one entry.");
            var cv = new ComputedValue { Kind = ComputedValueKind.Table, _tableIndex = string.IsNullOrEmpty(index) ? "count" : index };
            cv._table = new double[table.Count];
            for (int i = 0; i < table.Count; i++) cv._table[i] = table[i];
            return cv;
        }

        // ---- evaluation --------------------------------------------------
        /// <summary>Full-precision value. Use for non-integer targets (resources, multipliers, ...).</summary>
        public double Evaluate(IExpressionContext context)
        {
            switch (Kind)
            {
                case ComputedValueKind.Literal: return _literal;
                case ComputedValueKind.Formula: return _formula.Evaluate(context);
                case ComputedValueKind.Curve: return EvaluateCurve(RequireVar(context, _curveInput));
                case ComputedValueKind.Table: return EvaluateTable(RequireVar(context, _tableIndex));
                default: throw new InvalidOperationException("Unknown ComputedValue kind.");
            }
        }

        /// <summary>Value rounded half-away-from-zero, for integer targets (AlterableValues).</summary>
        public long EvaluateInt(IExpressionContext context)
            => (long)Math.Round(Evaluate(context), MidpointRounding.AwayFromZero);

        private static double RequireVar(IExpressionContext ctx, string name)
        {
            if (ctx == null) throw new ExpressionException("No evaluation context supplied.");
            if (ctx.TryGetVariable(name, out var v)) return v;
            throw new ExpressionException($"ComputedValue needs variable '{name}' but the context did not provide it.");
        }

        private double EvaluateCurve(double x)
        {
            int n = _curveX.Length;
            if (x <= _curveX[0]) return _curveY[0];
            if (x >= _curveX[n - 1]) return _curveY[n - 1];

            // Largest i with _curveX[i] <= x. The endpoint clamps above guarantee
            // 0 <= i <= n-2 here, so [i, i+1] is always a valid segment.
            int i = 0;
            while (i < n - 1 && _curveX[i + 1] <= x) i++;

            if (_curveMode == CurveMode.Step) return _curveY[i];

            double x0 = _curveX[i], x1 = _curveX[i + 1];
            double y0 = _curveY[i], y1 = _curveY[i + 1];
            double t = (x1 == x0) ? 0.0 : (x - x0) / (x1 - x0);
            return y0 + (y1 - y0) * t;
        }

        private double EvaluateTable(double indexValue)
        {
            int idx = (int)Math.Round(indexValue, MidpointRounding.AwayFromZero);
            if (idx < 0) idx = 0;
            if (idx >= _table.Length) idx = _table.Length - 1; // clamp: last tier repeats
            return _table[idx];
        }

        // ---- for validation / tooling -----------------------------------
        public IReadOnlyCollection<string> VariableReferences()
        {
            switch (Kind)
            {
                case ComputedValueKind.Formula: return _formula.VariableReferences();
                case ComputedValueKind.Curve: return new[] { _curveInput };
                case ComputedValueKind.Table: return new[] { _tableIndex };
                default: return Array.Empty<string>();
            }
        }

        public IReadOnlyCollection<(string kind, string name)> AccessorReferences()
            => Kind == ComputedValueKind.Formula
                ? _formula.AccessorReferences()
                : Array.Empty<(string, string)>();

        public override string ToString() => Kind switch
        {
            ComputedValueKind.Literal => _literal.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ComputedValueKind.Formula => $"Formula({FormulaSource})",
            ComputedValueKind.Curve => $"Curve({_curveMode},{_curveX.Length} keys)",
            ComputedValueKind.Table => $"Table({_table.Length})",
            _ => "ComputedValue"
        };

        // Internal setters used by the converter.
        internal void InitLiteral(double v) { Kind = ComputedValueKind.Literal; _literal = v; }
        internal void InitFormula(string src) { Kind = ComputedValueKind.Formula; FormulaSource = src; _formula = Expression.Parse(src); }
        internal void InitCurve(CurveMode m, string input, double[] xs, double[] ys)
        { Kind = ComputedValueKind.Curve; _curveMode = m; _curveInput = input; _curveX = xs; _curveY = ys; }
        internal void InitTable(double[] table, string index) { Kind = ComputedValueKind.Table; _table = table; _tableIndex = index; }
    }

    // =====================================================================
    // JSON converter: accepts a bare number or a {Formula|Curve|Table} object.
    // =====================================================================
    public sealed class ComputedValueConverter : JsonConverter<ComputedValue>
    {
        public override ComputedValue ReadJson(JsonReader reader, Type objectType, ComputedValue existingValue,
                                               bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            var cv = new ComputedValue();

            switch (token.Type)
            {
                case JTokenType.Integer:
                case JTokenType.Float:
                    cv.InitLiteral(token.Value<double>());
                    return cv;

                case JTokenType.Object:
                {
                    var obj = (JObject)token;

                    if (obj["Formula"] != null)
                    {
                        var src = obj["Formula"].Value<string>();
                        try { cv.InitFormula(src); }
                        catch (ExpressionException e) { throw new JsonSerializationException($"Invalid Formula \"{src}\": {e.Message}"); }
                        return cv;
                    }

                    if (obj["Table"] != null)
                    {
                        var arr = (JArray)obj["Table"];
                        var table = new double[arr.Count];
                        for (int i = 0; i < arr.Count; i++) table[i] = arr[i].Value<double>();
                        var index = obj["Index"]?.Value<string>() ?? "count";
                        cv.InitTable(table, index);
                        return cv;
                    }

                    if (obj["Keys"] != null || obj["Curve"] != null)
                    {
                        var modeStr = obj["Curve"]?.Value<string>() ?? "linear";
                        var mode = modeStr.Equals("step", StringComparison.OrdinalIgnoreCase) ? CurveMode.Step : CurveMode.Linear;
                        var input = obj["Input"]?.Value<string>() ?? "x";

                        var keys = (JArray)obj["Keys"];
                        if (keys == null || keys.Count == 0)
                            throw new JsonSerializationException("Curve ComputedValue requires a non-empty 'Keys' array.");

                        var pts = new List<(double x, double y)>(keys.Count);
                        foreach (var k in keys)
                            pts.Add((k["x"].Value<double>(), k["y"].Value<double>()));
                        pts.Sort((p, q) => p.x.CompareTo(q.x));

                        var xs = new double[pts.Count]; var ys = new double[pts.Count];
                        for (int i = 0; i < pts.Count; i++) { xs[i] = pts[i].x; ys[i] = pts[i].y; }
                        cv.InitCurve(mode, input, xs, ys);
                        return cv;
                    }

                    throw new JsonSerializationException(
                        "ComputedValue object must contain one of: 'Formula', 'Table', or 'Keys'/'Curve'.");
                }

                default:
                    throw new JsonSerializationException($"ComputedValue must be a number or an object, got {token.Type}.");
            }
        }

        public override void WriteJson(JsonWriter writer, ComputedValue value, JsonSerializer serializer)
        {
            switch (value.Kind)
            {
                case ComputedValueKind.Literal:
                    writer.WriteValue(value.Evaluate(new ExpressionContext())); // literal ignores context
                    break;
                case ComputedValueKind.Formula:
                    writer.WriteStartObject(); writer.WritePropertyName("Formula"); writer.WriteValue(value.FormulaSource); writer.WriteEndObject();
                    break;
                default:
                    // Curve/Table round-trip is authoring-side; not needed for runtime writes yet.
                    throw new JsonSerializationException($"Serializing ComputedValue kind {value.Kind} is not supported.");
            }
        }
    }
}
