using FirmwarePatcher.Models;
using Serilog;

namespace FirmwarePatcher.Services;

public class ValidationService
{
    private readonly ILogger _logger;

    public ValidationService(ILogger logger)
    {
        _logger = logger;
    }

    public bool ValidateAssemblyFile(string sourceFile)
    {
        try
        {
            if (!File.Exists(sourceFile))
            {
                _logger.Error("Assembly source file not found: {SourceFile}", sourceFile);
                return false;
            }

            var content = File.ReadAllText(sourceFile);
            
            // Check for required patch label pairs
            var requiredPatterns = new[]
            {
                ("PATCH_", "_START"),
                ("PATCH_", "_END")
            };

            var hasPatches = false;
            foreach (var (prefix, suffix) in requiredPatterns)
            {
                if (content.Contains(prefix) && content.Contains(suffix))
                {
                    hasPatches = true;
                    break;
                }
            }

            if (!hasPatches)
            {
                _logger.Warning("No patch labels found in assembly file");
                return false;
            }

            // Check for .org directives
            if (!content.Contains(".org"))
            {
                _logger.Warning("No .org directives found in assembly file");
            }

            // Check for CPU specification
            if (!content.Contains(".cpu") && !content.Contains(".arch"))
            {
                _logger.Warning("No CPU architecture specified in assembly file");
            }

            _logger.Information("Assembly file validation passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to validate assembly file");
            return false;
        }
    }

    public bool ValidateFirmware(string firmwarePath, List<PatchSection> patches)
    {
        try
        {
            if (!File.Exists(firmwarePath))
            {
                _logger.Error("Firmware file not found: {FirmwarePath}", firmwarePath);
                return false;
            }

            var firmwareInfo = new FileInfo(firmwarePath);
            _logger.Information("Firmware file: {Size} bytes", firmwareInfo.Length);

            // Check if firmware is reasonable size (between 1KB and 32MB)
            if (firmwareInfo.Length < 1024)
            {
                _logger.Warning("Firmware file seems too small: {Size} bytes", firmwareInfo.Length);
            }
            else if (firmwareInfo.Length > 32 * 1024 * 1024)
            {
                _logger.Warning("Firmware file seems very large: {Size} bytes", firmwareInfo.Length);
            }

            // Validate patch addresses
            foreach (var patch in patches)
            {
                if (patch.TargetAddress == 0)
                {
                    _logger.Error("Patch {PatchName} has invalid target address: 0x{Address:X8}", patch.Name, patch.TargetAddress);
                    return false;
                }

                if (patch.Size == 0)
                {
                    _logger.Error("Patch {PatchName} has zero size", patch.Name);
                    return false;
                }

                // Note: We allow patches beyond current firmware size (will extend)
                _logger.Debug("Patch {PatchName}: 0x{Address:X8} ({Size} bytes)", patch.Name, patch.TargetAddress, patch.Size);
            }

            _logger.Information("Firmware validation passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to validate firmware");
            return false;
        }
    }

    public bool ValidateToolchain(string? toolchainPath)
    {
        var tools = new[] { "m68k-elf-as", "m68k-elf-nm", "m68k-elf-objdump" };
        
        foreach (var tool in tools)
        {
            var toolPath = string.IsNullOrEmpty(toolchainPath) ? tool : Path.Combine(toolchainPath, tool);
            
            if (!IsToolAvailable(toolPath))
            {
                _logger.Error("Required tool not found or not executable: {Tool}", toolPath);
                return false;
            }
        }

        _logger.Information("Toolchain validation passed");
        return true;
    }

    private bool IsToolAvailable(string toolPath)
    {
        try
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(5000); // 5 second timeout
            
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
