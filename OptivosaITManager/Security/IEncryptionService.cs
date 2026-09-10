namespace OptivosaITManager.Security;

/// <summary>
/// Cifrado reversible para credenciales que deben poder mostrarse a usuarios autorizados.
/// No usar para contraseñas de inicio de sesión de la aplicación (esas usan hashing vía Identity).
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
