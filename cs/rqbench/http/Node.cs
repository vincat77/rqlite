using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace rqbench.Http
{
    public class Node
    {
        public string? ID { get; set; }
        public string? APIAddr { get; set; }
        public string? Addr { get; set; }
        public string Version { get; set; } = string.Empty;
        public bool Voter { get; set; }
        public bool Reachable { get; set; }
        public bool Leader { get; set; }
        public double Time { get; set; }
        public string TimeS { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;

        private readonly object _lock = new();

        public void SetError(string err)
        {
            lock (_lock)
            {
                Error = err;
            }
        }

        public string ToJson()
        {
            lock (_lock)
            {
                return JsonSerializer.Serialize(this);
            }
        }
    }

    public static class NodeUtils
    {
        public static void TestNodes(IEnumerable<Node> nodes, Func<Node, bool> tester)
        {
            var threads = new List<Thread>();
            foreach (var n in nodes)
            {
                var t = new Thread(() => tester(n));
                threads.Add(t);
                t.Start();
            }
            foreach (var t in threads)
                t.Join();
        }

        public static void EncodeNodes(IEnumerable<Node> nodes, Stream output)
        {
            JsonSerializer.Serialize(output, new { nodes });
        }

        public static List<Node> DecodeNodes(Stream input)
        {
            using var doc = JsonDocument.Parse(input);
            var list = new List<Node>();
            if (doc.RootElement.TryGetProperty("nodes", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var elem in arr.EnumerateArray())
                {
                    list.Add(JsonSerializer.Deserialize<Node>(elem.GetRawText())!);
                }
            }
            return list;
        }
    }
}
