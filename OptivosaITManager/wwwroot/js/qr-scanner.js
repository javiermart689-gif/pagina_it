// Escaneo de QR con la cámara del navegador (jsQR) para la pantalla "Registrar / Recibir equipo".
// El QR de un equipo solo contiene una ruta interna (/q/{token}); este script valida que el
// contenido decodificado sea del propio sitio y tenga esa forma antes de navegar, para que un
// QR ajeno o manipulado no pueda redirigir al técnico a un sitio externo (phishing).
(function () {
    var root = document.getElementById('qrScannerRoot');
    if (!root || typeof jsQR === 'undefined') return;

    var video = document.getElementById('qrScannerVideo');
    var videoWrap = video ? video.closest('.qr-scanner-video-wrap') : null;
    var statusEl = document.getElementById('qrScannerStatus');
    var startBtn = document.getElementById('qrScannerStart');
    var stopBtn = document.getElementById('qrScannerStop');
    var scanPrefix = root.getAttribute('data-scan-target-prefix') || '/q/';

    var canvas = document.createElement('canvas');
    var ctx = canvas.getContext('2d', { willReadFrequently: true });

    var stream = null;
    var rafId = null;
    var scanning = false;

    function setStatus(text) {
        if (statusEl) statusEl.textContent = text;
    }

    // Solo se acepta una URL del propio origen cuya ruta empiece con el prefijo de escaneo
    // (por defecto "/q/"), seguida de un identificador. Cualquier otra cosa (otro dominio,
    // "javascript:", una ruta distinta) se rechaza sin navegar.
    function resolveInternalScanUrl(decodedText) {
        var url;
        try {
            url = new URL(decodedText, window.location.origin);
        } catch (e) {
            return null;
        }

        if (url.origin !== window.location.origin) return null;

        var prefixPath = new URL(scanPrefix, window.location.origin).pathname;
        if (!url.pathname.startsWith(prefixPath)) return null;

        var token = url.pathname.slice(prefixPath.length);
        if (!token || !/^[A-Za-z0-9]+$/.test(token)) return null;

        return url.pathname + url.search;
    }

    function stopScanning() {
        scanning = false;
        if (rafId) cancelAnimationFrame(rafId);
        rafId = null;
        if (stream) {
            stream.getTracks().forEach(function (t) { t.stop(); });
            stream = null;
        }
        if (video) video.srcObject = null;
        if (videoWrap) videoWrap.hidden = true;
        if (startBtn) startBtn.hidden = false;
        if (stopBtn) stopBtn.hidden = true;
    }

    function tick() {
        if (!scanning) return;

        if (video.readyState === video.HAVE_ENOUGH_DATA) {
            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;
            ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
            var imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
            var code = jsQR(imageData.data, imageData.width, imageData.height);

            if (code && code.data) {
                var target = resolveInternalScanUrl(code.data);
                if (target) {
                    setStatus('Equipo identificado. Abriendo ficha...');
                    stopScanning();
                    window.location.href = target;
                    return;
                }
                setStatus('El código escaneado no corresponde a un equipo de este sistema. Siga intentando o use la búsqueda manual.');
            }
        }

        rafId = requestAnimationFrame(tick);
    }

    async function startScanning() {
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            setStatus('Este navegador no admite acceso a la cámara. Use la búsqueda manual.');
            return;
        }
        if (!window.isSecureContext) {
            setStatus('El escaneo con cámara requiere una conexión segura (HTTPS). Use la búsqueda manual.');
            return;
        }

        try {
            stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } });
        } catch (e) {
            setStatus('No se pudo acceder a la cámara (permiso denegado o no disponible). Use la búsqueda manual.');
            return;
        }

        video.srcObject = stream;
        await video.play();

        if (videoWrap) videoWrap.hidden = false;
        if (startBtn) startBtn.hidden = true;
        if (stopBtn) stopBtn.hidden = false;
        setStatus('Apunte la cámara al código QR del equipo.');

        scanning = true;
        rafId = requestAnimationFrame(tick);
    }

    if (startBtn) startBtn.addEventListener('click', startScanning);
    if (stopBtn) stopBtn.addEventListener('click', function () {
        stopScanning();
        setStatus('Cámara detenida.');
    });

    window.addEventListener('pagehide', stopScanning);
})();
