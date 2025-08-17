// Enhanced JavaScript for Movie Management Pages - Modern Cinema Admin UI
// Advanced admin dashboard with modern interactions and effects

document.addEventListener('DOMContentLoaded', function () {
    // Initialize all enhanced features
    initializeModernAdminSystem();
});

function initializeModernAdminSystem() {
    console.log('🎬 Initializing Enhanced Cinema Admin System...');
    
    // Core system initialization
    initMobileSidebar();
    initModernParticleBackground();
    enhanceFormControls();
    enhanceFileUploads();
    addButtonLoadingStates();
    enhanceTableInteractions();
    enhanceCategorySelection();
    enhanceFormValidation();
    createNotificationSystem();
    initModernScrollEffects();
    initAdvancedAnimations();
    initThemeSystem();
    initKeyboardShortcuts();
    initAdvancedSearch();
    initDataVisualization();
    
    // Show startup notification
    setTimeout(() => {
        if (typeof showNotification === 'function') {
            showNotification('🎬 Enhanced Cinema Admin System Ready!', 'success', 3000);
        }
    }, 1000);
}

// ========== ENHANCED MOBILE SIDEBAR WITH MODERN ANIMATIONS ==========
function initMobileSidebar() {
    const mobileToggle = document.createElement('button');
    mobileToggle.className = 'mobile-menu-toggle';
    mobileToggle.innerHTML = `
        <span></span>
        <span></span>
        <span></span>
    `;
    
    // Enhanced mobile toggle styles
    mobileToggle.style.cssText = `
        display: none;
        position: fixed;
        top: 1.5rem;
        left: 1.5rem;
        z-index: 2001;
        background: linear-gradient(135deg, var(--primary-red), var(--dark-red));
        color: white;
        border: none;
        border-radius: 12px;
        padding: 1rem;
        cursor: pointer;
        box-shadow: 0 8px 25px rgba(220, 38, 38, 0.4);
        transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
        backdrop-filter: blur(15px);
        border: 1px solid rgba(255, 255, 255, 0.2);
    `;

    const mobileStyles = document.createElement('style');
    mobileStyles.textContent = `
        @media (max-width: 992px) {
            .mobile-menu-toggle {
                display: flex !important;
                flex-direction: column;
                justify-content: center;
                align-items: center;
                gap: 4px;
            }
            
            .mobile-menu-toggle span {
                width: 26px;
                height: 3px;
                background: white;
                border-radius: 2px;
                transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
                transform-origin: center;
            }
            
            .mobile-menu-toggle.active span:nth-child(1) {
                transform: rotate(45deg) translate(7px, 7px);
            }
            
            .mobile-menu-toggle.active span:nth-child(2) {
                opacity: 0;
                transform: translateX(20px);
            }
            
            .mobile-menu-toggle.active span:nth-child(3) {
                transform: rotate(-45deg) translate(7px, -7px);
            }
        }
        
        .mobile-menu-toggle:hover {
            background: linear-gradient(135deg, var(--light-red), var(--primary-red)) !important;
            transform: scale(1.05) !important;
            box-shadow: 0 12px 35px rgba(220, 38, 38, 0.6) !important;
        }
        
        .sidebar.mobile-open {
            transform: translateX(0) !important;
            box-shadow: 
                20px 0 60px rgba(0, 0, 0, 0.8),
                0 0 100px rgba(220, 38, 38, 0.3) !important;
        }
        
        .mobile-overlay {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0, 0, 0, 0.7);
            backdrop-filter: blur(5px);
            z-index: 1999;
            opacity: 0;
            visibility: hidden;
            transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
        }
        
        .mobile-overlay.active {
            opacity: 1;
            visibility: visible;
        }
    `;
    
    document.head.appendChild(mobileStyles);
    document.body.appendChild(mobileToggle);

    // Create mobile overlay
    const overlay = document.createElement('div');
    overlay.className = 'mobile-overlay';
    document.body.appendChild(overlay);

    const sidebar = document.querySelector('.sidebar');
    let isOpen = false;

    function toggleMobileMenu() {
        isOpen = !isOpen;
        mobileToggle.classList.toggle('active', isOpen);
        sidebar?.classList.toggle('mobile-open', isOpen);
        overlay.classList.toggle('active', isOpen);
        
        // Prevent body scroll when menu is open
        document.body.style.overflow = isOpen ? 'hidden' : '';
    }

    mobileToggle.addEventListener('click', toggleMobileMenu);
    overlay.addEventListener('click', toggleMobileMenu);

    // Close on escape key
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && isOpen) {
            toggleMobileMenu();
        }
    });

    // Close on window resize if desktop
    window.addEventListener('resize', () => {
        if (window.innerWidth > 992 && isOpen) {
            toggleMobileMenu();
        }
    });
}

