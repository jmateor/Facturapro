/**
 * UI Enhancements - Facturapro
 * - Modo Oscuro Inteligente
 * - Micro-interacciones de Botones
 */

const UIEnhancements = {
    init() {
        this.initDarkMode();
        this.initButtonEffects();
    },

    // ========================================
    // MODO OSCURO
    // ========================================
    initDarkMode() {
        const themeToggle = document.getElementById('light-dark-mode');
        const html = document.documentElement;
        const savedTheme = localStorage.getItem('facturapro-theme') || 'auto';

        const applyTheme = (theme) => {
            let actualTheme = theme;
            if (theme === 'auto') {
                actualTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
            }
            document.documentElement.setAttribute('data-bs-theme', actualTheme);
            this.updateThemeIcon(actualTheme);
        };

        applyTheme(savedTheme);

        if (themeToggle) {
            themeToggle.addEventListener('click', (e) => {
                e.preventDefault();
                const currentTheme = document.documentElement.getAttribute('data-bs-theme');
                const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
                localStorage.setItem('facturapro-theme', newTheme);
                applyTheme(newTheme);
            });
        }

        // Escuchar cambios del sistema si está en auto
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
            if (localStorage.getItem('facturapro-theme') === 'auto') {
                applyTheme('auto');
            }
        });
    },

    updateThemeIcon(theme) {
        const icon = document.getElementById('theme-icon');
        if (icon) {
            icon.className = theme === 'dark' ? 'ri-sun-line fs-22' : 'ri-moon-line fs-22';
        }
    },

    // ========================================
    // EFECTOS DE BOTONES
    // ========================================
    initButtonEffects() {
        // Efecto Ripple (Onda)
        document.querySelectorAll('.btn').forEach(button => {
            button.addEventListener('click', function(e) {
                let x = e.clientX - e.target.offsetLeft;
                let y = e.clientY - e.target.offsetTop;
                
                let ripples = document.createElement('span');
                ripples.className = 'btn-ripple';
                ripples.style.left = x + 'px';
                ripples.style.top = y + 'px';
                this.appendChild(ripples);

                setTimeout(() => {
                    ripples.remove();
                }, 1000);
            });
        });

        // Loading State en formularios
        document.querySelectorAll('form').forEach(form => {
            form.addEventListener('submit', (e) => {
                const submitBtn = form.querySelector('[type="submit"]');
                if (submitBtn && !submitBtn.classList.contains('no-loader')) {
                    const originalHtml = submitBtn.innerHTML;
                    submitBtn.disabled = true;
                    submitBtn.innerHTML = `<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span> Procesando...`;
                    
                    // Failsafe por si el submit falla o es prevenido
                    setTimeout(() => {
                        if (submitBtn.disabled) {
                            submitBtn.disabled = false;
                            submitBtn.innerHTML = originalHtml;
                        }
                    }, 10000);
                }
            });
        });
    }
};

document.addEventListener('DOMContentLoaded', () => UIEnhancements.init());
