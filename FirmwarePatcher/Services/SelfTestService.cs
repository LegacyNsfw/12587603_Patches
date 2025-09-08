using Serilog;
using System.Collections.Generic;
using System.Reflection;

namespace FirmwarePatcher.Services;

public class SelfTestService
{
    private readonly ILogger _logger;
    private int _testsPassed = 0;
    private int _testsFailed = 0;

    public SelfTestService(ILogger logger)
    {
        _logger = logger;
    }

    public bool RunAllTests()
    {
        _logger.Information("=== Running Self Tests ===");

        try
        {
            TestExtractHexBytes();
            TestParseObjdumpLine();
            TestChecksumService();

            _logger.Information("=== Self Test Results ===");
            _logger.Information("Tests passed: {Passed}", _testsPassed);
            _logger.Information("Tests failed: {Failed}", _testsFailed);

            var success = _testsFailed == 0;
            if (success)
            {
                _logger.Information("All tests passed!");
            }
            else
            {
                _logger.Error("Some tests failed!");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Self test execution failed");
            return false;
        }
    }

    private void TestExtractHexBytes()
    {
        _logger.Information("Testing ExtractHexBytes method...");

        var extractor = new SectionExtractor(_logger);

        // Use reflection to access the private method
        var extractHexBytesMethod = typeof(SectionExtractor)
            .GetMethod("ExtractHexBytes", BindingFlags.NonPublic | BindingFlags.Instance);

        if (extractHexBytesMethod == null)
        {
            Assert("ExtractHexBytes method not found", false);
            return;
        }

        // Test case 1: Normal full line
        var result1 = extractHexBytesMethod.Invoke(extractor, new object[] { "1900028d 028d028d 028d028d 038d048d  ................" }) as List<byte>;
        var expected1 = new byte[] { 0x19, 0x00, 0x02, 0x8d, 0x02, 0x8d, 0x02, 0x8d, 0x02, 0x8d, 0x02, 0x8d, 0x03, 0x8d, 0x04, 0x8d };
        Assert("ExtractHexBytes - full line", result1 != null && ByteArraysEqual(result1.ToArray(), expected1));

        // Test case 2: Partial line (like the last line at a0020)
        var result2 = extractHexBytesMethod.Invoke(extractor, new object[] { "1234                                 .4" }) as List<byte>;
        var expected2 = new byte[] { 0x12, 0x34 };
        Assert("ExtractHexBytes - partial line", result2 != null && ByteArraysEqual(result2.ToArray(), expected2));

        // Test case 3: Multiple hex groups separated by spaces
        var result3 = extractHexBytesMethod.Invoke(extractor, new object[] { "4eb90008 0000 4e71  ..." }) as List<byte>;
        var expected3 = new byte[] { 0x4e, 0xb9, 0x00, 0x08, 0x00, 0x00, 0x4e, 0x71 };
        Assert("ExtractHexBytes - multiple groups", result3 != null && ByteArraysEqual(result3.ToArray(), expected3));

        // Test case 4: Empty or invalid input
        var result4 = extractHexBytesMethod.Invoke(extractor, new object[] { "                                     ................" }) as List<byte>;
        Assert("ExtractHexBytes - empty hex data", result4 != null && result4.Count == 0);
    }

    private void TestParseObjdumpLine()
    {
        _logger.Information("Testing ParseSectionDump method...");

        var extractor = new SectionExtractor(_logger);
        var addressBytes = new SortedDictionary<uint, byte>();

        // Use reflection to access the private method
        var parseSectionDumpMethod = typeof(SectionExtractor)
            .GetMethod("ParseSectionDump", BindingFlags.NonPublic | BindingFlags.Instance);

        if (parseSectionDumpMethod == null)
        {
            Assert("ParseSectionDump method not found", false);
            return;
        }

        // Test parsing the problematic lines from our actual objdump
        var testContent = @"
 9fff0 00000000 00000000 00000000 00000000  ................
 a0000 1900028d 028d028d 028d028d 038d048d  ................
 a0010 048d058d 058d058d 058d058d 058d058d  ................
 a0020 1234                                 .4              
";

        parseSectionDumpMethod.Invoke(extractor, new object[] { testContent, addressBytes });

        // Verify that we parsed the data correctly
        Assert("ParseSectionDump - a0000 first byte", addressBytes.ContainsKey(0xa0000) && addressBytes[0xa0000] == 0x19);
        Assert("ParseSectionDump - a0001 second byte", addressBytes.ContainsKey(0xa0001) && addressBytes[0xa0001] == 0x00);
        Assert("ParseSectionDump - a0020 first byte", addressBytes.ContainsKey(0xa0020) && addressBytes[0xa0020] == 0x12);
        Assert("ParseSectionDump - a0021 second byte", addressBytes.ContainsKey(0xa0021) && addressBytes[0xa0021] == 0x34);
        Assert("ParseSectionDump - a0022 should not exist", !addressBytes.ContainsKey(0xa0022));

        // Check total bytes in the a0000-a0021 range
        var bytesInRange = 0;
        for (uint addr = 0xa0000; addr <= 0xa0021; addr++)
        {
            if (addressBytes.ContainsKey(addr))
                bytesInRange++;
        }
        Assert("ParseSectionDump - total bytes parsed", bytesInRange == 34); // 16 + 16 + 2 = 34 bytes
    }

    private void Assert(string testName, bool condition)
    {
        if (condition)
        {
            _logger.Debug("✓ {TestName}", testName);
            _testsPassed++;
        }
        else
        {
            _logger.Error("✗ {TestName}", testName);
            _testsFailed++;
        }
    }

    private bool ByteArraysEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    private void TestChecksumService()
    {
        _logger.Information("Testing ChecksumService...");

        var checksumService = new ChecksumService(_logger);

        // Create a test firmware with known data
        var testFirmware = new byte[0x1000]; // 4KB test firmware

        // Fill with known pattern
        for (int i = 0; i < testFirmware.Length; i++)
        {
            testFirmware[i] = (byte)(i & 0xFF);
        }

        // Test the ComputeSegmentChecksum function directly
        var testSegment = new ChecksumService.ChecksumSegment
        {
            Index = 0,
            StartAddress = 0x100,
            EndAddress = 0x1FF
        };

        uint expectedChecksum = 49152;
        uint actualChecksum = checksumService.ComputeSegmentChecksum(testFirmware, testSegment);
        Assert($"ChecksumService segment checksum computation {actualChecksum}", actualChecksum == expectedChecksum);

        // Test with a different segment to ensure it's calculating correctly
        var testSegment2 = new ChecksumService.ChecksumSegment
        {
            Index = 1,
            StartAddress = 0x50,
            EndAddress = 0x5F
        };

        uint expectedChecksum2 = 47808;
        uint actualChecksum2 = checksumService.ComputeSegmentChecksum(testFirmware, testSegment2);
        Assert($"ChecksumService segment checksum computation (second test) {actualChecksum2}", actualChecksum2 == expectedChecksum2);

        // Test with real firmware file - KnownGood.bin
        TestChecksumServiceWithRealFirmware(checksumService);

        TestChecksumRepair(checksumService);
    }

    private void TestChecksumServiceWithRealFirmware(ChecksumService checksumService)
    {
        try
        {
            var knownGoodPath = Path.Combine("Resources", "KnownGood.bin");

            if (!File.Exists(knownGoodPath))
            {
                _logger.Warning("KnownGood.bin not found at {Path}, skipping real firmware test", knownGoodPath);
                Assert("ChecksumService real firmware test (file not found)", true); // Pass the test since file is optional
                return;
            }

            _logger.Information("Testing ChecksumService with real firmware: {Path}", knownGoodPath);

            // Use the async method to compute all checksums
            var firmware = File.ReadAllBytesAsync(knownGoodPath).GetAwaiter().GetResult();
            var segments = checksumService.ComputeAllChecksumsAsync(firmware);

            _logger.Information("Found {SegmentCount} segments in KnownGood.bin:", segments.Count);
            foreach (var segment in segments)
            {
                _logger.Information("  {Segment}", segment);
                Assert($"Segment from 0x{segment.StartAddress:X8} to 0x{segment.EndAddress:X8} has valid checksum",
                    segment.CalculatedChecksum == 0);
            }

            // Basic validation that we found some segments
            Assert("ChecksumService real firmware test (segments found)", segments.Count == 8);

            // Validate that all segments have reasonable values
            foreach (var segment in segments)
            {
                Assert($"ChecksumService real firmware segment {segment.Index} has valid addresses",
                    segment.StartAddress < segment.EndAddress);
                Assert($"ChecksumService real firmware segment {segment.Index} has non-zero size",
                    segment.Size > 0);
                // Note: We're not validating the actual checksum values yet, just that they computed
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to test ChecksumService with real firmware");
            Assert("ChecksumService real firmware test (exception)", false);
        }
    }

    private void TestChecksumRepair(ChecksumService checksumService)
    {
        try
        {
            var corruptedPath = Path.Combine("Resources", "BadChecksums.bin");
            var repairedPath = Path.Combine("Resources", "Repaired.bin");

            if (!File.Exists(corruptedPath))
            {
                _logger.Warning("BadChecksums.bin not found at {Path}, skipping checksum repair test", corruptedPath);
                Assert("Checksum repair test (file not found)", false); // Pass the test since file is optional
                return;
            }

            // Now use the ChecksumService to fix the checksums
            checksumService.FixChecksums(corruptedPath, repairedPath).GetAwaiter().GetResult();

            // Load the repaired firmware and verify checksums are now valid
            var repairedFirmware = File.ReadAllBytes(repairedPath);
            var repairedSegments = checksumService.ComputeAllChecksumsAsync(repairedFirmware);

            bool allValid = true;
            foreach (var segment in repairedSegments)
            {
                if (segment.CalculatedChecksum != 0)
                {
                    allValid = false;
                    _logger.Error("Segment {Index} still has invalid checksum: 0x{Checksum:X8}", segment.Index, segment.CalculatedChecksum);
                }
                else
                {
                    _logger.Information("Segment {Index} checksum successfully repaired", segment.Index);
                }
            }

            Assert("Checksum repair test", allValid);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to test checksum repair");
            Assert("Checksum repair test (exception)", false);
        }
    }
}
