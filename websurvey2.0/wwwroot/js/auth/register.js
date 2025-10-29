(function () {
  const form = document.getElementById('registerForm');
  const alertContainer = document.getElementById('alert-container');

  function showAlert(messages, type) {
    const html = Array.isArray(messages) ? messages.map(m => `<div>${m}</div>`).join('') : messages;
    alertContainer.innerHTML = `<div class="alert alert-${type}" role="alert">${html}</div>`;
  }

  form.addEventListener('submit', async function (e) {
    e.preventDefault();
    alertContainer.innerHTML = '';

    const formData = new FormData(form);
    try {
      const resp = await fetch(form.action, {
        method: 'POST',
        headers: {
          'X-Requested-With': 'XMLHttpRequest'
        },
        body: formData
      });

      // Kiểm tra content-type trước khi parse JSON
      const contentType = resp.headers.get('content-type');
      if (!contentType || !contentType.includes('application/json')) {
        console.error('Unexpected content-type:', contentType);
        console.error('Response status:', resp.status);
        const text = await resp.text();
        console.error('Response body:', text);
        throw new Error('Server did not return JSON. Check console for details.');
      }

      const result = await resp.json();

      if (resp.ok && result.success) {
        showAlert('Registration successful. Redirecting...', 'success');
        setTimeout(() => {
          window.location.href = result.redirectUrl || '/';
        }, 800);
      } else {
        const errs = result?.errors || ['Registration failed.'];
        showAlert(errs, 'danger');
      }
    } catch (err) {
      console.error('Registration error:', err);
      showAlert('Network error or server issue. Please check console and try again.', 'danger');
    }
  });
})();