// ========== MODERN PARTICLE BACKGROUND SYSTEM ==========
function initModernParticleBackground() {
    const canvas = document.createElement('canvas');
    canvas.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        z-index: -1;
        pointer-events: none;
        opacity: 0.3;
    `;
    document.body.appendChild(canvas);

    const ctx = canvas.getContext('2d');
    let particles = [];
    let mouse = { x: 0, y: 0 };

    function resizeCanvas() {
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
    }

    function createParticle() {
        return {
            x: Math.random() * canvas.width,
            y: Math.random() * canvas.height,
            vx: (Math.random() - 0.5) * 0.5,
            vy: (Math.random() - 0.5) * 0.5,
            size: Math.random() * 2 + 1,
            opacity: Math.random() * 0.5 + 0.2,
            color: `rgba(220, 38, 38, ${Math.random() * 0.3 + 0.1})`
        };
    }

    function initParticles() {
        particles = [];
        for (let i = 0; i < 50; i++) {
            particles.push(createParticle());
        }
    }

    function updateParticles() {
        particles.forEach(particle => {
            particle.x += particle.vx;
            particle.y += particle.vy;

            // Mouse interaction
            const dx = mouse.x - particle.x;
            const dy = mouse.y - particle.y;
            const distance = Math.sqrt(dx * dx + dy * dy);
            
            if (distance < 100) {
                particle.vx += dx * 0.0001;
                particle.vy += dy * 0.0001;
            }

            // Boundary check
            if (particle.x < 0 || particle.x > canvas.width) particle.vx *= -1;
            if (particle.y < 0 || particle.y > canvas.height) particle.vy *= -1;

            // Keep particles in bounds
            particle.x = Math.max(0, Math.min(canvas.width, particle.x));
            particle.y = Math.max(0, Math.min(canvas.height, particle.y));
        });
    }

    function drawParticles() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        
        particles.forEach(particle => {
            ctx.beginPath();
            ctx.arc(particle.x, particle.y, particle.size, 0, Math.PI * 2);
            ctx.fillStyle = particle.color;
            ctx.fill();

            // Connect nearby particles
            particles.forEach(otherParticle => {
                const dx = particle.x - otherParticle.x;
                const dy = particle.y - otherParticle.y;
                const distance = Math.sqrt(dx * dx + dy * dy);
                
                if (distance < 80) {
                    ctx.beginPath();
                    ctx.moveTo(particle.x, particle.y);
                    ctx.lineTo(otherParticle.x, otherParticle.y);
                    ctx.strokeStyle = `rgba(220, 38, 38, ${0.1 * (1 - distance / 80)})`;
                    ctx.lineWidth = 0.5;
                    ctx.stroke();
                }
            });
        });
    }

    function animate() {
        updateParticles();
        drawParticles();
        requestAnimationFrame(animate);
    }

    // Event listeners
    window.addEventListener('resize', resizeCanvas);
    document.addEventListener('mousemove', (e) => {
        mouse.x = e.clientX;
        mouse.y = e.clientY;
    });

    // Initialize
    resizeCanvas();
    initParticles();
    animate();
}

// ========== ENHANCED FORM INTERACTIONS WITH MODERN UX ==========
function enhanceFormControls() {
    const formControls = document.querySelectorAll('.form-control, .form-control-file');

    formControls.forEach(control => {
        const formGroup = control.closest('.form-group');
        const label = formGroup?.querySelector('label');

        // Enhanced focus effects
        control.addEventListener('focus', function() {
            formGroup?.classList.add('focused');
            createRippleEffect(this);
            
            if (label) {
                label.style.transform = 'translateY(-25px) scale(0.85)';
                label.style.color = 'var(--primary-red)';
            }
        });

        control.addEventListener('blur', function() {
            formGroup?.classList.remove('focused');
            
            if (label && !this.value) {
                label.style.transform = '';
                label.style.color = '';
            }
            
            // Check if field has value
            if (this.value) {
                formGroup?.classList.add('has-value');
            } else {
                formGroup?.classList.remove('has-value');
            }
        });

        // Real-time validation
        control.addEventListener('input', function() {
            debounce(validateField, 300)(this);
        });

        // Initialize state
        if (control.value) {
            formGroup?.classList.add('has-value');
        }
    });
}

function createRippleEffect(element) {
    const rect = element.getBoundingClientRect();
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
        z-index: 1;
    `;

    element.style.position = 'relative';
    element.appendChild(ripple);

    setTimeout(() => {
        ripple.remove();
    }, 600);
}

