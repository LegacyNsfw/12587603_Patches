: Note that you will need to remove the --no-restore argument the first time you run this

dotnet run --no-restore --project FirmwarePatcher -- --source patches.s --firmware %1 --output patched.bin --verbose