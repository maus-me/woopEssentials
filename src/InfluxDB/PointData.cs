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
            StringBuilder sb = new StringBuilder();
            EscapeKey(sb, measurementName, false);
            AppendTags(sb);
            bool appendedFields = AppendFields(sb);
            return !appendedFields ? "" : sb.ToString();
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
            foreach (char c in key)
            {
                switch (c)
                {
                    case '\n':
                        _ = sb.Append("\\n");
                        continue;
                    case '\r':
                        _ = sb.Append("\\r");
                        continue;
                    case '\t':
                        _ = sb.Append("\\t");
                        continue;
                    case ' ':
                    case ',':
                        _ = sb.Append("\\");
                        break;
                    case '=':
                        if (escapeEqual)
                        {
                            _ = sb.Append("\\");
                        }
                        break;
                    default:
                        break;
                }

                _ = sb.Append(c);
            }
        }

        private void AppendTags(StringBuilder writer)
        {


            foreach (KeyValuePair<string, string> keyValue in Tags)
            {
                string key = keyValue.Key;
                string value = keyValue.Value;

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                {
                    continue;
                }

                _ = writer.Append(',');
                EscapeKey(writer, key);
                _ = writer.Append('=');
                EscapeKey(writer, value);
            }

            _ = writer.Append(' ');
        }

        private bool AppendFields(StringBuilder sb)
        {
            bool appended = false;

            foreach (KeyValuePair<string, object> keyValue in Fields)
            {
                string key = keyValue.Key;
                object value = keyValue.Value;

                if (IsNotDefined(value))
                {
                    continue;
                }

                EscapeKey(sb, key);
                _ = sb.Append('=');

                if (value is double || value is float)
                {
                    _ = sb.Append(((IConvertible)value).ToString(CultureInfo.InvariantCulture));
                }
                else if (value is uint || value is ulong || value is ushort)
                {
                    _ = sb.Append(((IConvertible)value).ToString(CultureInfo.InvariantCulture));
                    _ = sb.Append('u');
                }
                else if (value is byte || value is int || value is long || value is sbyte || value is short)
                {
                    _ = sb.Append(((IConvertible)value).ToString(CultureInfo.InvariantCulture));
                    _ = sb.Append('i');
                }
                else if (value is bool b)
                {
                    _ = sb.Append(b ? "true" : "false");
                }
                else if (value is string s)
                {
                    _ = sb.Append('"');
                    EscapeValue(sb, s);
                    _ = sb.Append('"');
                }
                else if (value is IConvertible c)
                {
                    _ = sb.Append(c.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    _ = sb.Append(value);
                }

                _ = sb.Append(',');
                appended = true;
            }

            if (appended)
            {
                _ = sb.Remove(sb.Length - 1, 1);
            }

            return appended;
        }

        private void EscapeValue(StringBuilder sb, string value)
        {
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                    case '\"':
                        _ = sb.Append("\\");
                        break;
                    default:
                        break;
                }

                _ = sb.Append(c);
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