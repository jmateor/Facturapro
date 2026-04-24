// App principal - Funcionalidades modernas
(function() {
  'use strict';

  const App = {
    init() {
      this.initSidebar();
      this.initDropdowns();
      this.initModals();
      this.initTooltips();
      this.initAnimations();
      this.initFormValidation();
      this.initNotifications();
    },

    // Sidebar toggle para móvil
    initSidebar() {
      const toggle = document.querySelector('.menu-toggle');
      const sidebar = document.querySelector('.sidebar');
      const overlay = document.querySelector('.sidebar-overlay');

      if (toggle) {
        toggle.addEventListener('click', () => {
          sidebar.classList.toggle('open');
          document.body.classList.toggle('sidebar-open');
        });
      }

      // Cerrar sidebar al hacer click fuera en móvil
      document.addEventListener('click', (e) => {
        if (window.innerWidth <= 1024 &&
            sidebar?.classList.contains('open') &&
            !sidebar.contains(e.target) &&
            !toggle?.contains(e.target)) {
          sidebar.classList.remove('open');
          document.body.classList.remove('sidebar-open');
        }
      });

      // Marcar link activo según la URL actual
      const currentPath = window.location.pathname;
      document.querySelectorAll('.nav-link').forEach(link => {
        if (link.getAttribute('href') === currentPath) {
          link.classList.add('active');
        }
      });
    },

    // Dropdowns
    initDropdowns() {
      document.querySelectorAll('.dropdown').forEach(dropdown => {
        const trigger = dropdown.querySelector('.dropdown-trigger');

        if (trigger) {
          trigger.addEventListener('click', (e) => {
            e.stopPropagation();
            // Cerrar otros dropdowns
            document.querySelectorAll('.dropdown.active').forEach(d => {
              if (d !== dropdown) d.classList.remove('active');
            });
            dropdown.classList.toggle('active');
          });
        }
      });

      // Cerrar dropdowns al hacer click fuera
      document.addEventListener('click', () => {
        document.querySelectorAll('.dropdown.active').forEach(d => {
          d.classList.remove('active');
        });
      });
    },

    // Modales
    initModals() {
      // Abrir modal
      document.querySelectorAll('[data-modal]').forEach(trigger => {
        trigger.addEventListener('click', () => {
          const modalId = trigger.getAttribute('data-modal');
          const modal = document.querySelector(modalId);
          if (modal) {
            modal.classList.add('active');
            document.body.style.overflow = 'hidden';
          }
        });
      });

      // Cerrar modal
      document.querySelectorAll('.modal-overlay').forEach(modal => {
        // Cerrar al hacer click en el overlay
        modal.addEventListener('click', (e) => {
          if (e.target === modal) {
            this.closeModal(modal);
          }
        });

        // Cerrar con botón
        const closeBtn = modal.querySelector('.modal-close');
        if (closeBtn) {
          closeBtn.addEventListener('click', () => this.closeModal(modal));
        }
      });

      // Cerrar con Escape
      document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
          document.querySelectorAll('.modal-overlay.active').forEach(modal => {
            this.closeModal(modal);
          });
        }
      });
    },

    closeModal(modal) {
      modal.classList.remove('active');
      document.body.style.overflow = '';
    },

    // Tooltips simples
    initTooltips() {
      document.querySelectorAll('[data-tooltip]').forEach(el => {
        el.addEventListener('mouseenter', (e) => {
          const text = el.getAttribute('data-tooltip');
          const tooltip = document.createElement('div');
          tooltip.className = 'tooltip';
          tooltip.textContent = text;
          document.body.appendChild(tooltip);

          const rect = el.getBoundingClientRect();
          tooltip.style.left = `${rect.left + rect.width / 2 - tooltip.offsetWidth / 2}px`;
          tooltip.style.top = `${rect.top - tooltip.offsetHeight - 8}px`;
          tooltip.style.opacity = '1';

          el._tooltip = tooltip;
        });

        el.addEventListener('mouseleave', () => {
          if (el._tooltip) {
            el._tooltip.remove();
            el._tooltip = null;
          }
        });
      });
    },

    // Animaciones de entrada
    initAnimations() {
      const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
      };

      const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            entry.target.classList.add('animate-visible');
            observer.unobserve(entry.target);
          }
        });
      }, observerOptions);

      document.querySelectorAll('.animate-on-scroll').forEach(el => {
        observer.observe(el);
      });
    },

    // Validación de formularios
    initFormValidation() {
      document.querySelectorAll('form[data-validate]').forEach(form => {
        form.addEventListener('submit', (e) => {
          let isValid = true;

          form.querySelectorAll('[required]').forEach(field => {
            const value = field.value.trim();
            const formGroup = field.closest('.form-group');

            if (!value) {
              isValid = false;
              field.classList.add('is-invalid');

              let error = formGroup.querySelector('.form-error');
              if (!error) {
                error = document.createElement('div');
                error.className = 'form-error';
                formGroup.appendChild(error);
              }
              error.textContent = 'Este campo es obligatorio';
            } else {
              field.classList.remove('is-invalid');
              const error = formGroup.querySelector('.form-error');
              if (error) error.remove();
            }
          });

          if (!isValid) {
            e.preventDefault();
          }
        });

        // Limpiar errores al escribir
        form.querySelectorAll('.form-input').forEach(field => {
          field.addEventListener('input', () => {
            field.classList.remove('is-invalid');
            const formGroup = field.closest('.form-group');
            const error = formGroup?.querySelector('.form-error');
            if (error) error.remove();
          });
        });
      });
    },

    // Sistema de notificaciones toast
    initNotifications() {
      window.AppNotification = {
        show(message, type = 'info', duration = 3000) {
          const toast = document.createElement('div');
          toast.className = `toast toast-${type}`;
          toast.innerHTML = `
            <span class="toast-message">${message}</span>
            <button class="toast-close">&times;</button>
          `;

          const container = document.querySelector('.toast-container') || (() => {
            const c = document.createElement('div');
            c.className = 'toast-container';
            document.body.appendChild(c);
            return c;
          })();

          container.appendChild(toast);

          // Animación de entrada
          requestAnimationFrame(() => {
            toast.classList.add('toast-visible');
          });

          // Auto cerrar
          const timeout = setTimeout(() => {
            this.remove(toast);
          }, duration);

          // Cerrar manual
          toast.querySelector('.toast-close').addEventListener('click', () => {
            clearTimeout(timeout);
            this.remove(toast);
          });
        },

        remove(toast) {
          toast.classList.remove('toast-visible');
          setTimeout(() => toast.remove(), 300);
        },

        success(message, duration) {
          this.show(message, 'success', duration);
        },

        error(message, duration) {
          this.show(message, 'error', duration);
        },

        warning(message, duration) {
          this.show(message, 'warning', duration);
        }
      };
    },

    // Utilidades
    utils: {
      // Debounce para eventos
      debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
          const later = () => {
            clearTimeout(timeout);
            func(...args);
          };
          clearTimeout(timeout);
          timeout = setTimeout(later, wait);
        };
      },

      // Formatear fechas
      formatDate(date, options = {}) {
        const d = new Date(date);
        return d.toLocaleDateString('es-ES', {
          year: 'numeric',
          month: 'short',
          day: 'numeric',
          ...options
        });
      },

      // Formatear moneda
      formatCurrency(amount, currency = 'EUR') {
        return new Intl.NumberFormat('es-ES', {
          style: 'currency',
          currency
        }).format(amount);
      },

      // Copiar al portapapeles
      async copyToClipboard(text) {
        try {
          await navigator.clipboard.writeText(text);
          return true;
        } catch (err) {
          console.error('Error al copiar:', err);
          return false;
        }
      }
    }
  };

  // Inicializar cuando el DOM esté listo
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => App.init());
  } else {
    App.init();
  }

  // Exponer App globalmente
  window.App = App;
})();
