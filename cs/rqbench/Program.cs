using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Options
{
    public string Addr = "localhost:4001";
    public int NumReqs = 100;
    public int BatchSz = 1;
    public int ModPrint = 0;
    public bool Tx = false;
    public bool Qw = false;
    public string Tp = "http";
    public string Path = "/db/execute";
    public string OneShot = "";
    public List<string> Remaining = new();
}

interface ITester
{
    string String();
    void Prepare(string stmt, int bSz, bool tx);
    TimeSpan Once();
    void Close();
}

class HTTPTester : ITester
{
    private readonly HttpClient client = new HttpClient();
    private string url;
    private byte[] payload = Array.Empty<byte>();

    public HTTPTester(string addr, string path)
    {
        url = $"http://{addr}{path}";
    }

    public override string ToString() => url;
    public string String() => url;

    public void Prepare(string stmt, int bSz, bool tx)
    {
        var arr = Enumerable.Repeat(stmt, bSz).ToArray();
        payload = JsonSerializer.SerializeToUtf8Bytes(arr);
        if (tx)
        {
            url += "?transaction";
        }
    }

    public TimeSpan Once()
    {
        var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var sw = Stopwatch.StartNew();
        var resp = client.PostAsync(url, content).GetAwaiter().GetResult();
        resp.EnsureSuccessStatusCode();
        var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var r = JsonSerializer.Deserialize<Response>(body);
        if (r?.Results == null || r.Results.Length == 0)
            throw new Exception($"expected at least 1 result, got {r?.Results?.Length ?? 0}");
        foreach (var res in r.Results)
        {
            if (!string.IsNullOrEmpty(res.Error))
                throw new Exception($"received error: {res.Error}");
        }
        return sw.Elapsed;
    }

    public void Close() { }
}

class QueuedHTTPTester : ITester
{
    private readonly HttpClient client = new HttpClient();
    private string url;
    private string waitURL;
    private byte[] payload = Array.Empty<byte>();

    public QueuedHTTPTester(string addr, string path)
    {
        url = $"http://{addr}{path}?queue";
        waitURL = $"http://{addr}{path}?queue&wait";
    }

    public override string ToString() => url;
    public string String() => url;

    public void Prepare(string stmt, int bSz, bool _)
    {
        var arr = Enumerable.Repeat(stmt, bSz).ToArray();
        payload = JsonSerializer.SerializeToUtf8Bytes(arr);
    }

    public TimeSpan Once()
    {
        var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var sw = Stopwatch.StartNew();
        var resp = client.PostAsync(url, content).GetAwaiter().GetResult();
        resp.EnsureSuccessStatusCode();
        resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return sw.Elapsed;
    }

    public void Close()
    {
        var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        client.PostAsync(waitURL, content).GetAwaiter().GetResult().EnsureSuccessStatusCode();
    }
}

class Response
{
    public Result[] Results { get; set; } = Array.Empty<Result>();
}

class Result
{
    public string Error { get; set; } = string.Empty;
}

class Program
{
    const string name = "rqbench";
    const string desc = "rqbench is a simple load testing utility for rqlite.";

    static void Usage()
    {
        Console.WriteLine($"\n{desc}\n");
        Console.WriteLine($"Usage: {name} [arguments] <SQL statement>");
        Console.WriteLine("  -a string\n        Node address (default \"localhost:4001\")");
        Console.WriteLine("  -n int\n        Number of requests (default 100)");
        Console.WriteLine("  -b int\n        Statements per request (default 1)");
        Console.WriteLine("  -m int\n        Print progress every m requests");
        Console.WriteLine("  -x\n        Use explicit transaction per request");
        Console.WriteLine("  -q\n        Use queued writes");
        Console.WriteLine("  -t string\n        Transport to use (default \"http\")");
        Console.WriteLine("  -p string\n        Endpoint to use (default \"/db/execute\")");
        Console.WriteLine("  -o string\n        One-shot execute statement to preload");
    }

    static Options Parse(string[] args)
    {
        var opt = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-a":
                    opt.Addr = args[++i];
                    break;
                case "-n":
                    opt.NumReqs = int.Parse(args[++i]);
                    break;
                case "-b":
                    opt.BatchSz = int.Parse(args[++i]);
                    break;
                case "-m":
                    opt.ModPrint = int.Parse(args[++i]);
                    break;
                case "-x":
                    opt.Tx = true;
                    break;
                case "-q":
                    opt.Qw = true;
                    break;
                case "-t":
                    opt.Tp = args[++i];
                    break;
                case "-p":
                    opt.Path = args[++i];
                    break;
                case "-o":
                    opt.OneShot = args[++i];
                    break;
                default:
                    opt.Remaining.Add(args[i]);
                    break;
            }
        }
        return opt;
    }

    static TimeSpan Run(ITester t, int n, int modPrint)
    {
        TimeSpan dur = TimeSpan.Zero;
        for (int i = 0; i < n; i++)
        {
            var d = t.Once();
            dur += d;
            if (modPrint != 0 && i != 0 && i % modPrint == 0)
            {
                Console.WriteLine($"{i} requests completed in {d}");
            }
        }
        t.Close();
        return dur;
    }

    static void Main(string[] args)
    {
        var opt = Parse(args);
        if (opt.Remaining.Count == 0)
        {
            Usage();
            return;
        }
        var stmt = opt.Remaining[0];
        if (opt.Tp != "http")
        {
            Console.Error.WriteLine($"not a valid transport: {opt.Tp}");
            return;
        }

        if (!string.IsNullOrEmpty(opt.OneShot))
        {
            var o = new HTTPTester(opt.Addr, "/db/execute");
            o.Prepare(opt.OneShot, 1, false);
            Run(o, 1, 0);
        }

        ITester tester = new HTTPTester(opt.Addr, opt.Path);
        if (opt.Qw)
        {
            Console.WriteLine("using queued write tester");
            tester = new QueuedHTTPTester(opt.Addr, opt.Path);
        }
        tester.Prepare(stmt, opt.BatchSz, opt.Tx);
        Console.WriteLine($"Test target: {tester.String()}");
        var d = Run(tester, opt.NumReqs, opt.ModPrint);
        Console.WriteLine($"Total duration: {d}");
        Console.WriteLine($"Requests/sec: {opt.NumReqs / d.TotalSeconds:F2}");
        Console.WriteLine($"Statements/sec: {(opt.NumReqs * opt.BatchSz) / d.TotalSeconds:F2}");
    }
}
