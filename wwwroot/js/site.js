// Enhanced header functionality with improved ratios
document.addEventListener('DOMContentLoaded', function () {

    // Header scroll effect for better proportions
    let lastScrollTop = 0;
    const header = document.querySelector('header');
    const navbar = document.querySelector('.navbar');

    window.addEventListener('scroll', function () {
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;

        if (scrollTop > 100) {
            header.classList.add('scrolled');
            navbar.style.padding = '0.5rem 0'; // Reduced padding when scrolled
        } else {
            header.classList.remove('scrolled');
            navbar.style.padding = '0.875rem 0'; // Original padding
        }

        lastScrollTop = scrollTop;
    });

    // Enhanced search functionality
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        let searchTimeout;

        searchInput.addEventListener('input', function (e) {
            clearTimeout(searchTimeout);
            const query = e.target.value.trim();

            // Add search loading state
            if (query.length > 0) {
                this.style.paddingRight = '2.5rem';
                // You can add a loading icon here
            } else {
                this.style.paddingRight = '1rem';
            }

            // Debounced search
            searchTimeout = setTimeout(() => {
                if (query.length > 2) {
                    performSearch(query);
                }
            }, 300);
        });

        // Search on Enter
        searchInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                const query = this.value.trim();
                if (query.length > 0) {
                    performSearch(query);
                }
            }
        });
    }

    // Improved dropdown behavior
    const dropdownToggle = document.querySelector('.dropdown-toggle');
    if (dropdownToggle) {
        dropdownToggle.addEventListener('click', function () {
            // Add ripple effect for better UX
            createRipple(this, event);
        });
    }

    // Enhanced mobile menu
    const navbarToggler = document.querySelector('.navbar-toggler');
    const navbarCollapse = document.querySelector('.navbar-collapse');

    if (navbarToggler && navbarCollapse) {
        navbarToggler.addEventListener('click', function () {
            // Add smooth animation class
            navbarCollapse.classList.add('animating');

            setTimeout(() => {
                navbarCollapse.classList.remove('animating');
            }, 300);
        });
    }

    // Auto-hide mobile menu when clicking nav links
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
        link.addEventListener('click', function () {
            if (window.innerWidth < 992) {
                const navbarCollapse = document.querySelector('.navbar-collapse');
                if (navbarCollapse.classList.contains('show')) {
                    navbarToggler.click();
                }
            }
        });
    });

    // Improved active link highlighting
    function updateActiveLink() {
        const currentPath = window.location.pathname;
        const navLinks = document.querySelectorAll('.nav-link');

        navLinks.forEach(link => {
            const href = link.getAttribute('href');
            if (href && currentPath.includes(href)) {
                link.classList.add('active');
            } else {
                link.classList.remove('active');
            }
        });
    }

    updateActiveLink();

    // Logout confirmation with better styling
    window.confirmLogout = function () {
        return confirm('Bạn có chắc chắn muốn đăng xuất?');
    };

    // Search function placeholder
    function performSearch(query) {
        console.log('Searching for:', query);
        // Implement your search logic here
        // You can redirect to search results page or show dropdown results

        // Example: redirect to search page
        // window.location.href = `/Search?q=${encodeURIComponent(query)}`;

        // Or show inline results dropdown
        showSearchResults(query);
    }

    function showSearchResults(query) {
        // Create and show search results dropdown
        let resultsDropdown = document.getElementById('searchResults');

        if (!resultsDropdown) {
            resultsDropdown = document.createElement('div');
            resultsDropdown.id = 'searchResults';
            resultsDropdown.className = 'search-results-dropdown';
            resultsDropdown.style.cssText = `
                position: absolute;
                top: 100%;
                left: 0;
                right: 0;
                background: rgba(20,20,20,0.95);
                border: 1px solid rgba(255,255,255,0.1);
                border-radius: 12px;
                margin-top: 0.5rem;
                max-height: 300px;
                overflow-y: auto;
                backdrop-filter: blur(10px);
                z-index: 1000;
                display: none;
            `;

            const searchContainer = document.querySelector('.search-container');
            if (searchContainer) {
                searchContainer.appendChild(resultsDropdown);
                searchContainer.style.position = 'relative';
            }
        }

        // Show loading state
        resultsDropdown.innerHTML = `
            <div style="padding: 1rem; text-align: center; color: rgba(255,255,255,0.7);">
                <i class="fas fa-spinner fa-spin"></i> Đang tìm kiếm...
            </div>
        `;
        resultsDropdown.style.display = 'block';

        // Simulate search results (replace with actual API call)
        setTimeout(() => {
            resultsDropdown.innerHTML = `
                <div style="padding: 0.75rem 1rem; border-bottom: 1px solid rgba(255,255,255,0.1); color: #ffffff;">
                    <i class="fas fa-video" style="margin-right: 0.5rem; color: #e50914;"></i>
                    Kết quả cho "${query}"
                </div>
                <a href="#" style="display: block; padding: 0.75rem 1rem; color: #ffffff; text-decoration: none; border-bottom: 1px solid rgba(255,255,255,0.05);">
                    <i class="fas fa-film" style="margin-right: 0.5rem; color: #ffd700;"></i>
                    Phim mẫu 1
                </a>
                <a href="#" style="display: block; padding: 0.75rem 1rem; color: #ffffff; text-decoration: none;">
                    <i class="fas fa-film" style="margin-right: 0.5rem; color: #ffd700;"></i>
                    Phim mẫu 2
                </a>
            `;
        }, 500);
    }

    // Hide search results when clicking outside
    document.addEventListener('click', function (e) {
        const searchContainer = document.querySelector('.search-container');
        const searchResults = document.getElementById('searchResults');

        if (searchResults && searchContainer && !searchContainer.contains(e.target)) {
            searchResults.style.display = 'none';
        }
    });

    // Ripple effect function
    function createRipple(element, event) {
        const ripple = document.createElement('span');
        const rect = element.getBoundingClientRect();
        const size = Math.max(rect.width, rect.height);
        const x = event.clientX - rect.left - size / 2;
        const y = event.clientY - rect.top - size / 2;

        ripple.style.cssText = `
            position: absolute;
            width: ${size}px;
            height: ${size}px;
            left: ${x}px;
            top: ${y}px;
            background: rgba(255,255,255,0.3);
            border-radius: 50%;
            transform: scale(0);
            animation: ripple 0.6s ease-out;
            pointer-events: none;
        `;

        // Add ripple animation keyframes if not exists
        if (!document.getElementById('rippleStyles')) {
            const style = document.createElement('style');
            style.id = 'rippleStyles';
            style.textContent = `
                @keyframes ripple {
                    to {
                        transform: scale(2);
                        opacity: 0;
                    }
                }
            `;
            document.head.appendChild(style);
        }

        element.style.position = 'relative';
        element.style.overflow = 'hidden';
        element.appendChild(ripple);

        setTimeout(() => {
            ripple.remove();
        }, 600);
    }

    // Responsive font size adjustment
    function adjustResponsiveFonts() {
        const width = window.innerWidth;
        const navbar = document.querySelector('.navbar');

        if (width < 576) {
            // Extra small devices
            document.documentElement.style.setProperty('--nav-font-size', '0.875rem');
        } else if (width < 768) {
            // Small devices
            document.documentElement.style.setProperty('--nav-font-size', '0.9375rem');
        } else {
            // Medium and larger devices
            document.documentElement.style.setProperty('--nav-font-size', '0.9375rem');
        }
    }

    // Call on load and resize
    adjustResponsiveFonts();
    window.addEventListener('resize', adjustResponsiveFonts);

});