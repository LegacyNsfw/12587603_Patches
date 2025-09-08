: Note that you will need to remove the --no-restore argument the first time you run this

dotnet run --no-restore --project FirmwarePatcher -- --source test_patches.s --firmware empty.bin --output test_output.bin --verbose
fc /b test_output.bin test_expected.bin