// ========== ADVANCED FILE UPLOAD WITH DRAG & DROP ==========
function enhanceFileUploads() {
    const fileInputs = document.querySelectorAll('input[type="file"]');

    fileInputs.forEach(input => {
        const wrapper = createAdvancedFileWrapper(input);
        setupAdvancedFileEvents(wrapper, input);
    });
}

function createAdvancedFileWrapper(input) {
    const wrapper = document.createElement('div');
    wrapper.className = 'advanced-file-wrapper';
    wrapper.style.cssText = `
        position: relative;
        border: 2px dashed rgba(220, 38, 38, 0.3);
        border-radius: 16px;
        padding: 3rem 2rem;
        text-align: center;
        background: linear-gradient(135deg, rgba(220, 38, 38, 0.05), rgba(220, 38, 38, 0.02));
        backdrop-filter: blur(10px);
        transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
        cursor: pointer;
        overflow: hidden;
    `;

    const display = document.createElement('div');
    display.className = 'file-upload-display';
    display.innerHTML = `
        <div class="upload-icon">📁</div>
        <div class="upload-text">
            <h4>Drop files here or click to browse</h4>
            <p>Supports: ${getAcceptedFormats(input)}</p>
        </div>
        <div class="upload-progress" style="display: none;">
            <div class="progress-bar"></div>
            <div class="progress-text">Uploading...</div>
        </div>
    `;

    // Insert wrapper
    input.parentNode.insertBefore(wrapper, input);
    wrapper.appendChild(input);
    wrapper.appendChild(display);
    
    // Hide original input
    input.style.opacity = '0';
    input.style.position = 'absolute';
    input.style.width = '100%';
    input.style.height = '100%';
    input.style.cursor = 'pointer';

    return wrapper;
}

function setupAdvancedFileEvents(wrapper, input) {
    const display = wrapper.querySelector('.file-upload-display');
    const uploadText = display.querySelector('.upload-text');
    const uploadProgress = display.querySelector('.upload-progress');

    // File selection
    input.addEventListener('change', function() {
        handleAdvancedFileSelection(wrapper, this);
    });

    // Drag and drop
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        wrapper.addEventListener(eventName, preventDefaults, false);
    });

    ['dragenter', 'dragover'].forEach(eventName => {
        wrapper.addEventListener(eventName, () => {
            wrapper.classList.add('drag-over');
            wrapper.style.borderColor = 'var(--primary-red)';
            wrapper.style.background = 'linear-gradient(135deg, rgba(220, 38, 38, 0.1), rgba(220, 38, 38, 0.05))';
            wrapper.style.transform = 'scale(1.02)';
        });
    });

    ['dragleave', 'drop'].forEach(eventName => {
        wrapper.addEventListener(eventName, () => {
            wrapper.classList.remove('drag-over');
            wrapper.style.borderColor = '';
            wrapper.style.background = '';
            wrapper.style.transform = '';
        });
    });

    wrapper.addEventListener('drop', function(e) {
        const files = e.dataTransfer.files;
        if (files.length > 0) {
            input.files = files;
            handleAdvancedFileSelection(wrapper, input);
        }
    });
}

function handleAdvancedFileSelection(wrapper, input) {
    const file = input.files[0];
    const display = wrapper.querySelector('.file-upload-display');
    const uploadText = display.querySelector('.upload-text');
    
    if (file) {
        wrapper.classList.add('has-file');
        uploadText.innerHTML = `
            <div class="file-info">
                <div class="file-icon">${getFileIcon(file.type)}</div>
                <div class="file-details">
                    <h4>${file.name}</h4>
                    <p>${formatFileSize(file.size)} • ${file.type || 'Unknown type'}</p>
                </div>
                <button type="button" class="remove-file" onclick="removeFile(this)">×</button>
            </div>
        `;
        
        // Show preview for images
        if (file.type.startsWith('image/')) {
            showImagePreview(wrapper, file);
        }
        
        // Simulate upload progress
        simulateUploadProgress(wrapper);
    }
}

