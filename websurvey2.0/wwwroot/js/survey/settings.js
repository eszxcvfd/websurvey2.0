(function () {
    const form = document.getElementById('settingsForm');
    const btn = document.getElementById('saveBtn');
    const btnText = document.getElementById('btnText');
    const btnSpinner = document.getElementById('btnSpinner');
    if (!form) return;

    function setLoading(b) {
        btn.disabled = b;
        btnText.classList.toggle('d-none', b);
        btnSpinner.classList.toggle('d-none', !b);
    }

    form.addEventListener('submit', async (e) => {
        if (!window.fetch) return; // fallback normal post
        e.preventDefault();
        setLoading(true);

        try {
            const res = await fetch(form.action, {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: new FormData(form)
            });
            if (res.ok) {
                alert('Settings updated.');
                location.reload();
            } else {
                const data = await res.json();
                alert((data?.errors || ['Update settings failed']).join('\n'));
                setLoading(false);
            }
        } catch {
            alert('Network error. Please try again later.');
            setLoading(false);
        }
    });
})();