(function () {
    const btn = document.getElementById('copyUserIdBtn');
    const text = document.getElementById('userIdText');
    if (!btn || !text) return;

    btn.addEventListener('click', async () => {
        try {
            await navigator.clipboard.writeText(text.textContent || '');
            btn.textContent = 'Copied!';
            setTimeout(() => btn.textContent = 'Copy', 1500);
        } catch {
            const r = document.createRange();
            r.selectNode(text);
            const sel = window.getSelection();
            sel?.removeAllRanges();
            sel?.addRange(r);
            document.execCommand('copy');
            sel?.removeAllRanges();
            btn.textContent = 'Copied!';
            setTimeout(() => btn.textContent = 'Copy', 1500);
        }
    });
})();