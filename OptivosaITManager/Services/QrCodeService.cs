using QRCoder;

namespace OptivosaITManager.Services;

public class QrCodeService : IQrCodeService
{
    public string GenerateSvg(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var svgQr = new SvgQRCode(data);
        return svgQr.GetGraphic(6);
    }
}
