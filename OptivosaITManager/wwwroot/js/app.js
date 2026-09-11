// Revelar/copiar una contraseña de forma temporal (consulta auditada en el servidor).
// Funciona para cualquier entidad con contraseña (Credential, EmailAccount, ...): cada
// botón indica su propia URL vía data-reveal-url / data-copy-url y a qué slot de la
// página debe escribir el valor revelado vía data-reveal-target.
document.addEventListener('click', async function (e) {
    const revealBtn = e.target.closest('[data-reveal-url]');
    if (revealBtn) {
        e.preventDefault();
        const url = revealBtn.getAttribute('data-reveal-url');
        const target = document.querySelector(`[data-password-slot="${revealBtn.getAttribute('data-reveal-target')}"]`);
        if (!target) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token || '' }
            });

            if (!response.ok) {
                target.textContent = 'No autorizado';
                return;
            }

            const data = await response.json();
            target.textContent = data.password;
            revealBtn.textContent = 'Ocultar';
            revealBtn.setAttribute('data-revealed', 'true');

            setTimeout(() => {
                target.textContent = '••••••••';
                revealBtn.textContent = 'Mostrar contraseña';
                revealBtn.removeAttribute('data-revealed');
            }, 20000);
        } catch (err) {
            target.textContent = 'Error al consultar';
        }
        return;
    }

    // Copia directa de texto ya visible en pantalla (p. ej. número de servicio, teléfono de
    // soporte): a diferencia de data-copy-url, no hay nada sensible que desencriptar ni
    // auditar, así que copia de inmediato en el navegador sin llamar al servidor.
    const copyTextBtn = e.target.closest('[data-copy-text]');
    if (copyTextBtn) {
        e.preventDefault();
        const text = copyTextBtn.getAttribute('data-copy-text');
        try {
            await navigator.clipboard.writeText(text);
            const original = copyTextBtn.textContent;
            copyTextBtn.textContent = 'Copiado';
            setTimeout(() => { copyTextBtn.textContent = original; }, 2000);
        } catch (err) {
            // Silencioso: si el navegador bloquea el portapapeles no se registra nada indebido.
        }
        return;
    }

    const copyBtn = e.target.closest('[data-copy-url]');
    if (copyBtn) {
        e.preventDefault();
        const url = copyBtn.getAttribute('data-copy-url');
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token || '' }
            });
            if (!response.ok) return;
            const data = await response.json();
            await navigator.clipboard.writeText(data.password);

            const original = copyBtn.textContent;
            copyBtn.textContent = 'Copiado';
            setTimeout(() => { copyBtn.textContent = original; }, 2000);
        } catch (err) {
            // Silencioso: si el navegador bloquea el portapapeles no se registra nada indebido.
        }
    }
});

// ============================================================
// Transición visual Login -> Dashboard.
// NO cambia la autenticación: el formulario de login sigue siendo un POST normal
// del navegador (nunca se llama a preventDefault sobre el submit real), con lo cual
// las cookies, la sesión, los claims y las redirecciones existentes no se tocan.
// Esto son dos cargas de página reales; se simula una transición continua entre
// ambas mostrando un overlay de branding mientras el navegador procesa la
// navegación, y consumiendo un flag en sessionStorage (aislado por pestaña) para
// animar la entrada del contenido principal en la página siguiente, una sola vez.
// ============================================================
(function () {
    var TRANSITION_FLAG = 'optivosaAuthTransition';
    var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    var loginForm = document.querySelector('.login-card form');
    if (loginForm) {
        loginForm.addEventListener('submit', function (e) {
            if (loginForm.dataset.submitting === 'true') {
                e.preventDefault();
                return;
            }
            loginForm.dataset.submitting = 'true';

            var submitBtn = loginForm.querySelector('button[type=submit]');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = 'Iniciando sesión...' + (reduceMotion ? '' : ' <span class="btn-login-spinner"></span>');
            }

            try {
                sessionStorage.setItem(TRANSITION_FLAG, '1');
            } catch (err) {
                // Si sessionStorage no está disponible (modo privado estricto, etc.) simplemente
                // no habrá animación de entrada en la página siguiente; el login sigue funcionando.
            }

            if (!reduceMotion) {
                var overlay = document.createElement('div');
                overlay.className = 'auth-transition-overlay';
                overlay.setAttribute('aria-hidden', 'true');
                overlay.innerHTML =
                    '<div class="auth-transition-content">' +
                    '<img src="/images/optivosa-logo-white-mark.png" alt="" class="auth-transition-logo" />' +
                    '<div class="auth-transition-tagline">TI Manager</div>' +
                    '<div class="auth-transition-check"><svg viewBox="0 0 24 24"><polyline points="4,13 9,18 20,6"></polyline></svg></div>' +
                    '</div>';
                document.body.appendChild(overlay);
                requestAnimationFrame(function () {
                    overlay.classList.add('is-visible');
                });
            }
            // El submit no se cancela: el formulario continúa enviándose normalmente. Si el
            // login falla, el servidor vuelve a renderizar Login (página nueva, sin overlay ni
            // botón deshabilitado); si tiene éxito, la siguiente página consume el flag de abajo.
        });
    }

    var justLoggedIn = false;
    try {
        justLoggedIn = sessionStorage.getItem(TRANSITION_FLAG) === '1';
        if (justLoggedIn) sessionStorage.removeItem(TRANSITION_FLAG);
    } catch (err) {
        // Sin sessionStorage no hay forma de saber si venimos de un login exitoso: se omite
        // la animación de entrada, sin afectar el resto de la aplicación.
    }

    if (justLoggedIn && !reduceMotion) {
        var content = document.querySelector('.app-content');
        if (content) {
            content.classList.add('is-entering');
            content.addEventListener('animationend', function () {
                content.classList.remove('is-entering');
            }, { once: true });
        }
    }
})();
