using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace QChompLibrary
{
    public class TupleKeyConverter : JsonConverter
    {
        // Override ReadJson to read the dictionary key and value
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            (int, int) _nestedTuple = (-1, -1);
            double _value = 0;
            var _dict = new Dictionary<(int[,] State, (int Height, int Width) Action), double>();

            // Loop through the JSON string reader
            while (reader.Read())
            {
                // Check whether it is a property
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string readerValue = reader.Value.ToString();
                    if (reader.Read())
                    {
                        // Check if the property is tuple (Dictionary key)
                        if (readerValue.Contains('(') && readerValue.Contains(')'))
                        {
                            // First item of the main tuple
                            int[,] _array = default;

                            // Get the nested tuple string
                            int nestedTupleIndex = readerValue.IndexOf('(', readerValue.IndexOf('(') + 1);
                            string nested = readerValue.Substring(nestedTupleIndex);
                            if (nested.Contains(')')) nested = nested.Substring(0, nested.Length - 1);
                            
                            // Get tuple values
                            string[] result = ConvertTuple(nested);

                            if (result == null)
                                continue;

                            // Custom Deserialize the Dictionary key (Tuple)
                            _nestedTuple = (int.Parse(result[0].Trim()), int.Parse(result[1].Trim()));
                            _array = (int[,])serializer.Deserialize(reader, _array.GetType());
                            (int[,], (int, int)) _tuple = (_array, _nestedTuple);

                            // Custom Deserialize the Dictionary value
                            _value = (double)serializer.Deserialize(reader, _value.GetType());

                            _dict.Add(_tuple, _value);
                        }
                        else
                        {
                            // Deserialize the remaining data from the reader
                            serializer.Deserialize(reader);
                            break;
                        }
                    }
                }
            }
            return _dict;
        }

        // To convert Tuple
        public string[] ConvertTuple(string _string)
        {
            string tempStr = null;

            // remove the first character which is a brace '('
            if (_string.Contains('('))
                tempStr = _string.Remove(0, 1);

            // remove the last character which is a brace ')'
            if (_string.Contains(')'))
                tempStr = tempStr.Remove(tempStr.Length - 1, 1);

            // seperate the Item1 and Item2
            if (_string.Contains(','))
                return tempStr.Split(',');

            return null;
        }

        // WriteJson needs to be implemented since it is an abstract function.
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        // Check whether to convert or not
        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}
