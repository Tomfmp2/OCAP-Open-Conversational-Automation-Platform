using System.Security.Cryptography;
using System.Text;

namespace OCAP.Security.Infrastructure.Services;

// Implementación de TOTP (Time-Based One-Time Password Algorithm) conforme a RFC 6238 / RFC 4226.
public class TotpService : OCAP.Security.Abstractions.ITotpService
{
    private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public string GenerateSecretKey()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(20); // 160 bits secret
        return EncodeBase32(bytes);
    }

    public string GenerateQrCodeUri(string userEmail, string secret, string issuer = "OCAP")
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(userEmail);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
    }

    public bool ValidateCode(string secret, string code, int timeStepSeconds = 30)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code)) return false;
        var cleanCode = code.Trim().Replace(" ", string.Empty);
        if (cleanCode.Length != 6 || !int.TryParse(cleanCode, out _)) return false;

        byte[] secretBytes;
        try
        {
            secretBytes = DecodeBase32(secret);
        }
        catch
        {
            return false;
        }

        long currentTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long currentCounter = currentTimeSeconds / timeStepSeconds;

        // Tolerancia de ±1 ventana temporal (30s atrasado, actual, 30s adelantado) para deriva de reloj.
        for (int i = -1; i <= 1; i++)
        {
            string generatedCode = GenerateTotp(secretBytes, currentCounter + i);
            if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(generatedCode), Encoding.UTF8.GetBytes(cleanCode)))
            {
                return true;
            }
        }

        return false;
    }

    private static string GenerateTotp(byte[] secretBytes, long counter)
    {
        byte[] counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        byte[] hash;
        using (var hmac = new HMACSHA1(secretBytes))
        {
            hash = hmac.ComputeHash(counterBytes);
        }

        int offset = hash[hash.Length - 1] & 0x0F;
        int binaryCode = ((hash[offset] & 0x7F) << 24)
                       | ((hash[offset + 1] & 0xFF) << 16)
                       | ((hash[offset + 2] & 0xFF) << 8)
                       | (hash[offset + 3] & 0xFF);

        int otp = binaryCode % 1000000;
        return otp.ToString("D6");
    }

    private static string EncodeBase32(byte[] bytes)
    {
        var sb = new StringBuilder((bytes.Length * 8 + 4) / 5);
        int bitBuffer = 0;
        int bitsInBuffer = 0;

        foreach (byte b in bytes)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitsInBuffer += 8;
            while (bitsInBuffer >= 5)
            {
                int index = (bitBuffer >> (bitsInBuffer - 5)) & 0x1F;
                sb.Append(Base32Chars[index]);
                bitsInBuffer -= 5;
            }
        }

        if (bitsInBuffer > 0)
        {
            int index = (bitBuffer << (5 - bitsInBuffer)) & 0x1F;
            sb.Append(Base32Chars[index]);
        }

        return sb.ToString();
    }

    private static byte[] DecodeBase32(string base32)
    {
        string sanitized = base32.Trim().ToUpperInvariant().Replace("=", string.Empty);
        var bytes = new List<byte>();
        int bitBuffer = 0;
        int bitsInBuffer = 0;

        foreach (char c in sanitized)
        {
            int val = Base32Chars.IndexOf(c);
            if (val < 0) throw new FormatException($"Carácter Base32 no válido: '{c}'.");

            bitBuffer = (bitBuffer << 5) | val;
            bitsInBuffer += 5;

            if (bitsInBuffer >= 8)
            {
                bytes.Add((byte)((bitBuffer >> (bitsInBuffer - 8)) & 0xFF));
                bitsInBuffer -= 8;
            }
        }

        return bytes.ToArray();
    }
}
