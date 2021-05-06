using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashDownloader.DashFormat
{
    public class FormattedString
    {
        public const char VariableDelimeter = '$';
        public string Pattern { get; }

        private Dictionary<string, int> _variablePositionDictionary;
        private Dictionary<string, string> _variableValueDictionary;
        private void CreateVariablePositionDictionary()
        {
            int variableStart = -1;
            StringBuilder variableNameBuilder = null;
            for (int i = 0; i < Pattern.Length; i++)
            {
                if(variableStart == -1)
                {
                    if (Pattern[i] == VariableDelimeter)
                    {
                        variableStart = i;
                        variableNameBuilder = new StringBuilder();
                        continue;
                    }
                }
                if(variableStart != -1)
                {
                    if (Pattern[i] == VariableDelimeter)
                    {
                        string variableName = variableNameBuilder.ToString();
                        _variablePositionDictionary.Add(variableName, variableStart);
                        _variableValueDictionary.Add(variableName, null);
                        variableStart = -1;
                    }
                    else
                    {
                        variableNameBuilder.Append(Pattern[i]);
                    }
                }
            }
        }

        public FormattedString(string pattern)
        {
            Pattern = pattern;

            _variablePositionDictionary = new Dictionary<string, int>();
            _variableValueDictionary = new Dictionary<string, string>();

            CreateVariablePositionDictionary();
        }

        public string this[string key]
        {
            get => _variableValueDictionary[key];
            set => _variableValueDictionary[key] = value;
        }

        private string GetVariableByPosition(int patternPosition)
        {
            foreach(var variableEntry in _variablePositionDictionary)
            {
                if (variableEntry.Value <= patternPosition && patternPosition < variableEntry.Value + variableEntry.Key.Length)
                {
                    return variableEntry.Key;
                }
            }

            return null;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            for(int i = 0; i < Pattern.Length; i++)
            {
                if (Pattern[i] != VariableDelimeter)
                {
                    stringBuilder.Append(Pattern[i]);
                    continue;
                }
                else 
                {
                    string variableName = GetVariableByPosition(i);
                    string variableValue = this[variableName];
                    if(variableValue != null)
                    {
                        stringBuilder.Append(variableValue);
                    }

                    i += variableName.Length + 1;
                }
            }

            return stringBuilder.ToString();
        }
    }
}
