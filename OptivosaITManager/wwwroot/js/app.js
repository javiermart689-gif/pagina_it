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
