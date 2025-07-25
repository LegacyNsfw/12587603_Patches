namespace FirmwarePatcher.Models;

public class FirmwareFile
{
    public string FilePath { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public long OriginalSize { get; set; }
    public uint Checksum { get; set; }
    
    public void CalculateChecksum()
    {
        Checksum = (uint)Data.Aggregate(0L, (sum, b) => sum + b);
    }
}
