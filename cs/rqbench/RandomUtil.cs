using System;
using System.Text;

static class RandomUtil
{
    private const string srcChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly Random rng = new Random();

    public static string StringN(int n)
    {
        var sb = new StringBuilder(n);
        for (int i = 0; i < n; i++)
        {
            int idx = rng.Next(srcChars.Length);
            sb.Append(srcChars[idx]);
        }
        return sb.ToString();
    }

    public static string String() => StringN(20);

    public static string StringPattern(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (ch == 'X' || ch == 'x')
            {
                int idx = rng.Next(srcChars.Length);
                sb.Append(srcChars[idx]);
            }
            else
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }

    public static byte[] Bytes(int n)
    {
        var b = new byte[n];
        rng.NextBytes(b);
        return b;
    }

    public static TimeSpan Jitter(TimeSpan d)
    {
        return d + TimeSpan.FromTicks((long)(rng.NextDouble() * d.Ticks));
    }
}
