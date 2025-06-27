using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class Humanize
{
    private const double Base1000 = 1000.0;
    private const double Base1024 = 1024.0;

    private static readonly Dictionary<string, ulong> ByteSizeTable = new()
    {
        ["b"] = 1,
        ["kib"] = 1UL << 10,
        ["kb"] = 1000UL,
        ["mib"] = 1UL << 20,
        ["mb"] = 1000UL * 1000UL,
        ["gib"] = 1UL << 30,
        ["gb"] = 1000UL * 1000UL * 1000UL,
        ["tib"] = 1UL << 40,
        ["tb"] = 1000UL * 1000UL * 1000UL * 1000UL,
        ["pib"] = 1UL << 50,
        ["pb"] = 1000UL * 1000UL * 1000UL * 1000UL * 1000UL,
        ["eib"] = 1UL << 60,
        ["eb"] = 1000UL * 1000UL * 1000UL * 1000UL * 1000UL * 1000UL,
        [""] = 1,
        ["ki"] = 1UL << 10,
        ["k"] = 1000UL,
        ["mi"] = 1UL << 20,
        ["m"] = 1000UL * 1000UL,
        ["gi"] = 1UL << 30,
        ["g"] = 1000UL * 1000UL * 1000UL,
        ["ti"] = 1UL << 40,
        ["t"] = 1000UL * 1000UL * 1000UL * 1000UL,
        ["pi"] = 1UL << 50,
        ["p"] = 1000UL * 1000UL * 1000UL * 1000UL * 1000UL,
        ["ei"] = 1UL << 60,
        ["e"] = 1000UL * 1000UL * 1000UL * 1000UL * 1000UL * 1000UL,
    };

    private static string HumanateBytes(ulong s, double b, string[] sizes)
    {
        if (s < 10)
            return $"{s} B";
        var e = Math.Floor(Math.Log(s) / Math.Log(b));
        var suffix = sizes[(int)e];
        var val = Math.Floor(s / Math.Pow(b, e) * 10 + 0.5) / 10;
        string format = val < 10 ? "0.0" : "0";
        return string.Format(CultureInfo.InvariantCulture, $"{{0:{format}}} {{1}}", val, suffix);
    }

    public static string Bytes(ulong s)
    {
        string[] sizes = { "B", "kB", "MB", "GB", "TB", "PB", "EB" };
        return HumanateBytes(s, Base1000, sizes);
    }

    public static string IBytes(ulong s)
    {
        string[] sizes = { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" };
        return HumanateBytes(s, Base1024, sizes);
    }

    public static ulong ParseBytes(string s)
    {
        int last = 0;
        bool hasComma = false;
        foreach (char c in s)
        {
            if (!(char.IsDigit(c) || c == '.' || c == ','))
                break;
            if (c == ',')
                hasComma = true;
            last++;
        }
        string num = s.Substring(0, last);
        if (hasComma)
            num = num.Replace(",", "");
        if (!double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out double f))
            throw new FormatException("invalid number");
        string extra = s.Substring(last).Trim().ToLowerInvariant();
        if (ByteSizeTable.TryGetValue(extra, out ulong m))
        {
            f *= m;
            if (f >= ulong.MaxValue)
                throw new OverflowException("too large");
            return (ulong)f;
        }
        throw new FormatException($"unhandled size name: {extra}");
    }
}
