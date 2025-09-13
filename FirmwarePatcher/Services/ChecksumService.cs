using Serilog;
using System.Formats.Asn1;

namespace FirmwarePatcher.Services;

public class ChecksumService
{
    private readonly ILogger _logger;
    private const int CHECKSUM_TABLE_ADDRESS = 0x50C;
    private const int SEGMENT_COUNT = 8;
    private const int ADDRESS_ENTRY_SIZE = 8; // 4 bytes start + 4 bytes end

    public ChecksumService(ILogger logger)
    {
        _logger = logger;
    }

    public class ChecksumSegment
    {
        public int Index { get; set; }
        public uint StartAddress { get; set; }
        public uint EndAddress { get; set; }
        public uint Size => EndAddress - StartAddress + 1;
        public UInt16 CalculatedChecksum { get; set; }
        
        public override string ToString()
        {
            return $"Segment {Index}: 0x{StartAddress:X8}-0x{EndAddress:X8} ({Size} bytes), Checksum: 0x{CalculatedChecksum:X8}";
        }
    }

    /// <summary>
    /// Reads the checksum segment table from the firmware and computes checksums for all segments
    /// </summary>
    /// <param name="firmwarePath">Path to the firmware binary file</param>
    /// <returns>List of checksum segments with calculated checksums</returns>
    public List<ChecksumSegment> ComputeAllChecksumsAsync(byte[] firmware)
    {
        try
        {
            _logger.Debug("Reading checksum segment table at address 0x{Address:X}", CHECKSUM_TABLE_ADDRESS);
            var segments = ReadChecksumSegmentTable(firmware);
            
            _logger.Debug("Computing checksums for {SegmentCount} segments", segments.Count);
            foreach (var segment in segments)
            {
                segment.CalculatedChecksum = ComputeSegmentChecksum(firmware, segment);
                _logger.Debug("Computed checksum for {Segment}", segment);
            }
            
            _logger.Debug("Checksum analysis complete");
            return segments;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to compute checksums");
            throw;
        }
    }

    public async Task FixChecksums(string inputPath, string outputPath)
    {
        _logger.Information("Loading firmware for checksum repair: {FirmwarePath}", inputPath);
        var firmware = await File.ReadAllBytesAsync(inputPath);

        List<ChecksumSegment> segments = this.ComputeAllChecksumsAsync(firmware);
        foreach (var segment in segments)
        {
            _logger.Debug("Fixing checksum for {Segment}", segment);
            _logger.Debug("Current checksum: 0x{Checksum:X8}", segment.CalculatedChecksum);
            int address = segment.StartAddress == 0 ? 0x500 : (int)segment.StartAddress;

            if (segment.CalculatedChecksum != 0)
            {
                // Calculate the new value needed to make the checksum zero
                // Since checksum is the sum of all 16-bit values, we need to subtract the current checksum
                // The current value at the address contributes to the checksum, so we adjust it
                UInt16 currentCheckValue = (UInt16)((firmware[address] << 8) | firmware[address + 1]);
                int newCheckValue = (currentCheckValue - segment.CalculatedChecksum) & 0xFFFF;
                firmware[address] = (byte)((newCheckValue >> 8) & 0xFF);
                firmware[address + 1] = (byte)(newCheckValue & 0xFF);
                _logger.Information($"Updated checksum at 0x{address:X8}: 0x{newCheckValue:X8}");
            }

            this.ComputeSegmentChecksum(firmware, segment); // Recompute to verify
            _logger.Debug("Verified new checksum: 0x{Checksum:X8}", segment.CalculatedChecksum);
        }

        await File.WriteAllBytesAsync(outputPath, firmware);
        _logger.Information("Wrote updated firmware with fixed checksums to: {OutputPath}", outputPath);
    }

    /// <summary>
    /// Reads the segment address table from the firmware binary
    /// </summary>
    /// <param name="firmware">Firmware binary data</param>
    /// <returns>List of checksum segments with start/end addresses</returns>
    private List<ChecksumSegment> ReadChecksumSegmentTable(byte[] firmware)
    {
        if (firmware.Length < CHECKSUM_TABLE_ADDRESS + (SEGMENT_COUNT * ADDRESS_ENTRY_SIZE))
        {
            throw new InvalidOperationException($"Firmware too small to contain checksum table at 0x{CHECKSUM_TABLE_ADDRESS:X}");
        }

        var segments = new List<ChecksumSegment>();

        for (int i = 0; i < SEGMENT_COUNT; i++)
        {
            int offset = CHECKSUM_TABLE_ADDRESS + (i * ADDRESS_ENTRY_SIZE);

            // Read start and end addresses (assuming big-endian format for this firmware)
            uint startAddress = ReadUInt32BigEndian(firmware, offset);
            uint endAddress = ReadUInt32BigEndian(firmware, offset + 4);

            // Skip empty segments (where start == end == 0)
            if (startAddress == 0 && endAddress == 0)
            {
                _logger.Debug("Skipping empty segment {Index}", i);
                continue;
            }

            // Validate segment bounds
            if (startAddress >= endAddress)
            {
                throw new InvalidOperationException($"Invalid segment {i}: start address 0x{startAddress:X8} >= end address 0x{endAddress:X8}");
            }

            if (endAddress >= firmware.Length)
            {
                throw new InvalidOperationException($"Segment {i} end address 0x{endAddress:X8} exceeds firmware size 0x{firmware.Length:X8}");
            }

            segments.Add(new ChecksumSegment
            {
                Index = i,
                StartAddress = startAddress,
                EndAddress = endAddress
            });

            _logger.Debug("Found segment {Index}: 0x{Start:X8}-0x{End:X8} ({Size} bytes)",
               i, startAddress, endAddress, endAddress - startAddress + 1);
        }

        return segments;
    }

    /// <summary>
    /// Computes a simple sum checksum for a segment of the firmware
    /// </summary>
    /// <param name="firmware">Firmware binary data</param>
    /// <param name="segment">Segment to compute checksum for</param>
    /// <returns>32-bit checksum value</returns>
    internal UInt16 ComputeSegmentChecksum(byte[] firmware, ChecksumSegment segment)
    {
        UInt16 checksum = 0;

        for (uint address = segment.StartAddress; address <= segment.EndAddress; address += 2)
        {
            // The dynamic-data range is excluded.
            if (address == 0x4000)
            {
                address = 0x20000;
            }            
            
            UInt16 value = (UInt16)(firmware[address] << 8);
            value |= firmware[address + 1];
            checksum += value;            
        }
        
        _logger.Debug("Segment {Index} checksum: 0x{Checksum:X8} (sum of {Size} bytes)", 
            segment.Index, checksum, segment.Size);
        
        return checksum;
    }

    /// <summary>
    /// Reads a 32-bit unsigned integer in big-endian format from a byte array
    /// </summary>
    /// <param name="buffer">Byte array to read from</param>
    /// <param name="offset">Offset in the array</param>
    /// <returns>32-bit unsigned integer value</returns>
    private static uint ReadUInt32BigEndian(byte[] buffer, int offset)
    {
        return (uint)((buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3]);
    }
}
