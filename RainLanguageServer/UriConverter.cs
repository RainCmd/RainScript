using Newtonsoft.Json;
using System;

namespace RainLanguageServer
{
    sealed class UriConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Uri);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var str = (string)reader.Value;
                return new Uri(str.Replace("%3A", ":"));
            }

            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            throw new InvalidOperationException($"UriConverter: unsupported token type {reader.TokenType}");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (null == value)
            {
                writer.WriteNull();
                return;
            }

            if (value is Uri uri)
            {
                var scheme = uri.Scheme;
                var str = uri.ToString();
                // If URI does not have :// 它很可能没有标题:其中的方案
                // 没有路径，所以我们不必逃跑或处理斜杠。
                if (str.Contains("://"))
                {
                    str = uri.Scheme + "://" + str.Substring(scheme.Length + 3).Replace(":", "%3A").Replace('\\', '/');
                }

                writer.WriteValue(str);
                return;
            }

            throw new InvalidOperationException($"UriConverter: unsupported value type {value.GetType()}");
        }
    }
}