function getAcceptedFormats(input) {
    const accept = input.getAttribute('accept');
    if (!accept) return 'All files';
    
    return accept.split(',').map(format => format.trim().toUpperCase()).join(', ');
}

function getFileIcon(type) {
    if (type.startsWith('image/')) return '🖼️';
    if (type.startsWith('video/')) return '🎬';
    if (type.startsWith('audio/')) return '🎵';
    if (type.includes('pdf')) return '📄';
    return '📁';
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function simulateUploadProgress(wrapper) {
    const progressBar = wrapper.querySelector('.progress-bar');
    const uploadProgress = wrapper.querySelector('.upload-progress');
    
    if (!progressBar || !uploadProgress) return;
    
    uploadProgress.style.display = 'block';
    
    let progress = 0;
    const interval = setInterval(() => {
        progress += Math.random() * 15;
        progress = Math.min(progress, 100);
        
        progressBar.style.width = progress + '%';
        
        if (progress >= 100) {
            clearInterval(interval);
            setTimeout(() => {
                uploadProgress.style.display = 'none';
            }, 500);
        }
    }, 100);
}

function removeFile(button) {
    const wrapper = button.closest('.advanced-file-wrapper');
    const input = wrapper.querySelector('input[type="file"]');
    const display = wrapper.querySelector('.file-upload-display');
    
    input.value = '';
    wrapper.classList.remove('has-file');
    
    display.querySelector('.upload-text').innerHTML = `
        <h4>Drop files here or click to browse</h4>
        <p>Supports: ${getAcceptedFormats(input)}</p>
    `;
}

// ========== ENHANCED BUTTON LOADING STATES ==========
function addButtonLoadingStates() {
    const forms = document.querySelectorAll('form');
    
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            const submitButton = this.querySelector('button[type="submit"], input[type="submit"], .action-btn:not(.secondary)');
            
            if (submitButton && !submitButton.classList.contains('loading')) {
                startButtonLoading(submitButton);
                
                // Re-enable after timeout as fallback
                setTimeout(() => {
                    stopButtonLoading(submitButton);
                }, 15000);
            }
        });
    });
}

function startButtonLoading(button) {
    button.classList.add('loading');
    button.disabled = true;
    
    const originalContent = button.innerHTML;
    button.setAttribute('data-original-content', originalContent);
    
    button.innerHTML = `
        <span class="loading-spinner"></span>
        <span>Processing...</span>
    `;
    
    // Add loading spinner styles
    if (!document.getElementById('loading-spinner-styles')) {
        const styles = document.createElement('style');
        styles.id = 'loading-spinner-styles';
        styles.textContent = `
            .loading-spinner {
                width: 20px;
                height: 20px;
                border: 2px solid transparent;
                border-top: 2px solid currentColor;
                border-radius: 50%;
                animation: spin 1s linear infinite;
                display: inline-block;
                margin-right: 0.5rem;
            }
            
            @keyframes spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }
        `;
        document.head.appendChild(styles);
    }
}

function stopButtonLoading(button) {
    button.classList.remove('loading');
    button.disabled = false;
    
    const originalContent = button.getAttribute('data-original-content');
    if (originalContent) {
        button.innerHTML = originalContent;
        button.removeAttribute('data-original-content');
    }
}

// ========== ENHANCED TABLE INTERACTIONS ==========
function enhanceTableInteractions() {
    const tableRows = document.querySelectorAll('.table-row');
    
    tableRows.forEach((row, index) => {
        // Add animation delay
        row.style.animationDelay = `${index * 0.1}s`;
        
        // Enhanced hover effects
        row.addEventListener('mouseenter', function() {
            this.style.transform = 'translateX(8px) scale(1.01)';
            this.style.zIndex = '10';
        });
        
        row.addEventListener('mouseleave', function() {
            this.style.transform = '';
            this.style.zIndex = '';
        });
        
        // Click animation
        row.addEventListener('click', function(e) {
            if (!e.target.closest('.action-btn') && !e.target.closest('button')) {
                createClickEffect(this, e);
            }
        });
    });
}

