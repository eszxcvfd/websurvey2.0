(function () {
    const form = document.getElementById('createForm');
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
        if (!window.fetch) return; // fallback normal post
        e.preventDefault();
        setLoading(true);

        try {
            const res = await fetch(form.action, {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: new FormData(form)
            });

            const data = await res.json();
            if (res.ok) {
                window.location.href = data?.redirectUrl || '/';
            } else {
                alert((data?.errors || ['Create survey failed']).join('\n'));
                setLoading(false);
            }
        } catch (e) {
            console.error(e);
            alert('Network error. Please try again later.');
            setLoading(false);
        }
    });
})();