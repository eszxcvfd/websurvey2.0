(function () {
    const list = document.getElementById('qList');
    const hidden = document.getElementById('orderedIds');
    const form = document.getElementById('reorderForm');
    if (!list || !hidden || !form) return;

    // lightweight: just read DOM order on submit
    form.addEventListener('submit', (e) => {
        const ids = Array.from(list.querySelectorAll('[data-id]')).map(li => li.getAttribute('data-id'));
        hidden.value = ids.join('&orderedIds=');
        // Note: MVC binder binds multiple same-name fields; trick with querystring-like works with default binder too.
    });
})();