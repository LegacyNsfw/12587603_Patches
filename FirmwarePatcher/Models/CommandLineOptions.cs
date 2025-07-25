using CommandLine;

namespace FirmwarePatcher.Models;

public class CommandLineOptions
{
    [Option('s', "source", Required = false, HelpText = "Assembly source file (.s)")]
    public string? SourceFile { get; set; }

    [Option('f', "firmware", Required = false, HelpText = "Input firmware binary file")]
    public string? FirmwareFile { get; set; }

    [Option('o', "output", Required = false, HelpText = "Output patched firmware file")]
    public string? OutputFile { get; set; }

    [Option('t', "toolchain", Required = false, HelpText = "Path to m68k toolchain (default: system PATH)")]
    public string? ToolchainPath { get; set; }

    [Option('v', "verify", Required = false, Default = true, HelpText = "Verify patches after application")]
    public bool Verify { get; set; }

    [Option('b', "backup", Required = false, Default = true, HelpText = "Create backup of original firmware")]
    public bool CreateBackup { get; set; }

    [Option("verbose", Required = false, Default = false, HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; }

    [Option('r', "report", Required = false, HelpText = "Generate detailed patch report file")]
    public string? ReportFile { get; set; }

    [Option("selftest", Required = false, Default = false, HelpText = "Run internal unit tests and exit")]
    public bool SelfTest { get; set; }
}
