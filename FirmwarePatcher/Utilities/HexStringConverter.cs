namespace FirmwarePatcher.Utilities;

public static class HexStringConverter
{
    public static string ToHexString(byte[] data, int maxLength = 32)
    {
        if (data.Length <= maxLength)
        {
            return Convert.ToHexString(data);
        }
        
        var prefix = Convert.ToHexString(data.Take(maxLength / 2).ToArray());
        var suffix = Convert.ToHexString(data.TakeLast(maxLength / 2).ToArray());
        return $"{prefix}...{suffix}";
    }

    public static byte[] FromHexString(string hexString)
    {
        if (hexString.Length % 2 != 0)
        {
            throw new ArgumentException("Hex string must have even length");
        }

        var bytes = new byte[hexString.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        }
        
        return bytes;
    }

    public static string FormatHexDump(byte[] data, uint baseAddress = 0, int bytesPerLine = 16)
    {
        var result = new System.Text.StringBuilder();
        
        for (int i = 0; i < data.Length; i += bytesPerLine)
        {
            var address = baseAddress + (uint)i;
            result.Append($"{address:X8}: ");
            
            // Hex bytes
            for (int j = 0; j < bytesPerLine; j++)
            {
                if (i + j < data.Length)
                {
                    result.Append($"{data[i + j]:X2} ");
                }
                else
                {
                    result.Append("   ");
                }
            }
            
            result.Append(" ");
            
            // ASCII representation
            for (int j = 0; j < bytesPerLine && i + j < data.Length; j++)
            {
                var b = data[i + j];
                result.Append(b >= 32 && b <= 126 ? (char)b : '.');
            }
            
            result.AppendLine();
        }
        
        return result.ToString();
    }
}