function createClickEffect(element, event) {
    const rect = element.getBoundingClientRect();
    const clickX = event.clientX - rect.left;
    const clickY = event.clientY - rect.top;
    
    const ripple = document.createElement('div');
    ripple.style.cssText = `
        position: absolute;
        left: ${clickX}px;
        top: ${clickY}px;
        width: 0;
        height: 0;
        border-radius: 50%;
        background: rgba(220, 38, 38, 0.3);
        transform: translate(-50%, -50%);
        animation: clickRipple 0.6s ease-out;
        pointer-events: none;
        z-index: 1;
    `;
    
    element.style.position = 'relative';
    element.appendChild(ripple);
    
    // Add click ripple animation
    if (!document.getElementById('click-ripple-styles')) {
        const styles = document.createElement('style');
        styles.id = 'click-ripple-styles';
        styles.textContent = `
            @keyframes clickRipple {
                to {
                    width: 100px;
                    height: 100px;
                    opacity: 0;
                }
            }
        `;
        document.head.appendChild(styles);
    }
    
    setTimeout(() => {
        ripple.remove();
    }, 600);
}

// ========== ENHANCED CATEGORY SELECTION ==========
function enhanceCategorySelection() {
    const categoryCheckboxes = document.querySelectorAll('.category-checkbox input[type="checkbox"], .category-item input[type="checkbox"]');
    
    categoryCheckboxes.forEach(checkbox => {
        const wrapper = checkbox.closest('.category-checkbox') || checkbox.closest('.category-item');
        
        // Enhanced selection effects
        checkbox.addEventListener('change', function() {
            updateCategoryState(wrapper, this.checked);
            updateCategoryCount();
            
            // Play selection sound (optional)
            if (this.checked) {
                playSelectionFeedback();
            }
        });
        
        // Initialize state
        if (checkbox.checked) {
            updateCategoryState(wrapper, true);
        }
    });
    
    // Initialize category count
    updateCategoryCount();
}

function updateCategoryState(wrapper, isChecked) {
    if (isChecked) {
        wrapper.classList.add('selected');
        wrapper.style.background = 'rgba(220, 38, 38, 0.2)';
        wrapper.style.borderColor = 'var(--primary-red)';
        wrapper.style.transform = 'scale(1.02)';
        wrapper.style.boxShadow = '0 8px 25px rgba(220, 38, 38, 0.3)';
    } else {
        wrapper.classList.remove('selected');
        wrapper.style.background = '';
        wrapper.style.borderColor = '';
        wrapper.style.transform = '';
        wrapper.style.boxShadow = '';
    }
}

function updateCategoryCount() {
    const categorySection = document.querySelector('.category-section');
    const checkedBoxes = document.querySelectorAll('.category-section input[type="checkbox"]:checked');
    
    if (categorySection) {
        categorySection.setAttribute('data-selected-count', checkedBoxes.length);
    }
}

function playSelectionFeedback() {
    // Create audio feedback (optional)
    const audio = new Audio('data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBTuO0vLF9IbeKqQKMHXF7+GTRwsYaLbq46ZUEA5OqOXvuGkeA++F0vPAeC4Fmh1fvXW19hGv2g7Z+vQB8AOEiqGAOAj7Rno8AabIf4dkM2E6A1qT9S0FK+A6dh');
    audio.volume = 0.1;
    audio.play().catch(() => {}); // Ignore errors if audio doesn't play
}

// ========== ENHANCED FORM VALIDATION ==========
function enhanceFormValidation() {
    const forms = document.querySelectorAll('form');
    
    forms.forEach(form => {
        const inputs = form.querySelectorAll('.form-control');
        
        inputs.forEach(input => {
            input.addEventListener('blur', function() {
                validateField(this);
            });
            
            input.addEventListener('input', function() {
                debounce(validateField, 300)(this);
            });
        });
        
        // Form submission validation
        form.addEventListener('submit', function(e) {
            if (!validateForm(this)) {
                e.preventDefault();
                showNotification('Please fix the errors before submitting', 'error');
            }
        });
    });
}

function validateField(field) {
    const formGroup = field.closest('.form-group');
    const isValid = field.checkValidity() && validateCustomRules(field);
    
    formGroup.classList.toggle('has-error', !isValid);
    
    if (!isValid) {
        field.style.animation = 'shake 0.5s ease-in-out';
        createValidationMessage(formGroup, getValidationMessage(field));
    } else {
        field.style.animation = '';
        removeValidationMessage(formGroup);
    }
    
    setTimeout(() => {
        field.style.animation = '';
    }, 500);
    
    return isValid;
}

