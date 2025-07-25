dotnet run --project FirmwarePatcher -- --source test_patches.s --firmware empty.bin --output test_output.bin --verbose
fc /b test_output.bin test_expected.bin