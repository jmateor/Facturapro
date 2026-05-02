/**
 * Buscador Global (Command Palette) - Facturapro
 * Maneja el atajo Ctrl+K, búsqueda dinámica y navegación por teclado.
 */

class GlobalSearch {
    constructor() {
        this.modalElement = document.getElementById('globalSearchModal');
        this.inputElement = document.getElementById('globalSearchInput');
        this.resultsContainer = document.getElementById('globalSearchResults');
        this.modal = null;
        this.selectedIndex = -1;
        this.debounceTimeout = null;
        
        // Módulos estáticos para acceso rápido
        this.modules = [
            { title: 'Dashboard', url: '/Home/Index', icon: 'ri-dashboard-3-line', category: 'Navegación' },
            { title: 'Punto de Venta (POS)', url: '/POS/Index', icon: 'ri-shopping-cart-2-line', category: 'Acciones Rápidas' },
            { title: 'Crear Nueva Factura', url: '/Facturas/Create', icon: 'ri-add-circle-line', category: 'Acciones Rápidas' },
            { title: 'Listado de Facturas', url: '/Facturas/Index', icon: 'ri-file-list-3-line', category: 'Navegación' },
            { title: 'Clientes', url: '/Clientes/Index', icon: 'ri-user-3-line', category: 'Navegación' },
            { title: 'Productos', url: '/Productos/Index', icon: 'ri-box-3-line', category: 'Navegación' },
            { title: 'Inventario (Kalder)', url: '/Kalder/Index', icon: 'ri-archive-line', category: 'Navegación' },
            { title: 'Reportes de Ventas', url: '/Reportes/Ventas', icon: 'ri-line-chart-line', category: 'Reportes' },
            { title: 'Gestión de Usuarios', url: '/Configuracion/Usuarios', icon: 'ri-user-settings-line', category: 'Sistema' },
            { title: 'Configuración Fiscal (DGII)', url: '/Configuracion/DGII', icon: 'ri-bank-card-line', category: 'Sistema' },
            { title: 'Configuración de Empresa', url: '/Configuracion/Empresa', icon: 'ri-building-line', category: 'Sistema' },
            { title: 'Nueva Compra / Entrada', url: '/Compras/Create', icon: 'ri-download-2-line', category: 'Acciones Rápidas' }
        ];

        this.init();
    }

    init() {
        if (!this.modalElement) return;

        this.modal = new bootstrap.Modal(this.modalElement);

        // Atajo de teclado Ctrl+K
        document.addEventListener('keydown', (e) => {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                this.open();
            }
        });

        // Eventos del input
        this.inputElement.addEventListener('input', () => this.handleInput());
        this.inputElement.addEventListener('keydown', (e) => this.handleNavigation(e));

        // Autofocus al abrir
        this.modalElement.addEventListener('shown.bs.modal', () => {
            this.inputElement.focus();
            if (!this.inputElement.value) {
                this.renderModules(); // Mostrar módulos al inicio
            }
        });
    }

    open() {
        this.modal.show();
    }

    handleInput() {
        const query = this.inputElement.value.trim();
        
        clearTimeout(this.debounceTimeout);
        
        if (!query) {
            this.renderModules();
            return;
        }

        this.debounceTimeout = setTimeout(() => {
            this.performSearch(query);
        }, 200);
    }

    async performSearch(query) {
        try {
            // Filtrar módulos locales
            const filteredModules = this.modules.filter(m => 
                m.title.toLowerCase().includes(query.toLowerCase()) || 
                m.category.toLowerCase().includes(query.toLowerCase())
            );

            // Buscar en el servidor (Clientes/Productos/Facturas)
            const response = await fetch(`/api/Search/global?q=${encodeURIComponent(query)}`);
            const serverResults = await response.json();

            this.renderResults(filteredModules, serverResults);
        } catch (error) {
            console.error('Error en búsqueda global:', error);
        }
    }

    renderModules() {
        this.renderResults(this.modules, []);
    }

    renderResults(modules, serverResults) {
        this.resultsContainer.innerHTML = '';
        this.selectedIndex = -1;

        if (modules.length === 0 && serverResults.length === 0) {
            this.resultsContainer.innerHTML = `
                <div class="p-4 text-center text-muted">
                    <i class="ri-search-eye-line fs-1 d-block mb-2 opacity-25"></i>
                    No se encontraron resultados para "${this.inputElement.value}"
                </div>`;
            return;
        }

        // Agrupar y renderizar
        const allResults = [...modules, ...serverResults];
        const categories = [...new Set(allResults.map(r => r.category))];

        categories.forEach(cat => {
            const catHeader = document.createElement('div');
            catHeader.className = 'search-category-title';
            catHeader.textContent = cat;
            this.resultsContainer.appendChild(catHeader);

            allResults.filter(r => r.category === cat).forEach(res => {
                const item = this.createSearchResultItem(res);
                this.resultsContainer.appendChild(item);
            });
        });
    }

    createSearchResultItem(res) {
        const item = document.createElement('a');
        item.href = res.url;
        item.className = 'list-group-item list-group-item-action border-0 search-item d-flex align-items-center py-3';
        item.innerHTML = `
            <div class="avatar-sm bg-light rounded-circle d-flex align-items-center justify-content-center me-3">
                <i class="${res.icon} fs-4 text-primary"></i>
            </div>
            <div class="flex-grow-1">
                <h6 class="mb-0 fw-semibold">${res.title}</h6>
                <small class="text-muted">${res.description || res.category}</small>
            </div>
            <i class="ri-arrow-right-s-line text-muted"></i>
        `;
        
        item.addEventListener('click', (e) => {
            if (e.ctrlKey || e.metaKey) return; // Permitir abrir en nueva pestaña
            e.preventDefault();
            this.modal.hide();
            window.location.href = res.url;
        });

        return item;
    }

    handleNavigation(e) {
        const items = this.resultsContainer.querySelectorAll('.search-item');
        if (items.length === 0) return;

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            this.selectedIndex = (this.selectedIndex + 1) % items.length;
            this.updateSelection(items);
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            this.selectedIndex = (this.selectedIndex - 1 + items.length) % items.length;
            this.updateSelection(items);
        } else if (e.key === 'Enter') {
            if (this.selectedIndex > -1) {
                e.preventDefault();
                items[this.selectedIndex].click();
            }
        }
    }

    updateSelection(items) {
        items.forEach((item, index) => {
            if (index === this.selectedIndex) {
                item.classList.add('active');
                item.scrollIntoView({ block: 'nearest' });
            } else {
                item.classList.remove('active');
            }
        });
    }
}

// Inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', () => {
    window.globalSearch = new GlobalSearch();
});
