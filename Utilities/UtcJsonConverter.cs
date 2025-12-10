using System.Text.Json;
using System.Text.Json.Serialization;

namespace QwenHT.Utilities
{
    public class UtcJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetDateTime();
            // If the DateTime is not in UTC, convert it to UTC
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Ensure all DateTime values are written in UTC format
            DateTime utcValue = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc), // ✅ NO SHIFT
                _ => value
            };
            writer.WriteStringValue(utcValue);
        }
    }
    
    public class NullableUtcJsonConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            var value = reader.GetDateTime();
            // If the DateTime is not in UTC, convert it to UTC
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                // Ensure all DateTime values are written in UTC format
                DateTime utcValue = value.Value.Kind switch
                {
                    DateTimeKind.Utc => value.Value,
                    DateTimeKind.Local => value.Value.ToUniversalTime(),
                    DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc), // ✅ NO SHIFT
                    _ => value.Value
                };
                writer.WriteStringValue(utcValue);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}