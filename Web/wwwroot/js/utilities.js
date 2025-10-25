// Scroll to top of the page smoothly
window.scrollToTop = () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
};

// Scroll to top instantly
window.scrollToTopInstant = () => {
    window.scrollTo(0, 0);
};

// Scroll modal content to top
window.scrollModalToTop = (modalId) => {
    const modal = document.getElementById(modalId);
    if (modal) {
        const modalBody = modal.querySelector('.modal-body');
        if (modalBody) {
            modalBody.scrollTo({ top: 0, behavior: 'instant' });
        }
    }
};

// Scroll element to top by ID
window.scrollElementToTop = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTo({ top: 0, behavior: 'instant' });
    }
};