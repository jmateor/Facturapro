/**
 * Toast Notifications Manager - Facturapro
 */

const Toast = {
    containerId: 'toast-container',
    
    init() {
        if (!document.getElementById(this.containerId)) {
            const container = document.createElement('div');
            container.id = this.containerId;
            document.body.appendChild(container);
        }
    },

    show(title, message, type = 'success', duration = 5000) {
        this.init();
        const container = document.getElementById(this.containerId);
        
        const icons = {
            success: 'ri-checkbox-circle-line',
            error: 'ri-error-warning-line',
            info: 'ri-information-line',
            warning: 'ri-alert-line'
        };

        const toast = document.createElement('div');
        toast.className = `toast-item toast-${type}`;
        
        toast.innerHTML = `
            <div class="toast-icon">
                <i class="${icons[type]}"></i>
            </div>
            <div class="toast-content">
                <div class="toast-title">${title}</div>
                <div class="toast-message">${message}</div>
            </div>
            <div class="toast-close">
                <i class="ri-close-line"></i>
            </div>
            <div class="toast-progress">
                <div class="toast-progress-bar" style="animation: toast-progress ${duration}ms linear forwards;"></div>
            </div>
        `;

        container.appendChild(toast);

        // Forzar reflow para animación
        setTimeout(() => toast.classList.add('show'), 10);

        // Cerrar al click
        toast.querySelector('.toast-close').addEventListener('click', () => this.remove(toast));

        // Auto-eliminar
        if (duration > 0) {
            setTimeout(() => this.remove(toast), duration);
        }
    },

    remove(toast) {
        toast.classList.add('hide');
        toast.classList.remove('show');
        setTimeout(() => {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 400);
    },

    success(title, message) { this.show(title, message, 'success'); },
    error(title, message) { this.show(title, message, 'error'); },
    info(title, message) { this.show(title, message, 'info'); },
    warning(title, message) { this.show(title, message, 'warning'); }
};

// Estilo para la barra de progreso
const style = document.createElement('style');
style.innerHTML = `
    @keyframes toast-progress {
        from { transform: scaleX(1); }
        to { transform: scaleX(0); }
    }
`;
document.head.appendChild(style);

// Exportar globalmente
window.Toast = Toast;
