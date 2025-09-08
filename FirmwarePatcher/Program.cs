using CommandLine;
using FirmwarePatcher.Models;
using FirmwarePatcher.Services;
using Serilog;

namespace FirmwarePatcher;

class Program
{
    static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<CommandLineOptions>(args)
            .MapResult(
                async (CommandLineOptions opts) => await RunAsync(opts),
                errs => Task.FromResult(1));
    }

    static async Task<int> RunAsync(CommandLineOptions options)
    {
        // Configure logging
        var logConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console();

        if (options.Verbose)
        {
            logConfig.MinimumLevel.Debug();
        }

        Log.Logger = logConfig.CreateLogger();

        try
        {
            // Handle self-test mode
            if (options.SelfTest)
            {
                Log.Information("Firmware Patcher v1.0 - Self Test Mode");
                var selfTestService = new SelfTestService(Log.Logger);
                var testSuccess = selfTestService.RunAllTests();
                return testSuccess ? 0 : 1;
            }

            Log.Information("Firmware Patcher v1.0");
            Log.Information("Source: {SourceFile}", options.SourceFile);
            Log.Information("Firmware: {FirmwareFile}", options.FirmwareFile);
            Log.Information("Output: {OutputFile}", options.OutputFile);

            // Validate required options when not in self-test mode
            if (string.IsNullOrEmpty(options.SourceFile) || 
                string.IsNullOrEmpty(options.FirmwareFile) || 
                string.IsNullOrEmpty(options.OutputFile))
            {
                Log.Error("Source, firmware, and output files are required");
                return 1;
            }

            // Create backup if requested
            if (options.CreateBackup)
            {
                await CreateBackupAsync(options.FirmwareFile);
            }

            // Initialize services
            var assemblerService = new AssemblerService(options.ToolchainPath, Log.Logger);
            var symbolParser = new SymbolTableParser(Log.Logger);
            var sectionExtractor = new SectionExtractor(Log.Logger);
            var patchApplicator = new PatchApplicator(Log.Logger);
            var validationService = new ValidationService(Log.Logger);
            var checksumService = new ChecksumService(Log.Logger);

            // Validate inputs
            Log.Information("=== Validation Phase ===");
            
            if (!validationService.ValidateToolchain(options.ToolchainPath))
            {
                Log.Error("Toolchain validation failed");
                return 1;
            }

            if (!validationService.ValidateAssemblyFile(options.SourceFile))
            {
                Log.Error("Assembly file validation failed");
                return 1;
            }

            // Assemble and link source file
            Log.Information("=== Assembly Phase ===");
            
            var elfFile = await assemblerService.AssembleAsync(options.SourceFile);

            // Extract symbol table and identify patches
            Log.Information("=== Analysis Phase ===");
            
            var symbols = await assemblerService.GetSymbolTableAsync(elfFile);
            Log.Information("Found {SymbolCount} symbols", symbols.Count);

            var patches = symbolParser.IdentifyPatchSections(symbols);
            if (patches.Count == 0)
            {
                Log.Error("No patch sections found in assembled ELF file");
                return 1;
            }

            // Extract binary data for each patch
            Log.Information("=== Data Extraction Phase ===");
            
            var sectionDumps = await assemblerService.GetSectionDumpAsync(elfFile);
            
            foreach (var patch in patches)
            {
                // First determine where this patch should be applied
                patch.TargetAddress = sectionExtractor.DetermineTargetAddress(patch, symbols);
                
                // Find the actual end of the data by looking for the next symbol after the target
                var symbolsAfterTarget = symbols
                    .Where(s => s.Address > patch.TargetAddress)
                    .OrderBy(s => s.Address)
                    .ToList();
                
                if (!symbolsAfterTarget.Any())
                {
                    Log.Error("Cannot determine end address for patch {PatchName} - no symbols found after target address 0x{Target:X8}", 
                        patch.Name, patch.TargetAddress);
                    throw new InvalidOperationException($"Cannot determine end address for patch {patch.Name}. No symbols found after target address 0x{patch.TargetAddress:X8}");
                }
                
                var targetEndAddress = symbolsAfterTarget.First().Address;
                
                // Extract the actual data from the source location (where assembler put it)
                patch.Data = sectionExtractor.ExtractSectionData(sectionDumps, patch.StartAddress, patch.EndAddress);
                
                Log.Information("Patch {PatchName}: {Size} bytes at target 0x{Target:X8} (extracted from 0x{Start:X8}-0x{End:X8})", 
                    patch.Name, patch.Data.Length, patch.TargetAddress, patch.StartAddress, patch.EndAddress);
            }

            Log.Information("Identified {PatchCount} patch sections:", patches.Count);
            foreach (var patch in patches)
            {
                Log.Information("  {Patch}", patch);
            }

            // Validate firmware and patches
            if (!validationService.ValidateFirmware(options.FirmwareFile, patches))
            {
                Log.Error("Firmware validation failed");
                return 1;
            }

            // Apply patches to firmware
            Log.Information("=== Patch Application Phase ===");
            
            var success = await patchApplicator.ApplyPatchesAsync(
                options.FirmwareFile, patches, options.OutputFile);

            if (!success)
            {
                Log.Error("Patch application failed");
                return 1;
            }

            // Compute and display checksums for the patched firmware
            Log.Information("=== Checksum Repair Phase ===");

            await checksumService.FixChecksums(options.OutputFile, options.OutputFile);

            // Sanity check...
            var firmware = await File.ReadAllBytesAsync(options.OutputFile);
            var checksumSegments = checksumService.ComputeAllChecksumsAsync(firmware);
            Log.Information("Computed checksums for {SegmentCount} segments:", checksumSegments.Count);
            foreach (var segment in checksumSegments)
            {
                Log.Information("  {Segment}", segment);
            }

            // Verify patches if requested
            if (options.Verify)
            {
                Log.Information("=== Verification Phase ===");
                
                if (!patchApplicator.VerifyPatches(options.OutputFile, patches))
                {
                    Log.Error("Patch verification failed");
                    return 1;
                }
            }

            // Generate report if requested
            if (!string.IsNullOrEmpty(options.ReportFile))
            {
                Log.Information("=== Report Generation ===");
                patchApplicator.GeneratePatchReport(patches, options.ReportFile);
            }

            // Show disassembly for verification
            if (options.Verbose)
            {
                Log.Information("=== Disassembly ===");
                var disassembly = await assemblerService.GetDisassemblyAsync(elfFile);
                Log.Debug("Disassembly:\n{Disassembly}", disassembly);
            }

            Log.Information("=== SUCCESS ===");
            Log.Information("Successfully applied {PatchCount} patches to firmware", patches.Count);
            Log.Information("Output written to: {OutputFile}", options.OutputFile);

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception occurred");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static async Task CreateBackupAsync(string firmwareFile)
    {
        var backupFile = $"{firmwareFile}.backup_{DateTime.Now:yyyyMMdd_HHmmss}";
        await File.WriteAllBytesAsync(backupFile, await File.ReadAllBytesAsync(firmwareFile));
        Log.Information("Created backup: {BackupFile}", backupFile);
    }
}
