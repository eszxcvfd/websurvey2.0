(function () {
    const form = document.getElementById('resetForm');
    const alertContainer = document.getElementById('alert-container');
    const submitBtn = document.getElementById('submitBtn');
    const btnText = document.getElementById('btnText');
    const btnSpinner = document.getElementById('btnSpinner');

    if (!form) return;

    function showAlert(message, type = 'danger') {
        alertContainer.innerHTML = `
            <div class="alert alert-${type} alert-dismissible fade show" role="alert">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;
    }

    function setLoading(loading) {
        submitBtn.disabled = loading;
        if (loading) {
            btnText.classList.add('d-none');
            btnSpinner.classList.remove('d-none');
        } else {
            btnText.classList.remove('d-none');
            btnSpinner.classList.add('d-none');
        }
    }

    form.addEventListener('submit', async (e) => {
        if (!window.fetch) return;
        
        e.preventDefault();
        alertContainer.innerHTML = '';
        setLoading(true);

        const formData = new FormData(form);
        
        try {
            const res = await fetch(form.action, {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: formData
            });

            const data = await res.json();

            if (res.ok) {
                showAlert('Password reset successfully! Redirecting to login...', 'success');
                setTimeout(() => {
                    window.location.href = data?.redirectUrl || '/Auth/Login';
                }, 2000);
            } else {
                const errors = data?.errors || ['Reset failed'];
                showAlert(errors.join('<br>'), 'danger');
                setLoading(false);
            }
        } catch (error) {
            console.error('Error:', error);
            showAlert('Network error. Please try again later.', 'danger');
            setLoading(false);
        }
    });
})();