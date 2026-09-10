// Revelar contraseña de una credencial de forma temporal (consulta auditada en el servidor).
document.addEventListener('click', async function (e) {
    const revealBtn = e.target.closest('[data-reveal-credential]');
    if (revealBtn) {
        e.preventDefault();
        const credentialId = revealBtn.getAttribute('data-reveal-credential');
        const target = document.querySelector(`[data-password-slot="${credentialId}"]`);
        if (!target) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const response = await fetch(`/Credentials/RevealPassword/${credentialId}`, {
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

    const copyBtn = e.target.closest('[data-copy-credential]');
    if (copyBtn) {
        e.preventDefault();
        const credentialId = copyBtn.getAttribute('data-copy-credential');
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        try {
            const response = await fetch(`/Credentials/CopyPassword/${credentialId}`, {
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
