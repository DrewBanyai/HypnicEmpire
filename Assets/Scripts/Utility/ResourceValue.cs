using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HypnicEmpire
{
    [Serializable]
    [JsonConverter(typeof(ResourceValueConverter))]
    public class ResourceValue
    {
        static public bool SHOW_HUNDREDTHS = false;

        // Using an internal integer to represent the value in "hundredths"
        // This avoids all floating point inaccuracy and mixed-sign arithmetic issues.
        [UnityEngine.SerializeField] private long _totalHundredths;

        public int WholeValue => (int)(_totalHundredths / 100);
        public int Tenths => (int)(Math.Abs(_totalHundredths / 10) % 10);
        public int Hundredths => (int)(Math.Abs(_totalHundredths) % 10);

        public ResourceValue(int value = 0)
        {
            _totalHundredths = (long)value * 100;
        }

        public ResourceValue(double value)
        {
            _totalHundredths = (long)Math.Round(value * 100);
        }

        public ResourceValue(int whole, int tenths, int hundredths)
        {
            long sign = whole < 0 ? -1 : 1;
            // If whole is 0, we need to check if we're intending a negative value from components
            // But usually the sign is carried by the whole value as per user request.
            _totalHundredths = (long)whole * 100 + sign * (tenths * 10 + hundredths);
        }

        private ResourceValue(long totalHundredths)
        {
            _totalHundredths = totalHundredths;
        }

        public static ResourceValue operator +(ResourceValue a, ResourceValue b)
        {
            return new ResourceValue(a._totalHundredths + b._totalHundredths);
        }

        public static ResourceValue operator -(ResourceValue a, ResourceValue b)
        {
            return new ResourceValue(a._totalHundredths - b._totalHundredths);
        }

        public static bool operator <(ResourceValue a, ResourceValue b)
        {
            return a._totalHundredths < b._totalHundredths;
        }

        public static bool operator >(ResourceValue a, ResourceValue b)
        {
            return a._totalHundredths > b._totalHundredths;
        }

        public static bool operator <=(ResourceValue a, ResourceValue b)
        {
            return a._totalHundredths <= b._totalHundredths;
        }

        public static bool operator >=(ResourceValue a, ResourceValue b)
        {
            return a._totalHundredths >= b._totalHundredths;
        }

        /// Operator Overrides for douintsbles
        public static ResourceValue operator +(ResourceValue a, int b)
        {
            return new ResourceValue(a._totalHundredths + (int)(b * 100));
        }

        public static ResourceValue operator -(ResourceValue a, int b)
        {
            return new ResourceValue(a._totalHundredths - (int)(b * 100));
        }

        public static ResourceValue operator *(ResourceValue a, int b)
        {
            return new ResourceValue((long)(a._totalHundredths * b));
        }

        public static ResourceValue operator /(ResourceValue a, int b)
        {
            return new ResourceValue((long)(a._totalHundredths / b));
        }

        public static bool operator <(ResourceValue a, int b)
        {
            return a._totalHundredths < (int)(b * 100);
        }

        public static bool operator >(ResourceValue a, int b)
        {
            return a._totalHundredths > (int)(b * 100);
        }

        public static bool operator <=(ResourceValue a, int b)
        {
            return a._totalHundredths <= (int)(b * 100);
        }

        public static bool operator >=(ResourceValue a, int b)
        {
            return a._totalHundredths >= (int)(b * 100);
        }

        /// Operator Overrides for doubles
        public static ResourceValue operator +(ResourceValue a, double b)
        {
            return new ResourceValue(a._totalHundredths + (int)(b * 100.0));
        }

        public static ResourceValue operator -(ResourceValue a, double b)
        {
            return new ResourceValue(a._totalHundredths - (int)(b * 100.0));
        }

        public static ResourceValue operator *(ResourceValue a, double b)
        {
            return new ResourceValue((long)(a._totalHundredths * b));
        }

        public static ResourceValue operator /(ResourceValue a, double b)
        {
            return new ResourceValue((long)(a._totalHundredths / b));
        }

        public static bool operator <(ResourceValue a, double b)
        {
            return a._totalHundredths < (int)(b * 100.0);
        }

        public static bool operator >(ResourceValue a, double b)
        {
            return a._totalHundredths > (int)(b * 100.0);
        }

        public static bool operator <=(ResourceValue a, double b)
        {
            return a._totalHundredths <= (int)(b * 100.0);
        }

        public static bool operator >=(ResourceValue a, double b)
        {
            return a._totalHundredths >= (int)(b * 100.0);
        }

        public static bool operator ==(ResourceValue a, ResourceValue b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a._totalHundredths == b._totalHundredths;
        }

        public static bool operator ==(ResourceValue a, int b)
        {
            if (a is null) return false;
            return a._totalHundredths == (long)(b * 100);
        }

        public static bool operator ==(ResourceValue a, double b)
        {
            if (a is null) return false;
            return a._totalHundredths == (long)(b * 100);
        }

        public static bool operator !=(ResourceValue a, ResourceValue b)
        {
            return !(a == b);
        }

        public static bool operator !=(ResourceValue a, int b)
        {
            if (a is null) return true;
            return !(a._totalHundredths == (long)(b * 100));
        }

        public static bool operator !=(ResourceValue a, double b)
        {
            if (a is null) return true;
            return !(a._totalHundredths == (long)(b * 100));
        }

        public override bool Equals(object obj)
        {
            if (obj is ResourceValue other)
                return _totalHundredths == other._totalHundredths;
            return false;
        }

        public override int GetHashCode()
        {
            return _totalHundredths.GetHashCode();
        }

        // Implicit cast from int to allow comparisons like "if (ResourceValue == 0)"
        public static implicit operator ResourceValue(int value)
        {
            return new ResourceValue(value);
        }

        public ResourceValue Abs()
        {
            return new ResourceValue(Math.Abs(_totalHundredths));
        }

        public bool Positive => _totalHundredths > 0;

        public static ResourceValue Min(ResourceValue a, ResourceValue b)
        {
            return a._totalHundredths < b._totalHundredths ? a : b;
        }

        public static ResourceValue Max(ResourceValue a, ResourceValue b)
        {
            return a._totalHundredths > b._totalHundredths ? a : b;
        }

        public string Text()
        {
            string sign = _totalHundredths < 0 && WholeValue == 0 ? "-" : "";
            if (Hundredths != 0 && SHOW_HUNDREDTHS)
                return $"{sign}{WholeValue}.{Tenths}{Hundredths}";
            if (Tenths != 0)
                return $"{sign}{WholeValue}.{Tenths}";
            return $"{sign}{WholeValue}";
        }

        public override string ToString() { return Text(); }

        public double ToDouble()
        {
            return _totalHundredths / 100.0;
        }
    }

    public class ResourceValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ResourceValue);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is ResourceValue resourceValue)
            {
                writer.WriteValue(resourceValue.ToDouble());
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer || reader.TokenType == JsonToken.Float)
            {
                return new ResourceValue(Convert.ToDouble(reader.Value));
            }
            if (reader.TokenType == JsonToken.String)
            {
                if (double.TryParse((string)reader.Value, out double val))
                {
                    return new ResourceValue(val);
                }
            }
            if (reader.TokenType == JsonToken.StartObject)
            {
                JObject item = JObject.Load(reader);
                if (item.TryGetValue("_totalHundredths", out JToken total))
                {
                    // This allows loading from the serialized internal state if needed
                    return (ResourceValue)typeof(ResourceValue).GetConstructor(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new[] { typeof(long) }, null).Invoke(new object[] { total.Value<long>() });
                }
                // Check for component pieces if they exist
                int w = item.Value<int>("WholeValue");
                int t = item.Value<int>("Tenths");
                int h = item.Value<int>("Hundredths");
                return new ResourceValue(w, t, h);
            }
            return null;
        }
    }
}
