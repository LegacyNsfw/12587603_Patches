using FirmwarePatcher.Models;
using Serilog;

namespace FirmwarePatcher.Services;

public class SymbolTableParser
{
    private readonly ILogger _logger;

    public SymbolTableParser(ILogger logger)
    {
        _logger = logger;
    }

    public List<PatchSection> IdentifyPatchSections(List<SymbolInfo> symbols)
    {
        var patches = new List<PatchSection>();
        
        // Find all PATCH_*_START symbols (including CODE and DATA sections)
        var patchStarts = symbols
            .Where(s => s.Name.StartsWith("PATCH_") && s.Name.EndsWith("_START"))
            .ToList();

        _logger.Information("Found {Count} patch start symbols", patchStarts.Count);

        foreach (var startSymbol in patchStarts)
        {
            var patchName = ExtractPatchName(startSymbol.Name);
            var endLabelName = $"PATCH_{patchName}_END";
            
            var endSymbol = symbols.FirstOrDefault(s => s.Name == endLabelName);
            if (endSymbol == null)
            {
                _logger.Error("No end label found for patch: {PatchName} (expected: {EndLabel})", patchName, endLabelName);
                throw new InvalidOperationException($"Missing end label for patch '{patchName}'. Expected label '{endLabelName}' was not found.");
            }

            if (endSymbol.Address <= startSymbol.Address)
            {
                _logger.Error("Invalid patch section: {PatchName} - end address <= start address", patchName);
                throw new InvalidOperationException($"Invalid patch section '{patchName}': end address (0x{endSymbol.Address:X8}) <= start address (0x{startSymbol.Address:X8})");
            }

            var patch = new PatchSection
            {
                Name = patchName,
                StartLabel = startSymbol.Name,
                EndLabel = endSymbol.Name,
                StartAddress = startSymbol.Address,
                EndAddress = endSymbol.Address
            };

            patches.Add(patch);
            _logger.Information("Discovered patch: {PatchName} at 0x{StartAddress:X8}-0x{EndAddress:X8} (size: {Size} bytes)", 
                patch.Name, patch.StartAddress, patch.EndAddress, patch.Size);
        }

        return patches.OrderBy(p => p.StartAddress).ToList();
    }

    public uint DetermineTargetAddress(PatchSection patch, List<SymbolInfo> symbols)
    {
        // Find the first symbol after the start label that has a different address
        // This should be the address from the .org directive
        var symbolsAfterStart = symbols
            .Where(s => s.Address >= patch.StartAddress && s.Address < patch.EndAddress)
            .Where(s => s.Name != patch.StartLabel)
            .OrderBy(s => s.Address)
            .ToList();

        if (symbolsAfterStart.Any())
        {
            var targetAddress = symbolsAfterStart.First().Address;
            _logger.Debug("Target address for {PatchName}: 0x{Address:X8}", patch.Name, targetAddress);
            return targetAddress;
        }

        // Error: Cannot determine target address
        _logger.Error("Could not determine target address for {PatchName} - no symbols found between start and end labels", patch.Name);
        throw new InvalidOperationException($"Cannot determine target address for patch '{patch.Name}'. Make sure there are symbols (code/data) between {patch.StartLabel} and {patch.EndLabel}.");
    }

    private string ExtractPatchName(string labelName)
    {
        // "PATCH_EOIT_HOOK_START" -> "EOIT_HOOK"
        const string prefix = "PATCH_";
        const string suffix = "_START";
        
        if (labelName.StartsWith(prefix) && labelName.EndsWith(suffix))
        {
            return labelName.Substring(prefix.Length, labelName.Length - prefix.Length - suffix.Length);
        }
        
        return labelName;
    }
}
