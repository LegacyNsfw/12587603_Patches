namespace FirmwarePatcher.Utilities;

public static class AddressCalculator
{
    public static bool IsValidAddress(uint address)
    {
        // Basic validation - address should not be zero for most use cases
        return address > 0;
    }

    public static bool AddressRangesOverlap(uint start1, uint end1, uint start2, uint end2)
    {
        return start1 < end2 && start2 < end1;
    }

    public static uint AlignAddress(uint address, uint alignment)
    {
        return (address + alignment - 1) & ~(alignment - 1);
    }

    public static bool IsAddressInRange(uint address, uint rangeStart, uint rangeEnd)
    {
        return address >= rangeStart && address < rangeEnd;
    }

    public static uint CalculateDistance(uint address1, uint address2)
    {
        return address1 > address2 ? address1 - address2 : address2 - address1;
    }

    public static string FormatAddress(uint address)
    {
        return $"0x{address:X8}";
    }

    public static uint ParseAddress(string addressString)
    {
        if (addressString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            addressString = addressString.Substring(2);
        }
        
        return uint.Parse(addressString, System.Globalization.NumberStyles.HexNumber);
    }
}