function validateCustomRules(field) {
    // Add custom validation rules here
    if (field.type === 'email') {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(field.value);
    }
    
    if (field.name === 'releaseYear') {
        const year = parseInt(field.value);
        return year >= 1888 && year <= new Date().getFullYear() + 5;
    }
    
    return true;
}

function getValidationMessage(field) {
    if (field.validity.valueMissing) {
        return `${field.labels[0]?.textContent || 'Field'} is required`;
    }
    
    if (field.validity.typeMismatch) {
        return `Please enter a valid ${field.type}`;
    }
    
    if (field.name === 'releaseYear') {
        return 'Please enter a valid year between 1888 and next year';
    }
    
    return 'Please check this field';
}

function createValidationMessage(formGroup, message) {
    removeValidationMessage(formGroup);
    
    const errorDiv = document.createElement('div');
    errorDiv.className = 'validation-error';
    errorDiv.textContent = message;
    errorDiv.style.cssText = `
        color: var(--danger-red);
        font-size: 0.85rem;
        margin-top: 0.5rem;
        padding: 0.5rem 1rem;
        background: rgba(239, 68, 68, 0.1);
        border-radius: 6px;
        border-left: 3px solid var(--danger-red);
        animation: slideDown 0.3s ease-out;
    `;
    
    formGroup.appendChild(errorDiv);
}

function removeValidationMessage(formGroup) {
    const existing = formGroup.querySelector('.validation-error');
    if (existing) {
        existing.remove();
    }
}

function validateForm(form) {
    const inputs = form.querySelectorAll('.form-control[required]');
    let isValid = true;
    
    inputs.forEach(input => {
        if (!validateField(input)) {
            isValid = false;
        }
    });
    
    return isValid;
}

