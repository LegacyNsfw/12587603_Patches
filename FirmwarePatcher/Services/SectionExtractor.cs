using FirmwarePatcher.Models;
using Serilog;
using System.Text.RegularExpressions;

namespace FirmwarePatcher.Services;

public class SectionExtractor
{
    private readonly ILogger _logger;

    public SectionExtractor(ILogger logger)
    {
        _logger = logger;
    }

    public byte[] ExtractSectionData(Dictionary<string, string> sectionDumps, uint startAddress, uint endAddress)
    {
        _logger.Debug("Extracting section data from 0x{Start:X8} to 0x{End:X8}", startAddress, endAddress);
        
        var allBytes = new List<byte>();
        
        // Parse all section dumps and collect bytes in address order
        var addressBytes = new SortedDictionary<uint, byte>();
        
        foreach (var (sectionName, dumpContent) in sectionDumps)
        {
            _logger.Debug("Processing section: {SectionName}", sectionName);
            ParseSectionDump(dumpContent, addressBytes);
        }

        // Extract bytes in the specified range
        var missingBytes = 0;
        for (uint addr = startAddress; addr < endAddress; addr++)
        {
            if (addressBytes.TryGetValue(addr, out var byteValue))
            {
                allBytes.Add(byteValue);
            }
            else
            {
                missingBytes++;
                allBytes.Add(0x00); // Fill with zeros for missing data
            }
        }

        if (missingBytes > 0)
        {
            _logger.Error("Found {MissingCount} missing bytes in range 0x{Start:X8}-0x{End:X8}", 
                missingBytes, startAddress, endAddress);
            throw new InvalidOperationException($"Missing {missingBytes} bytes in range 0x{startAddress:X8}-0x{endAddress:X8}. This indicates the objdump output doesn't contain the expected data.");
        }

        _logger.Information("Extracted {Count} bytes from address range 0x{Start:X8}-0x{End:X8}", 
            allBytes.Count, startAddress, endAddress);
        
        return allBytes.ToArray();
    }

    public uint DetermineTargetAddress(PatchSection patch, List<SymbolInfo> symbols)
    {
        // Check if the start symbol itself is at the target address
        // (This happens when .org comes before the start label)
        var startSymbol = symbols.FirstOrDefault(s => s.Name == patch.StartLabel);
        if (startSymbol != null)
        {
            _logger.Information("Found target address for {PatchName}: 0x{Address:X8} (from start symbol {Symbol})", 
                patch.Name, startSymbol.Address, startSymbol.Name);
            return startSymbol.Address;
        }

        // Error: Cannot determine target address
        _logger.Error("Could not determine target address for {PatchName} - start symbol {StartLabel} not found", 
            patch.Name, patch.StartLabel);
        throw new InvalidOperationException($"Cannot determine target address for patch {patch.Name}. Start symbol {patch.StartLabel} not found in symbol table. Make sure the .org directive comes before the start label.");
    }

    private void ParseSectionDump(string dumpContent, SortedDictionary<uint, byte> addressBytes)
    {
        var lines = dumpContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || !char.IsAsciiHexDigit(trimmedLine[0]))
                continue;

            // Parse lines like: " 32778 4eb90008 0000 4e71 ..."
            var match = Regex.Match(trimmedLine, @"^\s*([0-9a-fA-F]+)\s+(.+)");
            if (!match.Success)
                continue;

            if (!uint.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, null, out var baseAddress))
                continue;

            var hexData = match.Groups[2].Value;
            
            // Extract hex bytes from the line
            var hexBytes = ExtractHexBytes(hexData);
            
            for (int i = 0; i < hexBytes.Count; i++)
            {
                addressBytes[baseAddress + (uint)i] = hexBytes[i];
            }
        }
    }

    private List<byte> ExtractHexBytes(string hexData)
    {
        var bytes = new List<byte>();
        
        // Remove any ASCII representation at the end (after multiple spaces)
        var dataOnly = Regex.Replace(hexData, @"\s{2,}.*$", "");
        
        // Split by whitespace and process each hex group
        var hexGroups = dataOnly.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var group in hexGroups)
        {
            // Process each group as pairs of hex digits
            for (int i = 0; i < group.Length; i += 2)
            {
                if (i + 1 < group.Length)
                {
                    var hexByte = group.Substring(i, 2);
                    if (byte.TryParse(hexByte, System.Globalization.NumberStyles.HexNumber, null, out var byteValue))
                    {
                        bytes.Add(byteValue);
                    }
                }
            }
        }
        
        return bytes;
    }
}
