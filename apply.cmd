: Note that you will need to remove the --no-restore argument the first time you run this
:
: %1 = original firmware file, which will be patched
: %2 = optional additional arguments to pass to the patcher (consider --verbose)
dotnet run --no-restore --project FirmwarePatcher -- --source patches.s --firmware %1 --output patched.bin %2