namespace FirmwarePatcher.Models;

public class SectionHeaderInfo
{
    public string Name { get; set; } = string.Empty;
    public uint Address { get; set; }
    public uint Size { get; set; }

    public override string ToString()
    {
        return $"{Name}: 0x{Address:X8} ({Size} bytes)";
    }
}
