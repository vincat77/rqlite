using System;
using System.Collections.Generic;
using System.Net;
using System.Web;

namespace rqbench.Http
{
    public class QueryParams : Dictionary<string, string>
    {
        public static QueryParams FromUri(Uri uri)
        {
            var qp = new QueryParams();
            var parsed = HttpUtility.ParseQueryString(uri.Query);
            foreach (string key in parsed)
            {
                if (key != null)
                    qp[key] = parsed[key]!;
            }
            if (qp.ContainsKey("freshness_strict") && !qp.ContainsKey("freshness"))
                throw new ArgumentException("freshness_strict requires freshness");
            foreach (var k in new[] { "timeout", "freshness", "db_timeout", "linearizable_timeout" })
            {
                if (qp.TryGetValue(k, out var val))
                    _ = TimeSpan.Parse(val);
            }
            foreach (var k in new[] { "retries", "trailing_logs" })
            {
                if (qp.TryGetValue(k, out var val))
                    _ = int.Parse(val);
            }
            if (qp.TryGetValue("q", out var q) && q == "")
                throw new ArgumentException("query parameter not set");
            return qp;
        }

        public bool Timings() => ContainsKey("timings");
        public bool Tx() => ContainsKey("transaction");
        public bool Queue() => ContainsKey("queue");
        public bool Pretty() => ContainsKey("pretty");
        public bool Bypass() => ContainsKey("bypass");
        public bool NoParse() => ContainsKey("noparse");
        public bool Wait() => ContainsKey("wait");
        public bool Associative() => ContainsKey("associative");
        public bool BlobArray() => ContainsKey("blob_array");
        public bool NoRewriteRandom() => ContainsKey("norwrandom");
        public bool NoRewriteTime() => ContainsKey("norwtime");
        public bool NonVoters() => ContainsKey("nonvoters");
        public bool NoLeader() => ContainsKey("noleader");
        public bool Redirect() => ContainsKey("redirect");
        public bool Vacuum() => ContainsKey("vacuum");
        public bool Compress() => ContainsKey("compress");

        public string KeyParam => this.TryGetValue("key", out var v) ? v : string.Empty;

        public TimeSpan DBTimeout(TimeSpan def)
        {
            if (TryGetValue("db_timeout", out var v) && TimeSpan.TryParse(v, out var t))
                return t;
            return def;
        }

        public TimeSpan LinearizableTimeout(TimeSpan def)
        {
            if (TryGetValue("linearizable_timeout", out var v) && TimeSpan.TryParse(v, out var t))
                return t;
            return def;
        }

        public string Query => this.TryGetValue("q", out var v) ? v : string.Empty;

        public TimeSpan Freshness()
        {
            if (TryGetValue("freshness", out var v) && TimeSpan.TryParse(v, out var t))
                return t;
            return TimeSpan.Zero;
        }

        public bool FreshnessStrict() => ContainsKey("freshness_strict");

        public bool Sync() => ContainsKey("sync");
        public bool RaftIndex() => ContainsKey("raft_index");

        public TimeSpan Timeout(TimeSpan def)
        {
            if (TryGetValue("timeout", out var v) && TimeSpan.TryParse(v, out var t))
                return t;
            return def;
        }

        public int Retries(int def)
        {
            if (TryGetValue("retries", out var v) && int.TryParse(v, out var r))
                return r;
            return def;
        }

        public int TrailingLogs(int def)
        {
            if (TryGetValue("trailing_logs", out var v) && int.TryParse(v, out var r))
                return r;
            return def;
        }

        public string Version() => this.TryGetValue("ver", out var v) ? v : string.Empty;
    }
}
