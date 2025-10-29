(function () {
    const form = document.getElementById('profileForm');
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
                alert('Profile updated.');
                location.reload();
            } else {
                const data = await res.json();
                alert((data?.errors || ['Update failed']).join('\n'));
            }
        } catch {
            alert('Network error. Please try again later.');
        } finally {
            setLoading(false);
        }
    });
})();