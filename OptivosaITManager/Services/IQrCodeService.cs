namespace OptivosaITManager.Services;

/// <summary>
/// Genera códigos QR como SVG (sin dependencias nativas ni de System.Drawing, para que
/// funcione igual en Windows Server/IIS que en contenedores Linux). El QR nunca debe
/// codificar más que una URL/token de localización: jamás contraseñas, correos ni usuarios.
/// </summary>
public interface IQrCodeService
{
    /// <summary>Devuelve el marcado SVG (autocontenible) del código QR para el texto/URL dado.</summary>
    string GenerateSvg(string content);
}
