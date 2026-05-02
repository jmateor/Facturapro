/**
 * Skeleton Loader Helper - Facturapro
 * Permite inyectar placeholders animados fácilmente.
 */

const Skeleton = {
    templates: {
        product: `
            <div class="col-xl-3 col-lg-4 col-md-6 mb-4">
                <div class="skeleton-product-card shadow-sm">
                    <div class="skeleton skeleton-rect img rounded-3"></div>
                    <div class="skeleton skeleton-text title medium"></div>
                    <div class="skeleton skeleton-text title short"></div>
                    <div class="d-flex justify-content-between align-items-center mt-3">
                        <div class="skeleton skeleton-text price"></div>
                        <div class="skeleton skeleton-circle" style="width: 32px; height: 32px;"></div>
                    </div>
                </div>
            </div>`,
        
        category: `
            <div class="me-2 mb-2" style="display: inline-block;">
                <div class="skeleton skeleton-button" style="width: 100px; border-radius: 20px;"></div>
            </div>`,
        
        tableRow: `
            <tr>
                <td><div class="skeleton skeleton-text"></div></td>
                <td><div class="skeleton skeleton-text medium"></div></td>
                <td><div class="skeleton skeleton-text short"></div></td>
                <td><div class="skeleton skeleton-text"></div></td>
                <td><div class="skeleton skeleton-circle" style="width: 24px; height: 24px;"></div></td>
            </tr>`,
        
        statCard: `
            <div class="card">
                <div class="card-body">
                    <div class="d-flex align-items-center">
                        <div class="skeleton skeleton-circle me-3" style="width: 48px; height: 48px;"></div>
                        <div class="w-100">
                            <div class="skeleton skeleton-text short"></div>
                            <div class="skeleton skeleton-text title medium"></div>
                        </div>
                    </div>
                </div>
            </div>`
    },

    /**
     * Muestra skeletons en un contenedor
     * @param {string} selector - Selector del contenedor
     * @param {string} template - Nombre del template (product, category, etc)
     * @param {number} count - Cuántos elementos mostrar
     */
    show(selector, template, count = 4) {
        const container = document.querySelector(selector);
        if (!container) return;

        // Guardar contenido original si no se ha guardado
        if (!container.dataset.originalContent) {
            container.dataset.originalContent = container.innerHTML;
        }

        let html = '';
        const templateHtml = this.templates[template] || this.templates.product;
        
        for (let i = 0; i < count; i++) {
            html += templateHtml;
        }

        container.innerHTML = html;
        container.classList.add('loading-skeleton');
    },

    /**
     * Remueve los skeletons y (opcionalmente) restaura el contenido original
     * @param {string} selector - Selector del contenedor
     * @param {string} newHtml - Nuevo contenido a inyectar (si viene de una carga AJAX)
     */
    hide(selector, newHtml = null) {
        const container = document.querySelector(selector);
        if (!container) return;

        container.classList.remove('loading-skeleton');
        
        if (newHtml !== null) {
            container.innerHTML = newHtml;
        } else if (container.dataset.originalContent) {
            container.innerHTML = container.dataset.originalContent;
        }
    }
};

// Exportar globalmente
window.Skeleton = Skeleton;
