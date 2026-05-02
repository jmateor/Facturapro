/**
 * Global Search - Facturapro
 * Maneja el atajo Ctrl+K y la lógica de búsqueda en tiempo real.
 */

const GlobalSearch = {
    modal: null,
    input: null,
    results: null,
    selectedIndex: -1,

    init() {
        this.modal = new bootstrap.Modal(document.getElementById('globalSearchModal'));
        this.input = document.getElementById('globalSearchInput');
        this.results = document.getElementById('resultsContainer');
        this.placeholder = document.getElementById('searchPlaceholder');

        // Atajo de teclado Ctrl + K o Cmd + K
        document.addEventListener('keydown', (e) => {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                this.open();
            }
        });

        // Eventos del input
        this.input.addEventListener('input', (e) => this.handleInput(e));
        this.input.addEventListener('keydown', (e) => this.handleKeydown(e));

        // Limpiar al cerrar
        document.getElementById('globalSearchModal').addEventListener('hidden.bs.modal', () => {
            this.input.value = '';
            this.results.innerHTML = '';
            this.results.style.display = 'none';
            this.placeholder.style.display = 'block';
            this.selectedIndex = -1;
        });
    },

    open() {
        this.modal.show();
        setTimeout(() => this.input.focus(), 500);
    },

    handleInput(e) {
        const q = e.target.value.trim();

        if (q.length < 2) {
            this.results.style.display = 'none';
            this.placeholder.style.display = 'block';
            return;
        }

        this.placeholder.style.display = 'none';
        this.results.style.display = 'block';
        this.results.innerHTML = '<div class="p-4 text-center"><div class="spinner-border text-primary spinner-border-sm"></div></div>';

        // Debounce simple
        clearTimeout(this.searchTimeout);
        this.searchTimeout = setTimeout(() => {
            this.performSearch(q);
        }, 300);
    },

    async performSearch(q) {
        try {
            const response = await fetch(`/Search/Query?q=${encodeURIComponent(q)}`);
            const data = await response.json();

            if (data.results.length === 0) {
                this.results.innerHTML = '<div class="p-4 text-center text-muted">No se encontraron resultados</div>';
                return;
            }

            this.renderResults(data.results);
        } catch (err) {
            console.error('Error en búsqueda global:', err);
            this.results.innerHTML = '<div class="p-4 text-center text-danger">Error al buscar</div>';
        }
    },

    renderResults(items) {
        let html = '';
        let currentCategory = '';

        items.forEach((item, index) => {
            if (item.category !== currentCategory) {
                currentCategory = item.category;
                html += `<div class="search-category-title">${currentCategory}</div>`;
            }

            html += `
                <a href="${item.url}" class="search-result-item" data-index="${index}">
                    <i class="${item.icon} bg-light"></i>
                    <div>
                        <div class="fw-semibold">${item.title}</div>
                        ${item.subtitle ? `<div class="small text-muted">${item.subtitle}</div>` : ''}
                    </div>
                </a>
            `;
        });

        this.results.innerHTML = html;
        this.selectedIndex = -1;
    },

    handleKeydown(e) {
        const items = this.results.querySelectorAll('.search-result-item');
        
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
    },

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
};

// Auto-inicializar
document.addEventListener('DOMContentLoaded', () => GlobalSearch.init());
