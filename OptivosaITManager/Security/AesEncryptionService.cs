using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace OptivosaITManager.Security;

public class EncryptionOptions
{
    /// <summary>Clave de 256 bits codificada en Base64. Debe venir de User Secrets/variables de entorno, nunca del repositorio.</summary>
    public string Key { get; set; } = string.Empty;
}

/// <summary>
/// Cifrado simétrico AES-256-GCM. Cada valor cifrado incluye un nonce aleatorio propio,
/// por lo que dos credenciales con la misma contraseña producen textos cifrados distintos.
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public AesEncryptionService(IOptions<EncryptionOptions> options)
    {
        var keyBase64 = options.Value.Key;
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException(
                "No se encontró la clave de cifrado (Encryption:Key). Configúrela mediante User Secrets en desarrollo " +
                "o mediante la variable de entorno Encryption__Key en producción. Consulte el README.");
        }

        try
        {
            _key = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Encryption:Key debe ser una cadena Base64 válida.", ex);
        }

        if (_key.Length != 32)
        {
            throw new InvalidOperationException("Encryption:Key debe representar 32 bytes (AES-256) en Base64.");
        }
    }

    public string Encrypt(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var result = new byte[NonceSize + TagSize + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, result, NonceSize + TagSize, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        ArgumentNullException.ThrowIfNull(cipherText);

        var data = Convert.FromBase64String(cipherText);
        if (data.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Formato de texto cifrado inválido.");
        }

        var nonce = data[..NonceSize];
        var tag = data[NonceSize..(NonceSize + TagSize)];
        var cipherBytes = data[(NonceSize + TagSize)..];
        var plainBytes = new byte[cipherBytes.Length];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}
