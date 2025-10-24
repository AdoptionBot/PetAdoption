export function initializeModal(modalId, dotNetHelper) {
    const modalElement = document.getElementById(modalId);
    if (!modalElement) return;

    let mouseDownOutside = false;

    // ESC key handler
    const escHandler = (e) => {
        if (e.key === 'Escape') {
            dotNetHelper.invokeMethodAsync('CloseModal');
        }
    };

    // Track where mousedown occurs
    const mouseDownHandler = (e) => {
        const modalDialog = modalElement.querySelector('.modal-dialog');
        // Set flag to true only if mousedown is outside the modal dialog
        mouseDownOutside = modalDialog && !modalDialog.contains(e.target);
    };

    // Handle click - only close if both mousedown and mouseup were outside
    const clickHandler = (e) => {
        const modalDialog = modalElement.querySelector('.modal-dialog');
        
        // Only close if:
        // 1. mousedown was outside (mouseDownOutside is true)
        // 2. AND the current click target is also outside
        if (mouseDownOutside && modalDialog && !modalDialog.contains(e.target)) {
            dotNetHelper.invokeMethodAsync('CloseModal');
        }
        
        // Reset the flag after handling
        mouseDownOutside = false;
    };

    // Add event listeners
    document.addEventListener('keydown', escHandler);
    modalElement.addEventListener('mousedown', mouseDownHandler);
    modalElement.addEventListener('click', clickHandler);

    // Store handlers for cleanup
    modalElement._escHandler = escHandler;
    modalElement._mouseDownHandler = mouseDownHandler;
    modalElement._clickHandler = clickHandler;
}

export function cleanupModal(modalId) {
    const modalElement = document.getElementById(modalId);
    if (!modalElement) return;

    if (modalElement._escHandler) {
        document.removeEventListener('keydown', modalElement._escHandler);
    }
    if (modalElement._mouseDownHandler) {
        modalElement.removeEventListener('mousedown', modalElement._mouseDownHandler);
    }
    if (modalElement._clickHandler) {
        modalElement.removeEventListener('click', modalElement._clickHandler);
    }
    
    // Clean up stored references
    delete modalElement._escHandler;
    delete modalElement._mouseDownHandler;
    delete modalElement._clickHandler;
}