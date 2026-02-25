using System.Text.Json;
using System.Text.Json.Serialization;

namespace StatisticsAnalysisTool.MobileServer;

/// <summary>
/// Converts double values that are NaN, PositiveInfinity, or NegativeInfinity to 0
/// so they can be safely serialized as valid JSON numbers.
/// </summary>
public class SafeDoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (double.TryParse(str, out var val)) return val;
            return str switch
            {
                "NaN" => 0,
                "Infinity" => 0,
                "-Infinity" => 0,
                _ => 0
            };
        }

        return reader.GetDouble();
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            writer.WriteNumberValue(0);
        }
        else
        {
            writer.WriteNumberValue(value);
        }
    }
}
