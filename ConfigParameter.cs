using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace MarlinEasyConfig
{
    public enum ParameterType
    {
        Unset,
        String,
        Array,
        Boolean,
        Definition,
        Number
    }

    [Serializable]
    public class ConfigParameter : ISerializable, IComparable, IEquatable<ConfigParameter>
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Comment { get; set; }
        public bool IsAdvanced { get; set; }
        public bool IsDifferent { get; set; }
        public bool IsFiltered { get; set; }
        public string DifferentValue { get; set; }
        public ParameterType Type { get; private set; }
        
        private void SetType()
        {
            if (String.IsNullOrEmpty(Value))
            {
                Type = ParameterType.Definition;
            }
            else if (new Regex(@"^[\'\""].+[\'\""]").Match(Value).Success)
            {
                Type = ParameterType.String;
                Value = Value.Trim(new[] { '"', '\'' });
            }
            else if (new Regex(@"^{.+}").Match(Value).Success)
            {
                Type = ParameterType.Array;
            }
            else if (Value == "true" || Value == "false")
            {
                Type = ParameterType.Boolean;
            }
            else
            {
                Type = ParameterType.Number;
            }
        }

        public string CleanValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return Type switch
            {
                //ParameterType.String => Regex.Replace(value.Trim(new[] { '"', '\'', '\\' }).EscapeQuotes(), @"\s+", " "), //@"(?<!^)[\""\'](?!$)"
                ParameterType.String => Regex.Replace(value.EscapeQuotes(), @"\s+", " "), //@"(?<!^)[\""\'](?!$)"
                _ => Regex.Replace(value, @"\s+", " "),
            };
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            ConfigParameter otherParameters = obj as ConfigParameter;
            if (otherParameters != null)
            {
                if (Equals(otherParameters)) return 0;
                if (Name == otherParameters.Name && CleanValue(Value) != CleanValue(otherParameters.Value)) return -1;
            }
            return 1;
        }
        public bool Equals([AllowNull] ConfigParameter other)
        {
            return Name == other.Name && CleanValue(Value) == CleanValue(other.Value);
        }

        public ConfigParameter(string line, bool advanced = false)
        {
            string[] lineParts = line.Remove(0, 7).Trim().Split(new[] { ' ', '\t' }, 2);
            Name = lineParts[0];
            if (lineParts.Length > 1)
            {
                Value = lineParts[1].TrimStart().TrimEnd();

                var matchComment = new Regex(@"\/\/.+").Match(lineParts[1]);
                if (matchComment.Success)
                {
                    Value = Value.Replace(matchComment.Value, "").Trim();
                    Comment = matchComment.Value.Remove(0, 2).Trim();
                }
            }
            IsAdvanced = advanced;
            SetType();
        }

        public ConfigParameter(string name, string value, string comment = null, bool advanced = false)
        {
            Name = name;
            Value = value?.Trim(new[] { '"', '\'' });
            Comment = comment;
            IsAdvanced = advanced;
            SetType();
        }

        public Brush TypeBrush
        {
            get
            {
                return Type switch
                {
                    ParameterType.String => (Brush)(new BrushConverter().ConvertFromString("#d46f23")),
                    ParameterType.Number => Brushes.Black,
                    ParameterType.Boolean => (Brush)(new BrushConverter().ConvertFromString("#aa6cb9")),
                    ParameterType.Array => (Brush)(new BrushConverter().ConvertFromString("#efb82d")),
                    _ => Brushes.Black,
                };
            }
        }

        public Brush ComparisonBrush => IsDifferent ? (Brush)(new BrushConverter().ConvertFromString("#ffeaea")) : Brushes.Transparent;

        /**
         * Serialization
         **/
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("name", Name, typeof(string));
            info.AddValue("value", Value, typeof(string));
            info.AddValue("comment", Comment, typeof(string));
            info.AddValue("advanced", IsAdvanced, typeof(bool));
        }

        public ConfigParameter(SerializationInfo info, StreamingContext context)
        {
            Name = (string)info.GetValue("name", typeof(string));
            Value = (string)info.GetValue("value", typeof(string));
            Comment = (string)info.GetValue("comment", typeof(string));
            IsAdvanced = (bool)info.GetValue("advanced", typeof(bool));
        }
    }
}
