namespace FirmwarePatcher.Utilities;

public static class BinaryFileHandler
{
    public static async Task<byte[]> ReadAllBytesAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }
        
        return await File.ReadAllBytesAsync(filePath);
    }

    public static async Task WriteAllBytesAsync(string filePath, byte[] data)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        await File.WriteAllBytesAsync(filePath, data);
    }

    public static long GetFileSize(string filePath)
    {
        return new FileInfo(filePath).Length;
    }

    public static uint CalculateChecksum(byte[] data)
    {
        return (uint)data.Aggregate(0L, (sum, b) => sum + b);
    }

    public static string FormatSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
    }
}