// ========== ENHANCED NOTIFICATION SYSTEM ==========
function createNotificationSystem() {
    const container = document.createElement('div');
    container.id = 'notification-container';
    container.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
        max-width: 400px;
        pointer-events: none;
    `;
    document.body.appendChild(container);
    
    window.showNotification = function(message, type = 'info', duration = 5000) {
        const notification = createNotificationElement(message, type);
        container.appendChild(notification);
        
        // Animate in
        requestAnimationFrame(() => {
            notification.style.transform = 'translateX(0)';
            notification.style.opacity = '1';
        });
        
        // Auto remove
        if (duration > 0) {
            setTimeout(() => {
                removeNotification(notification);
            }, duration);
        }
        
        return notification;
    };
}

function createNotificationElement(message, type) {
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.style.cssText = `
        background: linear-gradient(135deg, var(--secondary-black), var(--primary-black));
        border: 1px solid var(--${type === 'success' ? 'success-green' : type === 'error' ? 'danger-red' : 'primary-red'});
        border-radius: 12px;
        padding: 1rem 1.5rem;
        margin-bottom: 0.75rem;
        box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
        transform: translateX(100%);
        opacity: 0;
        transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
        backdrop-filter: blur(15px);
        pointer-events: auto;
        position: relative;
        overflow: hidden;
    `;
    
    const icons = {
        success: '✅',
        error: '❌',
        warning: '⚠️',
        info: 'ℹ️'
    };
    
    notification.innerHTML = `
        <div style="display: flex; align-items: center; gap: 0.75rem;">
            <span style="font-size: 1.2rem;">${icons[type] || icons.info}</span>
            <span style="color: var(--text-light); font-weight: 500; flex: 1;">${message}</span>
            <button onclick="removeNotification(this.parentElement.parentElement)" 
                    style="background: none; border: none; color: var(--text-gray); cursor: pointer; font-size: 1.2rem; padding: 0; width: 24px; height: 24px; display: flex; align-items: center; justify-content: center;">×</button>
        </div>
    `;
    
    // Add shimmer effect
    notification.addEventListener('mouseenter', () => {
        notification.style.transform = 'translateX(0) scale(1.02)';
    });
    
    notification.addEventListener('mouseleave', () => {
        notification.style.transform = 'translateX(0) scale(1)';
    });
    
    return notification;
}

function removeNotification(notification) {
    notification.style.transform = 'translateX(100%)';
    notification.style.opacity = '0';
    
    setTimeout(() => {
        if (notification.parentNode) {
            notification.parentNode.removeChild(notification);
        }
    }, 400);
}

// ========== MODERN SCROLL EFFECTS ==========
function initModernScrollEffects() {
    const elements = document.querySelectorAll('.content-card, .table-row, .category-item');
    
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.animation = 'fadeInUp 0.6s ease-out forwards';
            }
        });
    }, {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    });
    
    elements.forEach(el => {
        el.style.opacity = '0';
        el.style.transform = 'translateY(30px)';
        observer.observe(el);
    });
}

// ========== ADVANCED ANIMATIONS ==========
function initAdvancedAnimations() {
    // Add stagger animations to table rows
    const tableRows = document.querySelectorAll('.table-row');
    tableRows.forEach((row, index) => {
        row.style.animationDelay = `${index * 0.05}s`;
    });
    
    // Add hover animations to cards
    const cards = document.querySelectorAll('.content-card');
    cards.forEach(card => {
        card.addEventListener('mouseenter', () => {
            card.style.transform = 'translateY(-5px) scale(1.01)';
        });
        
        card.addEventListener('mouseleave', () => {
            card.style.transform = '';
        });
    });
}

// ========== THEME SYSTEM ==========
function initThemeSystem() {
    // Add theme toggle if needed
    const themeToggle = document.createElement('button');
    themeToggle.className = 'theme-toggle';
    themeToggle.innerHTML = '🌙';
    themeToggle.style.cssText = `
        position: fixed;
        bottom: 20px;
        right: 20px;
        background: var(--primary-red);
        color: white;
        border: none;
        border-radius: 50%;
        width: 50px;
        height: 50px;
        cursor: pointer;
        font-size: 1.2rem;
        z-index: 1000;
        box-shadow: 0 4px 15px rgba(220, 38, 38, 0.4);
        transition: all 0.3s ease;
    `;
    
    // Hide theme toggle for now as the design is already dark
    // document.body.appendChild(themeToggle);
}

// ========== KEYBOARD SHORTCUTS ==========
function initKeyboardShortcuts() {
    document.addEventListener('keydown', (e) => {
        // Ctrl/Cmd + S to save form
        if ((e.ctrlKey || e.metaKey) && e.key === 's') {
            e.preventDefault();
            const submitButton = document.querySelector('button[type="submit"]');
            if (submitButton) {
                submitButton.click();
                showNotification('Form submitted via keyboard shortcut', 'info', 2000);
            }
        }
        
        // Escape to close modals/notifications
        if (e.key === 'Escape') {
            const notifications = document.querySelectorAll('.notification');
            notifications.forEach(notification => {
                removeNotification(notification);
            });
        }
    });
}

// ========== ADVANCED SEARCH ==========
function initAdvancedSearch() {
    const searchInputs = document.querySelectorAll('input[type="search"], input[placeholder*="search" i]');
    
    searchInputs.forEach(input => {
        input.addEventListener('input', debounce(performSearch, 300));
    });
}

function performSearch(input) {
    const query = input.value.toLowerCase();
    const searchableElements = document.querySelectorAll('.table-row, .movie-card');
    
    searchableElements.forEach(element => {
        const text = element.textContent.toLowerCase();
        const matches = text.includes(query);
        
        element.style.display = matches ? '' : 'none';
        
        if (matches && query) {
            highlightSearchTerm(element, query);
        } else {
            removeHighlights(element);
        }
    });
}

// ========== DATA VISUALIZATION ==========
function initDataVisualization() {
    // Add simple data visualization for admin stats
    const statsContainer = document.querySelector('.stats-container');
    if (statsContainer) {
        createStatsCharts(statsContainer);
    }
}

// ========== UTILITY FUNCTIONS ==========
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

function preventDefaults(e) {
    e.preventDefault();
    e.stopPropagation();
}

function showImagePreview(container, file) {
    const reader = new FileReader();
    reader.onload = function(e) {
        const preview = document.createElement('div');
        preview.className = 'image-preview';
        preview.style.cssText = `
            margin-top: 1rem;
            text-align: center;
        `;
        
        preview.innerHTML = `
            <img src="${e.target.result}" alt="Preview" style="
                max-width: 150px;
                max-height: 150px;
                border-radius: 8px;
                box-shadow: 0 4px 15px rgba(0, 0, 0, 0.3);
            ">
        `;
        
        container.appendChild(preview);
    };
    reader.readAsDataURL(file);
}

// Console log for debugging
console.log('🎬 Enhanced Cinema Admin System Loaded Successfully!');