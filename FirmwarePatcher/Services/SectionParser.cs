using FirmwarePatcher.Models;
using Serilog;

namespace FirmwarePatcher.Services;

public class SectionParser
{
    private const string ImplementationSectionName = ".patch_code";
    private const string DataSectionName = ".patch_data";

    private readonly ILogger _logger;

    public SectionParser(ILogger logger)
    {
        _logger = logger;
    }

    public PatchSection GetImplementation(List<SectionHeaderInfo> sectionHeaders)
    {
        return GetSection(sectionHeaders, ImplementationSectionName, "CODE");
    }

    public PatchSection GetData(List<SectionHeaderInfo> sectionHeaders)
    {
        return GetSection(sectionHeaders, DataSectionName, "DATA");
    }

    private PatchSection GetSection(List<SectionHeaderInfo> sectionHeaders, string sectionName, string patchName)
    {
        var header = sectionHeaders.FirstOrDefault(h => h.Name == sectionName);
        if (header == null)
        {
            _logger.Error("Section {SectionName} not found in ELF file", sectionName);
            throw new InvalidOperationException($"Section '{sectionName}' was not found in the linked ELF file.");
        }

        var patch = new PatchSection
        {
            Name = patchName,
            StartAddress = header.Address,
            EndAddress = header.Address + header.Size,
            TargetAddress = header.Address
        };

        _logger.Information("Discovered patch: {PatchName} at 0x{StartAddress:X8}-0x{EndAddress:X8} (size: {Size} bytes)",
            patch.Name, patch.StartAddress, patch.EndAddress, patch.Size);

        return patch;
    }
}
