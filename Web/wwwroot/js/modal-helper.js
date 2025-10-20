export function initializeModal(modalId, dotNetHelper) {
    const modalElement = document.getElementById(modalId);
    if (!modalElement) return;

    // ESC key handler
    const escHandler = (e) => {
        if (e.key === 'Escape') {
            dotNetHelper.invokeMethodAsync('CloseModal');
        }
    };

    // Outside click handler
    const clickHandler = (e) => {
        const modalDialog = modalElement.querySelector('.modal-dialog');
        if (modalDialog && !modalDialog.contains(e.target)) {
            dotNetHelper.invokeMethodAsync('CloseModal');
        }
    };

    // Add event listeners
    document.addEventListener('keydown', escHandler);
    modalElement.addEventListener('click', clickHandler);

    // Store handlers for cleanup
    modalElement._escHandler = escHandler;
    modalElement._clickHandler = clickHandler;
}

export function cleanupModal(modalId) {
    const modalElement = document.getElementById(modalId);
    if (!modalElement) return;

    if (modalElement._escHandler) {
        document.removeEventListener('keydown', modalElement._escHandler);
    }
    if (modalElement._clickHandler) {
        modalElement.removeEventListener('click', modalElement._clickHandler);
    }
}