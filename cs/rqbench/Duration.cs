using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

[JsonConverter(typeof(DurationJsonConverter))]
public struct Duration
{
    public TimeSpan Value { get; set; }

    public Duration(TimeSpan value) => Value = value;

    public override string ToString() => Value.ToString();

    public static implicit operator TimeSpan(Duration d) => d.Value;
    public static implicit operator Duration(TimeSpan ts) => new Duration(ts);
}

public class DurationJsonConverter : JsonConverter<Duration>
{
    private static readonly Regex _regex = new("^(\\d+)(ms|s|m|h)$", RegexOptions.IgnoreCase);

    public override Duration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            long ns = reader.GetInt64();
            return new Duration(TimeSpan.FromTicks(ns / 100));
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            string s = reader.GetString()!;
            if (_regex.Match(s) is { Success: true } m)
            {
                long val = long.Parse(m.Groups[1].Value);
                string unit = m.Groups[2].Value.ToLower();
                TimeSpan ts = unit switch
                {
                    "ms" => TimeSpan.FromMilliseconds(val),
                    "s" => TimeSpan.FromSeconds(val),
                    "m" => TimeSpan.FromMinutes(val),
                    "h" => TimeSpan.FromHours(val),
                    _ => TimeSpan.Zero
                };
                return new Duration(ts);
            }
            if (TimeSpan.TryParse(s, out var ts2))
                return new Duration(ts2);
        }
        throw new JsonException("invalid duration");
    }

    public override void Write(Utf8JsonWriter writer, Duration value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString());
    }
}

[JsonConverter(typeof(StorageTypeJsonConverter))]
public struct StorageType
{
    public string Value { get; set; }
    public StorageType(string value) => Value = value;
    public override string ToString() => Value;
}

public class StorageTypeJsonConverter : JsonConverter<StorageType>
{
    public override StorageType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException();
        string s = reader.GetString()!;
        if (s != "s3")
            throw new JsonException("unsupported storage type");
        return new StorageType(s);
    }

    public override void Write(Utf8JsonWriter writer, StorageType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
