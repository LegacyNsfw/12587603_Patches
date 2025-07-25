using System.Diagnostics;
using System.Text;
using FirmwarePatcher.Models;
using Serilog;

namespace FirmwarePatcher.Services;

public class AssemblerService
{
    private readonly string _toolchainPath;
    private readonly ILogger _logger;

    public AssemblerService(string? toolchainPath, ILogger logger)
    {
        _toolchainPath = toolchainPath ?? "";
        _logger = logger;
    }

    public async Task<string> AssembleAsync(string sourceFile)
    {
        var objectFile = Path.ChangeExtension(sourceFile, ".o");
        var elfFile = Path.ChangeExtension(sourceFile, ".elf");
        var assemblerPath = GetToolPath("m68k-elf-as");
        var linkerPath = GetToolPath("m68k-elf-ld");
        
        var assemblerArguments = $"-mcpu=cpu32 -g -o \"{objectFile}\" \"{sourceFile}\"";
        
        _logger.Information("Assembling {SourceFile} to {ObjectFile}", sourceFile, objectFile);
        _logger.Debug("Command: {Assembler} {Arguments}", assemblerPath, assemblerArguments);
        
        var assembleResult = await RunProcessAsync(assemblerPath, assemblerArguments);
        
        if (assembleResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Assembly failed: {assembleResult.StandardError}");
        }
        
        if (!File.Exists(objectFile))
        {
            throw new FileNotFoundException($"Object file not created: {objectFile}");
        }

        // Now link the object file to create ELF using custom linker script
        var linkerScriptPath = Path.Combine(Path.GetDirectoryName(sourceFile)!, "patches.ld");
        var linkerArguments = $"-T \"{linkerScriptPath}\" -Map={Path.ChangeExtension(sourceFile, ".map")} -o \"{elfFile}\" \"{objectFile}\"";
        
        _logger.Information("Linking {ObjectFile} to {ElfFile}", objectFile, elfFile);
        _logger.Debug("Command: {Linker} {Arguments}", linkerPath, linkerArguments);
        
        var linkResult = await RunProcessAsync(linkerPath, linkerArguments);
        
        if (linkResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Linking failed: {linkResult.StandardError}");
        }
        
        if (!File.Exists(elfFile))
        {
            throw new FileNotFoundException($"ELF file not created: {elfFile}");
        }
        
        _logger.Information("Assembly and linking successful: {ElfFile}", elfFile);
        return elfFile;
    }

    public async Task<List<SymbolInfo>> GetSymbolTableAsync(string elfFile)
    {
        var nmPath = GetToolPath("m68k-elf-nm");
        var arguments = $"-n \"{elfFile}\"";
        
        _logger.Debug("Getting symbol table: {Tool} {Arguments}", nmPath, arguments);
        
        var result = await RunProcessAsync(nmPath, arguments);
        
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to get symbol table: {result.StandardError}");
        }
        
        return ParseSymbolTable(result.StandardOutput);
    }

    public async Task<Dictionary<string, string>> GetSectionDumpAsync(string elfFile)
    {
        var objdumpPath = GetToolPath("m68k-elf-objdump");
        var arguments = $"-s \"{elfFile}\"";
        
        _logger.Debug("Getting section dump: {Tool} {Arguments}", objdumpPath, arguments);
        
        var result = await RunProcessAsync(objdumpPath, arguments);
        
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to get section dump: {result.StandardError}");
        }
        
        return ParseSectionDump(result.StandardOutput);
    }

    public async Task<string> GetDisassemblyAsync(string elfFile)
    {
        var objdumpPath = GetToolPath("m68k-elf-objdump");
        var arguments = $"-d \"{elfFile}\"";
        
        _logger.Debug("Getting disassembly: {Tool} {Arguments}", objdumpPath, arguments);
        
        var result = await RunProcessAsync(objdumpPath, arguments);
        
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to get disassembly: {result.StandardError}");
        }
        
        return result.StandardOutput;
    }

    private string GetToolPath(string toolName)
    {
        if (!string.IsNullOrEmpty(_toolchainPath))
        {
            return Path.Combine(_toolchainPath, toolName);
        }
        return toolName;
    }

    private async Task<ProcessResult> RunProcessAsync(string fileName, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) => {
            if (e.Data != null) outputBuilder.AppendLine(e.Data);
        };
        
        process.ErrorDataReceived += (_, e) => {
            if (e.Data != null) errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        await process.WaitForExitAsync();

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = outputBuilder.ToString(),
            StandardError = errorBuilder.ToString()
        };
    }

    private List<SymbolInfo> ParseSymbolTable(string nmOutput)
    {
        var symbols = new List<SymbolInfo>();
        var lines = nmOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                if (uint.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out var address))
                {
                    symbols.Add(new SymbolInfo
                    {
                        Address = address,
                        Type = ParseSymbolType(parts[1]),
                        Name = parts[2]
                    });
                }
            }
        }

        return symbols.OrderBy(s => s.Address).ToList();
    }

    private SymbolType ParseSymbolType(string typeChar)
    {
        return typeChar.ToUpper() switch
        {
            "T" => SymbolType.Text,
            "D" => SymbolType.Data,
            "B" => SymbolType.Bss,
            "A" => SymbolType.Absolute,
            "U" => SymbolType.Undefined,
            _ => SymbolType.Unknown
        };
    }

    private Dictionary<string, string> ParseSectionDump(string objdumpOutput)
    {
        var sections = new Dictionary<string, string>();
        var lines = objdumpOutput.Split('\n');
        string? currentSection = null;
        var contentBuilder = new StringBuilder();

        foreach (var line in lines)
        {
            if (line.StartsWith("Contents of section "))
            {
                if (currentSection != null && contentBuilder.Length > 0)
                {
                    sections[currentSection] = contentBuilder.ToString();
                }
                
                currentSection = line.Substring("Contents of section ".Length).TrimEnd(':');
                contentBuilder.Clear();
            }
            else if (currentSection != null && line.Contains(' '))
            {
                contentBuilder.AppendLine(line);
            }
        }

        if (currentSection != null && contentBuilder.Length > 0)
        {
            sections[currentSection] = contentBuilder.ToString();
        }

        return sections;
    }

    private class ProcessResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
    }
}
