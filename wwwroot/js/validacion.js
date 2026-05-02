/**
 * Validación Pro Max - Facturapro
 * Motor de validación reactiva con feedback visual inmediato.
 */

class RealTimeValidator {
    constructor(config = {}) {
        this.debounceTimeout = config.debounceTimeout || 300;
        this.selectors = config.selectors || '.form-control, .form-select';
        this.init();
    }

    init() {
        document.addEventListener('DOMContentLoaded', () => {
            this.attachEventListeners();
        });
        window.initRealTimeValidation = () => this.attachEventListeners();
    }

    attachEventListeners() {
        const inputs = document.querySelectorAll(this.selectors);
        inputs.forEach(input => {
            if (input.hasAttribute('data-no-validate')) return;
            
            const name = (input.name || input.id || '').toLowerCase();

            // Auto-formateo para campos específicos
            if (name.includes('rnc') || name.includes('cedula')) {
                input.addEventListener('input', () => window.FormFormatters.rnc(input));
            }
            if (name.includes('telefono') || name.includes('celular') || name.includes('phone')) {
                input.addEventListener('input', () => window.FormFormatters.phone(input));
            }

            // Escuchar cambios al escribir (con debounce)
            input.addEventListener('input', this.debounce((e) => {
                this.validateInput(e.target);
            }, this.debounceTimeout));

            // Validar al perder el foco (blur) inmediatamente
            input.addEventListener('blur', (e) => {
                this.validateInput(e.target);
            });
        });

        // Interceptar el submit del formulario
        document.querySelectorAll('form').forEach(form => {
            form.addEventListener('submit', (e) => {
                if (!this.validateForm(form)) {
                    e.preventDefault();
                    this.scrollToFirstError(form);
                }
            });
        });
    }

    validateInput(input) {
        const name = input.name || input.id;
        const value = input.value.trim();
        let isValid = true;
        let errorMessage = '';

        // 1. Required
        if (input.hasAttribute('required') && !value) {
            isValid = false;
            errorMessage = 'Este campo es obligatorio';
        }

        // 2. Email
        if (isValid && input.type === 'email' && value) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(value)) {
                isValid = false;
                errorMessage = 'Ingresa un correo electrónico válido';
            }
        }

        // 3. RNC / Cédula (Específico para Facturapro)
        if (isValid && (name.toLowerCase().includes('rnc') || name.toLowerCase().includes('cedula') || name.toLowerCase().includes('documento')) && value) {
            const cleanValue = value.replace(/[^0-9]/g, '');
            if (cleanValue.length !== 9 && cleanValue.length !== 11) {
                isValid = false;
                errorMessage = 'El RNC/Cédula debe tener 9 u 11 dígitos';
            }
        }

        // 4. Min/Max Length
        if (isValid && input.minLength > 0 && value.length < input.minLength) {
            isValid = false;
            errorMessage = `Mínimo ${input.minLength} caracteres`;
        }

        this.applyVisualFeedback(input, isValid, errorMessage);
        return isValid;
    }

    applyVisualFeedback(input, isValid, message) {
        const parent = input.closest('.col-12, .col-md-6, .col-md-4, .mb-3');
        const label = parent ? parent.querySelector('label') : null;
        
        // Remover clases previas
        input.classList.remove('is-valid-realtime', 'is-invalid-realtime');
        if (label) label.classList.remove('is-invalid-realtime-label');

        // Buscar o crear contenedor de error
        let feedback = input.nextElementSibling;
        if (!feedback || !feedback.classList.contains('invalid-feedback-realtime')) {
            feedback = document.createElement('div');
            feedback.className = 'invalid-feedback-realtime';
            input.after(feedback);
        }

        if (isValid) {
            if (input.value.trim() !== '') {
                input.classList.add('is-valid-realtime');
            }
            feedback.textContent = '';
        } else {
            input.classList.add('is-invalid-realtime');
            if (label) label.classList.add('is-invalid-realtime-label');
            feedback.textContent = message;
        }
    }

    validateForm(form) {
        const inputs = form.querySelectorAll(this.selectors);
        let isFormValid = true;

        inputs.forEach(input => {
            if (!this.validateInput(input)) {
                isFormValid = false;
            }
        });

        return isFormValid;
    }

    scrollToFirstError(form) {
        const firstError = form.querySelector('.is-invalid-realtime');
        if (firstError) {
            firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
            firstError.focus();
        }
    }

    debounce(func, wait) {
        let timeout;
        return function(...args) {
            clearTimeout(timeout);
            timeout = setTimeout(() => func.apply(this, args), wait);
        };
    }
}

// Instanciar el validador globalmente
const validator = new RealTimeValidator();

// Exponer funciones útiles para formateo en tiempo real (Helper para la UI)
window.FormFormatters = {
    rnc: (input) => {
        input.value = input.value.replace(/[^0-9]/g, '').substring(0, 11);
    },
    phone: (input) => {
        let value = input.value.replace(/[^0-9]/g, '');
        if (value.length > 3 && value.length <= 6) {
            value = value.slice(0, 3) + '-' + value.slice(3);
        } else if (value.length > 6) {
            value = value.slice(0, 3) + '-' + value.slice(3, 6) + '-' + value.slice(6, 10);
        }
        input.value = value;
    }
};
