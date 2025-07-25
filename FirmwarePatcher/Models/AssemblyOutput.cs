namespace FirmwarePatcher.Models;

public class AssemblyOutput
{
    public string ObjectFilePath { get; set; } = string.Empty;
    public List<SymbolInfo> Symbols { get; set; } = new();
    public Dictionary<string, byte[]> SectionData { get; set; } = new();
    public List<PatchSection> Patches { get; set; } = new();
}
