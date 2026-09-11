using QRCoder;

namespace OptivosaITManager.Services;

public class QrCodeService : IQrCodeService
{
    public string GenerateSvg(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var svgQr = new SvgQRCode(data);

        // SizingMode.ViewBoxAttribute: el SVG resultante solo trae "viewBox" (sin width/height
        // fijos en píxeles). Con eso, cuando el CSS escala el SVG con width:100%/height:100% en
        // un contenedor, el navegador reescala TODO el contenido interno de forma proporcional
        // (usa el viewBox como sistema de coordenadas) en vez de recortarlo: con width/height
        // fijos el contenedor solo cambia el "viewport" y el dibujo original se recorta si el
        // contenedor es más pequeño que esas dimensiones originales. drawQuietZones:true asegura
        // el margen blanco alrededor del QR requerido para que los lectores lo puedan enfocar.
        return svgQr.GetGraphic(10, "#000000", "#FFFFFF", drawQuietZones: true,
            sizingMode: SvgQRCode.SizingMode.ViewBoxAttribute);
    }
}
