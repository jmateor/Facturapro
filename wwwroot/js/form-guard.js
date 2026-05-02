/**
 * Form Guard - Facturapro
 * Previene la pérdida de datos avisando antes de salir de formularios con cambios.
 */

window.FormGuard = {
    isDirty: false,
    currentForm: null,

    init() {
        // Solo actuar en formularios marcados para seguimiento
        const forms = document.querySelectorAll('form.dirty-check');
        if (forms.length === 0) return;

        forms.forEach(form => {
            this.currentForm = form;
            
            // Escuchar cualquier cambio en los inputs
            form.querySelectorAll('input, select, textarea').forEach(input => {
                input.addEventListener('input', () => this.markAsDirty());
                input.addEventListener('change', () => this.markAsDirty());
            });

            // No avisar si el usuario está enviando el formulario
            form.addEventListener('submit', () => {
                this.isDirty = false;
            });
        });

        // 1. Aviso al cerrar pestaña o recargar (Nativo del navegador)
        window.addEventListener('beforeunload', (e) => {
            if (this.isDirty) {
                e.preventDefault();
                e.returnValue = ''; // Requerido por navegadores modernos
            }
        });

        // 2. Aviso en navegación interna (Clicks en enlaces)
        document.addEventListener('click', (e) => {
            const link = e.target.closest('a');
            if (!link) return;

            // Ignorar si el enlace abre en nueva pestaña, es un ancla o javascript
            if (link.target === '_blank' || link.href.startsWith('javascript:') || link.getAttribute('href')?.startsWith('#')) {
                return;
            }

            // Ignorar si el clic viene de dentro del modal de confirmación
            if (link.closest('#formGuardModal')) {
                return;
            }

            if (this.isDirty) {
                e.preventDefault();
                this.showConfirmModal(link.href);
            }
        });
    },

    markAsDirty() {
        if (!this.isDirty) {
            console.log('FormGuard: Detectados cambios sin guardar.');
            this.isDirty = true;
        }
    },

    showConfirmModal(targetUrl) {
        // Crear el modal dinámicamente si no existe
        let modalHtml = `
            <div class="modal fade" id="formGuardModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content border-0 shadow">
                        <div class="modal-header bg-warning text-white">
                            <h5 class="modal-title text-white"><i class="ri-error-warning-line me-2"></i>¿Salir sin guardar?</h5>
                            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body p-4">
                            <p class="mb-0">Has realizado cambios en este formulario que se perderán si sales ahora. ¿Deseas continuar?</p>
                        </div>
                        <div class="modal-footer bg-light d-flex gap-2">
                            <button type="button" class="btn btn-light" data-bs-dismiss="modal">Seguir editando</button>
                            <a href="${targetUrl}" class="btn btn-danger px-4">Descartar y salir</a>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Eliminar modal anterior si existe
        const oldModal = document.getElementById('formGuardModal');
        if (oldModal) oldModal.remove();

        document.body.insertAdjacentHTML('beforeend', modalHtml);
        const modalElement = document.getElementById('formGuardModal');
        const modal = new bootstrap.Modal(modalElement);
        
        // Desactivar guard al hacer clic en descartar
        modalElement.querySelector('.btn-danger').addEventListener('click', () => {
            this.isDirty = false;
        });

        // Limpiar al cerrar
        modalElement.addEventListener('hidden.bs.modal', () => {
            modalElement.remove();
        });

        modal.show();
    }
};

document.addEventListener('DOMContentLoaded', () => window.FormGuard.init());
