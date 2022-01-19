using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace InfluxDB
{
    public class PointData
    {
        private string measurementName;

        private readonly Dictionary<string, object> Fields;

        private readonly Dictionary<string, string> Tags;

        public PointData()
        {
            Fields = new Dictionary<string, object>();
            Tags = new Dictionary<string, string>();
        }

        internal static PointData Measurement(string measurement)
        {
            return new PointData
            {
                measurementName = measurement
            };
        }

        public string ToLineProtocol()
        {
            var sb = new StringBuilder();
            EscapeKey(sb, measurementName, false);
            AppendTags(sb);
            bool appendedFields = AppendFields(sb);
            if (!appendedFields)
            {
                return "";
            }

            return sb.ToString();

        }

        internal PointData Field(string key, object value)
        {
            Fields.Add(key, value);
            return this;
        }

        internal PointData Tag(string key, string value)
        {
            Tags.Add(key, value);
            return this;
        }

        private void EscapeKey(StringBuilder sb, string key, bool escapeEqual = true)
        {
            foreach (var c in key)
            {
                switch (c)
                {
                    case '\n':
                        sb.Append("\\n");
                        continue;
                    case '\r':
                        sb.Append("\\r");
                        continue;
                    case '\t':
                        sb.Append("\\t");
                        continue;
                    case ' ':
                    case ',':
                        sb.Append("\\");
                        break;
                    case '=':
                        if (escapeEqual)
                        {
                            sb.Append("\\");
                        }
                        break;
                }

                sb.Append(c);
            }
        }

        private void AppendTags(StringBuilder writer)
        {


            foreach (var keyValue in Tags)
            {
                var key = keyValue.Key;
                var value = keyValue.Value;

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                {
                    continue;
                }

                writer.Append(',');
                EscapeKey(writer, key);
                writer.Append('=');
                EscapeKey(writer, value);
            }

            writer.Append(' ');
        }

        private bool AppendFields(StringBuilder sb)
        {
            var appended = false;

            foreach (var keyValue in Fields)
            {
                var key = keyValue.Key;
                var value = keyValue.Value;

                if (IsNotDefined(value))
                {
                    continue;
                }

                EscapeKey(sb, key);
                sb.Append('=');

                if (value is double || value is float)
                {
                    sb.Append(((IConvertible)value).ToString(CultureInfo.InvariantCulture));
                }
                else if (value is uint || value is ulong || value is ushort)
                {
                    sb.Append(((IConvertible)value).ToString(CultureInfo.InvariantCulture));
                    sb.Append('u');
                }
                else if (value is byte || value is int || value is long || value is sbyte || value is short)
                {
                    sb.Append(((IConvertible)value).ToString(CultureInfo.InvariantCulture));
                    sb.Append('i');
                }
                else if (value is bool b)
                {
                    sb.Append(b ? "true" : "false");
                }
                else if (value is string s)
                {
                    sb.Append('"');
                    EscapeValue(sb, s);
                    sb.Append('"');
                }
                else if (value is IConvertible c)
                {
                    sb.Append(c.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    sb.Append(value);
                }

                sb.Append(',');
                appended = true;
            }

            if (appended)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return appended;
        }

        private void EscapeValue(StringBuilder sb, string value)
        {
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\':
                    case '\"':
                        sb.Append("\\");
                        break;
                }

                sb.Append(c);
            }
        }
        private bool IsNotDefined(object value)
        {
            return value == null
                   || (value is double d && (double.IsInfinity(d) || double.IsNaN(d)))
                   || (value is float f && (float.IsInfinity(f) || float.IsNaN(f)));
        }
    }
}