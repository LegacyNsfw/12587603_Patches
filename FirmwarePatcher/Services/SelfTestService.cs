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
}
