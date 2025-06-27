using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public interface IBasicAuther
{
    (string username, string password, bool ok) BasicAuth();
}

public class Credential
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string[] Perms { get; set; } = Array.Empty<string>();
}

public class CredentialsStore
{
    public const string AllUsers = "*";
    public const string PermAll = "all";
    public const string PermJoin = "join";
    public const string PermJoinReadOnly = "join-read-only";
    public const string PermRemove = "remove";
    public const string PermExecute = "execute";
    public const string PermQuery = "query";
    public const string PermStatus = "status";
    public const string PermReady = "ready";
    public const string PermBackup = "backup";
    public const string PermLoad = "load";
    public const string PermSnapshot = "snapshot";

    private readonly Dictionary<string, string> store = new();
    private readonly Dictionary<string, HashSet<string>> perms = new();

    public CredentialsStore() { }

    public static CredentialsStore NewCredentialsStoreFromFile(string path)
    {
        using var fs = File.OpenRead(path);
        var c = new CredentialsStore();
        c.Load(fs);
        return c;
    }

    public void Load(Stream s)
    {
        using var doc = JsonDocument.Parse(s);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            throw new JsonException("expected array");

        foreach (var elem in doc.RootElement.EnumerateArray())
        {
            var cred = elem.Deserialize<Credential>();
            if (cred == null) continue;
            store[cred.Username] = cred.Password;
            perms[cred.Username] = new HashSet<string>(cred.Perms ?? Array.Empty<string>());
        }
    }

    public bool Check(string username, string password)
    {
        return store.TryGetValue(username, out var pw) && pw == password;
    }

    public (string Password, bool Ok) PasswordFor(string username)
    {
        if (store.TryGetValue(username, out var pw))
            return (pw, true);
        return (string.Empty, false);
    }

    public bool CheckRequest(IBasicAuther b)
    {
        var (u, p, ok) = b.BasicAuth();
        return ok && Check(u, p);
    }

    public bool HasPerm(string username, string perm)
    {
        if (perms.TryGetValue(username, out var ps) && ps.Contains(perm))
            return true;
        if (perms.TryGetValue(AllUsers, out var ap) && ap.Contains(perm))
            return true;
        return false;
    }

    public bool HasAnyPerm(string username, params string[] perm)
    {
        foreach (var p in perm)
            if (HasPerm(username, p))
                return true;
        return false;
    }

    public static bool AA(CredentialsStore? c, string username, string password, string perm)
    {
        if (c == null)
            return true;
        if (c.HasAnyPerm(AllUsers, perm, PermAll))
            return true;
        if (string.IsNullOrEmpty(username))
            return false;
        if (!c.Check(username, password))
            return false;
        return c.HasAnyPerm(username, perm, PermAll);
    }

    public bool HasPermRequest(IBasicAuther b, string perm)
    {
        var (u, _, ok) = b.BasicAuth();
        return ok && HasPerm(u, perm);
    }
}
