// wwwroot/js/lucide-init.js

(function () {
    function initIcons() {
        if (window.lucide && typeof window.lucide.createIcons === "function") {
            window.lucide.createIcons();
            console.debug("[Lucide] Icons initialized.");
        } else {
            console.warn("[Lucide] lucide library not found on window.");
        }
    }

    // Public API if you ever want to call it manually from console or other scripts
    window.pgLucide = {
        init: initIcons
    };

    // Initial run after DOM is ready (first page load)
    document.addEventListener("DOMContentLoaded", initIcons);

    // Hook that Blazor will call after each render
    window.Blazor = window.Blazor || {};
    window.Blazor.LucideAfterRender = () => {
        // Defer to the end of the current tick so Blazor finishes updating the DOM
        setTimeout(initIcons, 0);
    };
})();
