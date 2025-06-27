using System;
using System.IO;

public static class CRC32Util
{
    private static readonly uint[] table = CreateTable();

    private static uint[] CreateTable()
    {
        var tbl = new uint[256];
        for (uint i = 0; i < tbl.Length; i++)
        {
            uint c = i;
            for (int j = 0; j < 8; j++)
            {
                if ((c & 1) != 0)
                    c = 0xEDB88320u ^ (c >> 1);
                else
                    c >>= 1;
            }
            tbl[i] = c;
        }
        return tbl;
    }

    public static uint CRC32(string path)
    {
        using var stream = File.OpenRead(path);
        return CRC32(stream);
    }

    public static uint CRC32(Stream stream)
    {
        uint crc = 0xFFFFFFFFu;
        int b;
        while ((b = stream.ReadByte()) != -1)
        {
            crc = table[(crc ^ (byte)b) & 0xFF] ^ (crc >> 8);
        }
        return crc ^ 0xFFFFFFFFu;
    }
}
