namespace FirmwarePatcher.Models;

public class PatchSection
{
    public string Name { get; set; } = string.Empty;
    public string StartLabel { get; set; } = string.Empty;
    public string EndLabel { get; set; } = string.Empty;
    public uint StartAddress { get; set; }
    public uint EndAddress { get; set; }
    public uint Size => EndAddress - StartAddress;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public uint TargetAddress { get; set; }
    
    public override string ToString()
    {
        return $"{Name}: 0x{TargetAddress:X8} ({Size} bytes)";
    }
}
