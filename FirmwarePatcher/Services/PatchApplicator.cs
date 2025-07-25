using FirmwarePatcher.Models;
using Serilog;

namespace FirmwarePatcher.Services;

public class PatchApplicator
{
    private readonly ILogger _logger;

    public PatchApplicator(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<bool> ApplyPatchesAsync(string firmwarePath, List<PatchSection> patches, string outputPath)
    {
        try
        {
            _logger.Information("Loading firmware: {FirmwarePath}", firmwarePath);
            var firmware = await LoadFirmwareAsync(firmwarePath);
            
            _logger.Information("Validating patches against firmware");
            ValidatePatches(firmware, patches);
            
            _logger.Information("Checking if patches fit within firmware bounds");
            ValidatePatchesFitInFirmware(firmware, patches);
            
            _logger.Information("Applying {PatchCount} patches", patches.Count);
            foreach (var patch in patches.OrderBy(p => p.TargetAddress))
            {
                ApplyPatch(firmware, patch);
                _logger.Information("Applied patch {PatchName} at 0x{Address:X8} ({Size} bytes)", 
                    patch.Name, patch.TargetAddress, patch.Size);
            }
            
            _logger.Information("Saving patched firmware: {OutputPath}", outputPath);
            await SaveFirmwareAsync(firmware, outputPath);
            
            _logger.Information("Patch application completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to apply patches");
            return false;
        }
    }

    public bool VerifyPatches(string patchedFirmwarePath, List<PatchSection> patches)
    {
        try
        {
            _logger.Information("Verifying patches in: {FirmwarePath}", patchedFirmwarePath);
            var firmware = File.ReadAllBytes(patchedFirmwarePath);
            
            bool allPatchesValid = true;
            
            foreach (var patch in patches)
            {
                if (patch.TargetAddress + patch.Size > firmware.Length)
                {
                    _logger.Error("Patch {PatchName} extends beyond firmware bounds", patch.Name);
                    allPatchesValid = false;
                    continue;
                }
                
                var actualData = new byte[patch.Size];
                Array.Copy(firmware, patch.TargetAddress, actualData, 0, (int)patch.Size);
                
                if (actualData.SequenceEqual(patch.Data))
                {
                    _logger.Information("Patch {PatchName} verified successfully", patch.Name);
                }
                else
                {
                    _logger.Error("Patch {PatchName} verification failed - data mismatch", patch.Name);
                    _logger.Debug("Expected: {Expected}", Convert.ToHexString(patch.Data));
                    _logger.Debug("Actual:   {Actual}", Convert.ToHexString(actualData));
                    allPatchesValid = false;
                }
            }
            
            return allPatchesValid;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to verify patches");
            return false;
        }
    }

    public void GeneratePatchReport(List<PatchSection> patches, string reportPath)
    {
        try
        {
            _logger.Information("Generating patch report: {ReportPath}", reportPath);
            
            using var writer = new StreamWriter(reportPath);
            writer.WriteLine("Firmware Patch Report");
            writer.WriteLine("====================");
            writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Total Patches: {patches.Count}");
            writer.WriteLine();
            
            foreach (var patch in patches.OrderBy(p => p.TargetAddress))
            {
                writer.WriteLine($"Patch: {patch.Name}");
                writer.WriteLine($"  Target Address: 0x{patch.TargetAddress:X8}");
                writer.WriteLine($"  Size: {patch.Size} bytes");
                writer.WriteLine($"  Start Label: {patch.StartLabel}");
                writer.WriteLine($"  End Label: {patch.EndLabel}");
                writer.WriteLine($"  Data: {Convert.ToHexString(patch.Data)}");
                writer.WriteLine();
            }
            
            _logger.Information("Patch report generated successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate patch report");
        }
    }

    private async Task<byte[]> LoadFirmwareAsync(string firmwarePath)
    {
        if (!File.Exists(firmwarePath))
        {
            throw new FileNotFoundException($"Firmware file not found: {firmwarePath}");
        }
        
        var firmware = await File.ReadAllBytesAsync(firmwarePath);
        _logger.Information("Loaded firmware: {Size} bytes", firmware.Length);
        
        return firmware;
    }

    private void ValidatePatches(byte[] firmware, List<PatchSection> patches)
    {
        foreach (var patch in patches)
        {
            if (patch.Data.Length == 0)
            {
                throw new InvalidOperationException($"Patch {patch.Name} has no data");
            }
            
            if (patch.Size != patch.Data.Length)
            {
                throw new InvalidOperationException($"Patch {patch.Name} size mismatch: expected {patch.Size}, got {patch.Data.Length}");
            }
        }
        
        // Check for overlapping patches
        var sortedPatches = patches.OrderBy(p => p.TargetAddress).ToList();
        for (int i = 0; i < sortedPatches.Count - 1; i++)
        {
            var current = sortedPatches[i];
            var next = sortedPatches[i + 1];
            
            if (current.TargetAddress + current.Size > next.TargetAddress)
            {
                throw new InvalidOperationException($"Overlapping patches: {current.Name} and {next.Name}");
            }
        }
    }

    private void ValidatePatchesFitInFirmware(byte[] firmware, List<PatchSection> patches)
    {
        foreach (var patch in patches)
        {
            if (patch.TargetAddress + patch.Size > firmware.Length)
            {
                throw new InvalidOperationException(
                    $"Patch '{patch.Name}' extends beyond firmware bounds. " +
                    $"Patch requires address range 0x{patch.TargetAddress:X8} - 0x{(patch.TargetAddress + patch.Size - 1):X8}, " +
                    $"but firmware only extends to 0x{(firmware.Length - 1):X8}. " +
                    $"Firmware cannot be extended - patches must fit within existing firmware size.");
            }
        }
        
        _logger.Information("All patches fit within firmware bounds (size: {FirmwareSize} bytes)", firmware.Length);
    }

    private void ApplyPatch(byte[] firmware, PatchSection patch)
    {
        if (patch.TargetAddress + patch.Size > firmware.Length)
        {
            throw new InvalidOperationException($"Patch {patch.Name} extends beyond firmware bounds");
        }
        
        Array.Copy(patch.Data, 0, firmware, patch.TargetAddress, patch.Data.Length);
        
        _logger.Debug("Applied {Size} bytes at 0x{Address:X8} for patch {PatchName}", 
            patch.Size, patch.TargetAddress, patch.Name);
    }

    private async Task SaveFirmwareAsync(byte[] firmware, string outputPath)
    {
        await File.WriteAllBytesAsync(outputPath, firmware);
        _logger.Information("Saved patched firmware: {Size} bytes to {OutputPath}", firmware.Length, outputPath);
    }
}
