namespace FirmwarePatcher.Models;

public class SymbolInfo
{
    public string Name { get; set; } = string.Empty;
    public uint Address { get; set; }
    public string Section { get; set; } = string.Empty;
    public SymbolType Type { get; set; }
    
    public override string ToString()
    {
        return $"{Address:X8} {Type} {Name}";
    }
}

public enum SymbolType
{
    Unknown,
    Text,
    Data,
    Bss,
    Absolute,
    Undefined
}
