using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace rqbench.Http
{
    public static class RequestParser
    {
        public class Statement
        {
            public string Sql { get; set; } = string.Empty;
            public List<Parameter> Parameters { get; } = new();
        }

        public class Parameter
        {
            public string Name { get; set; } = string.Empty;
            public object? Value { get; set; }
        }

        public static List<Statement> ParseRequest(Stream stream)
        {
            if (stream == null)
                throw new InvalidOperationException("no statements");

            using var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("invalid request");

            var result = new List<Statement>();
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                if (elem.ValueKind == JsonValueKind.String)
                {
                    result.Add(new Statement { Sql = elem.GetString()! });
                }
                else if (elem.ValueKind == JsonValueKind.Array)
                {
                    var items = new List<JsonElement>();
                    foreach (var item in elem.EnumerateArray())
                        items.Add(item);
                    if (items.Count == 0 || items[0].ValueKind != JsonValueKind.String)
                        throw new InvalidOperationException("invalid request");
                    var stmt = new Statement { Sql = items[0].GetString()! };
                    for (int i = 1; i < items.Count; i++)
                    {
                        stmt.Parameters.Add(MakeParameter(string.Empty, items[i]));
                    }
                    result.Add(stmt);
                }
                else
                {
                    throw new InvalidOperationException("invalid request");
                }
            }
            if (result.Count == 0)
                throw new InvalidOperationException("no statements");
            return result;
        }

        private static Parameter MakeParameter(string name, JsonElement elem)
        {
            switch (elem.ValueKind)
            {
                case JsonValueKind.Number:
                    if (elem.TryGetInt64(out long i64))
                        return new Parameter { Name = name, Value = i64 };
                    return new Parameter { Name = name, Value = elem.GetDouble() };
                case JsonValueKind.String:
                    return new Parameter { Name = name, Value = elem.GetString() };
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return new Parameter { Name = name, Value = elem.GetBoolean() };
                case JsonValueKind.Null:
                    return new Parameter { Name = name, Value = null };
                case JsonValueKind.Array:
                    var bytes = new List<byte>();
                    foreach (var it in elem.EnumerateArray())
                    {
                        if (it.TryGetInt32(out int b) && b >= 0 && b <= 255)
                            bytes.Add((byte)b);
                        else
                            throw new InvalidOperationException("unsupported type");
                    }
                    return new Parameter { Name = name, Value = bytes.ToArray() };
                case JsonValueKind.Object:
                    var p = new Parameter { Name = name };
                    foreach (var prop in elem.EnumerateObject())
                        p = MakeParameter(prop.Name, prop.Value);
                    return p;
                default:
                    throw new InvalidOperationException("unsupported type");
            }
        }
    }
}
