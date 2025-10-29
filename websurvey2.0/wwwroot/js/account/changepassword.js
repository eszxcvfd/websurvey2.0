(function () {
    const form = document.getElementById('pwdForm');
    const btn = document.getElementById('submitBtn');
    const btnText = document.getElementById('btnText');
    const btnSpinner = document.getElementById('btnSpinner');
    if (!form) return;

    function setLoading(b) {
        btn.disabled = b;
        btnText.classList.toggle('d-none', b);
        btnSpinner.classList.toggle('d-none', !b);
    }

    form.addEventListener('submit', async (e) => {
        if (!window.fetch) return;
        e.preventDefault();
        setLoading(true);
        try {
            const res = await fetch(form.action, {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: new FormData(form)
            });
            if (res.ok) {
                const data = await res.json();
                window.location.href = data?.redirectUrl || '/Account/Profile';
            } else {
                const data = await res.json();
                alert((data?.errors || ['Change password failed']).join('\n'));
                setLoading(false);
            }
        } catch {
            alert('Network error. Please try again later.');
            setLoading(false);
        }
    });
})();