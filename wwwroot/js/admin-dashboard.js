// Enhanced JavaScript for Movie Management Pages
// Add this to your admin-dashboard.js or create a new movie-management.js file

document.addEventListener('DOMContentLoaded', function () {

    // ========== MOBILE SIDEBAR TOGGLE ========== 
    function initMobileSidebar() {
        // Create mobile menu toggle button
        const mobileToggle = document.createElement('button');
        mobileToggle.className = 'mobile-menu-toggle';
        mobileToggle.innerHTML = '☰';
        mobileToggle.style.cssText = `
            display: none;
            position: fixed;
            top: 1rem;
            left: 1rem;
            z-index: 2001;
            background: var(--primary-red);
            color: white;
            border: none;
            border-radius: 8px;
            padding: 0.75rem;
            font-size: 1.2rem;
            cursor: pointer;
            box-shadow: 0 4px 15px rgba(220, 38, 38, 0.3);
            transition: all 0.3s ease;
        `;

        // Add mobile styles
        const mobileStyles = document.createElement('style');
        mobileStyles.textContent = `
            @media (max-width: 992px) {
                .mobile-menu-toggle {
                    display: block !important;
                }
            }
            
            .mobile-menu-toggle:hover {
                background: var(--light-red) !important;
                transform: scale(1.1) !important;
            }
        `;
        document.head.appendChild(mobileStyles);
        document.body.appendChild(mobileToggle);

        const sidebar = document.querySelector('.sidebar');
        if (sidebar) {
            mobileToggle.addEventListener('click', function () {
                sidebar.classList.toggle('open');
                mobileToggle.innerHTML = sidebar.classList.contains('open') ? '✕' : '☰';
            });

            // Close sidebar when clicking outside
            document.addEventListener('click', function (e) {
                if (window.innerWidth <= 992 &&
                    !sidebar.contains(e.target) &&
                    !mobileToggle.contains(e.target) &&
                    sidebar.classList.contains('open')) {
                    sidebar.classList.remove('open');
                    mobileToggle.innerHTML = '☰';
                }
            });
        }
    }

    // ========== ENHANCED FORM INTERACTIONS ========== 
    function enhanceFormControls() {
        const formControls = document.querySelectorAll('.form-control, .form-control-file');

        formControls.forEach(control => {
            // Add floating label effect
            const label = control.previousElementSibling;
            if (label && label.tagName === 'LABEL') {
                control.addEventListener('focus', () => {
                    label.style.transform = 'translateY(-25px) scale(0.85)';
                    label.style.color = 'var(--primary-red)';
                });

                control.addEventListener('blur', () => {
                    if (!control.value) {
                        label.style.transform = '';
                        label.style.color = '';
                    }
                });
            }

            // Add ripple effect on focus
            control.addEventListener('focus', function () {
                this.style.position = 'relative';
                this.style.overflow = 'hidden';

                const ripple = document.createElement('span');
                ripple.style.cssText = `
                    position: absolute;
                    top: 50%;
                    left: 50%;
                    width: 0;
                    height: 0;
                    border-radius: 50%;
                    background: rgba(220, 38, 38, 0.3);
                    transform: translate(-50%, -50%);
                    animation: ripple 0.6s ease-out;
                    pointer-events: none;
                `;

                this.appendChild(ripple);

                setTimeout(() => {
                    if (ripple.parentNode) {
                        ripple.parentNode.removeChild(ripple);
                    }
                }, 600);
            });
        });

        // Add ripple animation keyframes
        if (!document.getElementById('ripple-styles')) {
            const rippleStyles = document.createElement('style');
            rippleStyles.id = 'ripple-styles';
            rippleStyles.textContent = `
                @keyframes ripple {
                    to {
                        width: 100px;
                        height: 100px;
                        opacity: 0;
                    }
                }
            `;
            document.head.appendChild(rippleStyles);
        }
    }

    // ========== FILE UPLOAD ENHANCEMENTS ========== 
    function enhanceFileUploads() {
        const fileInputs = document.querySelectorAll('input[type="file"]');

        fileInputs.forEach(input => {
            const wrapper = document.createElement('div');
            wrapper.className = 'file-upload-wrapper';
            wrapper.style.cssText = `
                position: relative;
                display: inline-block;
                width: 100%;
            `;

            input.parentNode.insertBefore(wrapper, input);
            wrapper.appendChild(input);

            // Create custom file upload display
            const display = document.createElement('div');
            display.className = 'file-upload-display';
            display.style.cssText = `
                position: absolute;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                display: flex;
                align-items: center;
                padding: 1rem 1.5rem;
                background: transparent;
                pointer-events: none;
                color: var(--text-gray);
                font-size: 0.9rem;
            `;
            display.innerHTML = '📁 Choose file...';
            wrapper.appendChild(display);

            // Update display when file is selected
            input.addEventListener('change', function () {
                if (this.files && this.files.length > 0) {
                    const fileName = this.files[0].name;
                    const fileSize = (this.files[0].size / 1024 / 1024).toFixed(2) + ' MB';
                    display.innerHTML = `📎 ${fileName} (${fileSize})`;
                    display.style.color = 'var(--success-green)';
                } else {
                    display.innerHTML = '📁 Choose file...';
                    display.style.color = 'var(--text-gray)';
                }
            });

            // Drag and drop functionality
            wrapper.addEventListener('dragover', function (e) {
                e.preventDefault();
                this.style.borderColor = 'var(--primary-red)';
                this.style.background = 'rgba(220, 38, 38, 0.1)';
            });

            wrapper.addEventListener('dragleave', function (e) {
                e.preventDefault();
                this.style.borderColor = '';
                this.style.background = '';
            });

            wrapper.addEventListener('drop', function (e) {
                e.preventDefault();
                this.style.borderColor = '';
                this.style.background = '';

                const files = e.dataTransfer.files;
                if (files.length > 0) {
                    input.files = files;
                    const event = new Event('change', { bubbles: true });
                    input.dispatchEvent(event);
                }
            });
        });
    }

    // ========== BUTTON LOADING STATES ========== 
    function addButtonLoadingStates() {
        const forms = document.querySelectorAll('form');

        forms.forEach(form => {
            form.addEventListener('submit', function () {
                const submitButton = this.querySelector('button[type="submit"], input[type="submit"]');
                if (submitButton) {
                    const originalText = submitButton.innerHTML;
                    submitButton.innerHTML = '⏳ Processing...';
                    submitButton.disabled = true;
                    submitButton.classList.add('loading');

                    // Re-enable button after 10 seconds as fallback
                    setTimeout(() => {
                        submitButton.innerHTML = originalText;
                        submitButton.disabled = false;
                        submitButton.classList.remove('loading');
                    }, 10000);
                }
            });
        });
    }

    // ========== TABLE ENHANCEMENTS ========== 
    function enhanceTableInteractions() {
        const tableRows = document.querySelectorAll('.table-row');

        tableRows.forEach(row => {
            // Add click animation
            row.addEventListener('click', function (e) {
                // Don't trigger if clicking on buttons
                if (!e.target.closest('.action-btn') && !e.target.closest('button')) {
                    this.style.transform = 'scale(0.98)';
                    setTimeout(() => {
                        this.style.transform = '';
                    }, 150);
                }
            });

            // Add hover sound effect (optional)
            row.addEventListener('mouseenter', function () {
                // You can add a subtle sound effect here if desired
                this.style.transition = 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)';
            });
        });
    }

    // ========== CATEGORY SELECTION ENHANCEMENTS ========== 
    function enhanceCategorySelection() {
        const categoryCheckboxes = document.querySelectorAll('.category-checkbox input[type="checkbox"]');

        categoryCheckboxes.forEach(checkbox => {
            const wrapper = checkbox.closest('.category-checkbox');

            checkbox.addEventListener('change', function () {
                if (this.checked) {
                    wrapper.style.background = 'rgba(220, 38, 38, 0.2)';
                    wrapper.style.borderColor = 'var(--primary-red)';
                    wrapper.style.transform = 'scale(1.02)';
                } else {
                    wrapper.style.background = '';
                    wrapper.style.borderColor = '';
                    wrapper.style.transform = '';
                }
            });

            // Initialize state
            if (checkbox.checked) {
                wrapper.style.background = 'rgba(220, 38, 38, 0.2)';
                wrapper.style.borderColor = 'var(--primary-red)';
            }
        });
    }

    // ========== FORM VALIDATION ENHANCEMENTS ========== 
    function enhanceFormValidation() {
        const forms = document.querySelectorAll('form');

        forms.forEach(form => {
            const inputs = form.querySelectorAll('.form-control');

            inputs.forEach(input => {
                input.addEventListener('blur', function () {
                    const formGroup = this.closest('.form-group');
                    const isValid = this.checkValidity();

                    if (!isValid) {
                        formGroup.classList.add('has-error');
                        this.style.animation = 'shake 0.5s ease-in-out';
                    } else {
                        formGroup.classList.remove('has-error');
                        this.style.animation = '';
                    }
                });

                input.addEventListener('input', function () {
                    const formGroup = this.closest('.form-group');
                    if (this.checkValidity()) {
                        formGroup.classList.remove('has-error');
                        this.style.animation = '';
                    }
                });
            });
        });

        // Add shake animation
        if (!document.getElementById('validation-styles')) {
            const validationStyles = document.createElement('style');
            validationStyles.id = 'validation-styles';
            validationStyles.textContent = `
                @keyframes shake {
                    0%, 100% { transform: translateX(0); }
                    10%, 30%, 50%, 70%, 90% { transform: translateX(-5px); }
                    20%, 40%, 60%, 80% { transform: translateX(5px); }
                }
            `;
            document.head.appendChild(validationStyles);
        }
    }

    // ========== NOTIFICATION SYSTEM ========== 
    function createNotificationSystem() {
        // Create notification container
        const notificationContainer = document.createElement('div');
        notificationContainer.id = 'notification-container';
        notificationContainer.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 3000;
            max-width: 400px;
        `;
        document.body.appendChild(notificationContainer);

        // Function to show notifications
        window.showNotification = function (message, type = 'info', duration = 5000) {
            const notification = document.createElement('div');
            notification.className = `notification notification-${type}`;
            notification.style.cssText = `
                background: var(--secondary-black);
                border: 1px solid var(--primary-red);
                border-radius: var(--border-radius);
                padding: 1rem 1.5rem;
                margin-bottom: 0.5rem;
                box-shadow: var(--shadow-dark);
                transform: translateX(100%);
                transition: all 0.3s ease;
                position: relative;
                backdrop-filter: blur(10px);
            `;

            const icon = type === 'success' ? '✅' : type === 'error' ? '❌' : 'ℹ️';
            notification.innerHTML = `
                <div style="display: flex; align-items: center; gap: 0.75rem;">
                    <span style="font-size: 1.2rem;">${icon}</span>
                    <span style="color: var(--text-light); font-weight: 500;">${message}</span>
                    <button onclick="this.parentElement.parentElement.remove()" 
                            style="margin-left: auto; background: none; border: none; color: var(--text-gray); cursor: pointer; font-size: 1.2rem;">×</button>
                </div>
            `;

            notificationContainer.appendChild(notification);

            // Animate in
            setTimeout(() => {
                notification.style.transform = 'translateX(0)';
            }, 100);

            // Auto remove
            if (duration > 0) {
                setTimeout(() => {
                    notification.style.transform = 'translateX(100%)';
                    setTimeout(() => {
                        if (notification.parentNode) {
                            notification.parentNode.removeChild(notification);
                        }
                    }, 300);
                }, duration);
            }
        };
    }

    // ========== INITIALIZE ALL ENHANCEMENTS ========== 
    initMobileSidebar();
    enhanceFormControls();
    enhanceFileUploads();
    addButtonLoadingStates();
    enhanceTableInteractions();
    enhanceCategorySelection();
    enhanceFormValidation();
    createNotificationSystem();

    // Show loading complete notification
    setTimeout(() => {
        if (typeof showNotification === 'function') {
            showNotification('🎬 Movie management system ready!', 'success', 3000);
        }
    }, 1000);

    console.log('🎬 Enhanced Movie Management System Initialized');
});