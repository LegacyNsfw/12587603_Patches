# This whole thing was vibe-coded

I don't trust it yet, and you shouldn't either.

But it's getting there, which is kind of amazing.

# Firmware Patcher

A C# tool for applying assembly patches to binary firmware images using label-based metadata extraction.

## Features

- **Automatic patch detection** using `PATCH_*_START` and `PATCH_*_END` labels
- **M68K CPU32 support** via GNU toolchain integration  
- **Label-based metadata** - no manual size calculations needed
- **Patch verification** and detailed reporting
- **Backup creation** for safety

## Features That Were Removed

Claude thought these were good ideas. This is why vibe coding is equally horrifying and fascinating:

- **Automatic firmware extension** for patches larger than the original firmware
- **Heuristics** - if required data was missing or invalid, it would just choose a number and keep going

## Prerequisites

- .NET 8.0 SDK
- M68K GNU toolchain (`m68k-elf-as`, `m68k-elf-nm`, `m68k-elf-objdump`)

## Building

```bash
cd FirmwarePatcher
dotnet build
```

## Usage

```bash
# Basic usage
dotnet run -- --source patches.s --firmware original.bin --output patched.bin

# With full options
dotnet run -- \
  --source patches.s \
  --firmware original.bin \
  --output patched.bin \
  --toolchain /path/to/m68k-tools \
  --verify \
  --backup \
  --report patch_report.txt \
  --verbose
```

## Assembly File Format

Your assembly file must use label pairs to define patches:

```gas
# Specify CPU32 instruction set
.cpu cpu32

###############################################################################
# Patch 1: Hook at original code location
PATCH_EOIT_HOOK_START:
        .org    0x32778
        jsr     LowMafEoitPatch
        nop
PATCH_EOIT_HOOK_END:

###############################################################################  
# Patch 2: New code implementation
PATCH_LOW_MAF_CODE_START:
        .org    0x80000
LowMafEoitPatch:
        # Your code here
        rts
PATCH_LOW_MAF_CODE_END:

###############################################################################
# Patch 3: Data tables
PATCH_LOW_MAF_DATA_START:
        .org    0xa0000
        .word   0x1900
        .word   0x28d
        # More data...
PATCH_LOW_MAF_DATA_END:
```

## How It Works

1. **Assembly**: Assembles your source file using `m68k-elf-as`
2. **Symbol Extraction**: Uses `m68k-elf-nm` to get symbol addresses
3. **Patch Identification**: Finds `PATCH_*_START/END` label pairs
4. **Data Extraction**: Uses `m68k-elf-objdump` to extract binary data
5. **Target Resolution**: Determines target addresses from `.org` directives
6. **Application**: Applies patches to firmware at resolved addresses
7. **Verification**: Optionally verifies patches were applied correctly

## Command Line Options

| Option | Description |
|--------|-------------|
| `-s, --source` | Assembly source file (.s) **[Required]** |
| `-f, --firmware` | Input firmware binary file **[Required]** |  
| `-o, --output` | Output patched firmware file **[Required]** |
| `-t, --toolchain` | Path to m68k toolchain directory |
| `-v, --verify` | Verify patches after application (default: true) |
| `-b, --backup` | Create backup of original firmware (default: true) |
| `--verbose` | Enable verbose logging |
| `-r, --report` | Generate detailed patch report file |

## Output

The tool generates:
- **Patched firmware** at the specified output location
- **Backup file** (if enabled) with timestamp
- **Console log** showing detailed progress
- **Patch report** (if requested) with detailed information

## Error Handling

The tool validates:
- ✅ Assembly file syntax and required labels
- ✅ Toolchain availability and functionality  
- ✅ Firmware file accessibility
- ✅ Patch address ranges and overlaps
- ✅ Successful patch application

## Example Output

```
Firmware Patcher v1.0
Source: patches.s
Firmware: original.bin
Output: patched.bin

=== Validation Phase ===
[INFO] Toolchain validation passed
[INFO] Assembly file validation passed

=== Assembly Phase ===  
[INFO] Assembling patches.s to patches.o
[INFO] Assembly successful: patches.o

=== Analysis Phase ===
[INFO] Found 15 symbols
[INFO] Identified 3 patch sections:
  EOIT_HOOK: 0x00032778 (8 bytes)
  LOW_MAF_CODE: 0x00080000 (24 bytes)  
  LOW_MAF_DATA: 0x000A0000 (32 bytes)

=== Data Extraction Phase ===
[INFO] Patch EOIT_HOOK: 8 bytes -> 0x32778
[INFO] Patch LOW_MAF_CODE: 24 bytes -> 0x80000
[INFO] Patch LOW_MAF_DATA: 32 bytes -> 0xA0000

=== Patch Application Phase ===
[INFO] Loading firmware: original.bin
[INFO] Extending firmware from 524288 to 655392 bytes
[INFO] Applied patch EOIT_HOOK at 0x32778 (8 bytes)
[INFO] Applied patch LOW_MAF_CODE at 0x80000 (24 bytes)
[INFO] Applied patch LOW_MAF_DATA at 0xA0000 (32 bytes)

=== Verification Phase ===
[INFO] Patch EOIT_HOOK verified successfully
[INFO] Patch LOW_MAF_CODE verified successfully  
[INFO] Patch LOW_MAF_DATA verified successfully

=== SUCCESS ===
[INFO] Successfully applied 3 patches to firmware
[INFO] Output written to: patched.bin
```
