// Scroll to top of the page smoothly
window.scrollToTop = () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
};

// Scroll to top instantly
window.scrollToTopInstant = () => {
    window.scrollTo(0, 0);
};