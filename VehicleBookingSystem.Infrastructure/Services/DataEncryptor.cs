using System.Security.Cryptography;
using System.Text;

namespace VehicleBookingSystem.Infrastructure.Services;

/// <summary>AES-256-GCM authenticated encryption for JSON data files.</summary>
public class DataEncryptor
{
    private static readonly byte[] Salt = "HealthwiseFleet2024Salt"u8.ToArray();
    private readonly byte[] _key;

    public DataEncryptor(string passphrase)
    {
        _key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(passphrase),
            Salt, 100_000, HashAlgorithmName.SHA256, 32);
    }

    /// <summary>Encrypts UTF-8 JSON string ? [nonce(12)][tag(16)][ciphertext]</summary>
    public byte[] Encrypt(string plaintext)
    {
        var plain  = Encoding.UTF8.GetBytes(plaintext);
        var nonce  = new byte[12];
        var cipher = new byte[plain.Length];
        var tag    = new byte[16];
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plain, cipher, tag);

        var result = new byte[12 + 16 + cipher.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, 12);
        cipher.CopyTo(result, 28);
        return result;
    }

    /// <summary>Decrypts bytes produced by <see cref="Encrypt"/>.</summary>
    public string Decrypt(byte[] data)
    {
        var nonce  = data[..12];
        var tag    = data[12..28];
        var cipher = data[28..];
        var plain  = new byte[cipher.Length];

        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
