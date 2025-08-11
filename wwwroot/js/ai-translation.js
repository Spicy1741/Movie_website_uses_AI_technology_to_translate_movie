// AI Translation JavaScript
document.addEventListener('DOMContentLoaded', function () {
    initializeAITranslation();
});

function initializeAITranslation() {
    const fileInput = document.getElementById('videoFile');
    const uploadForm = document.getElementById('uploadForm');
    const submitBtn = document.getElementById('submitBtn');
    const progressSection = document.getElementById('progressSection');

    // File input change handler
    if (fileInput) {
        fileInput.addEventListener('change', handleFileSelect);
    }

    // Form submit handler
    if (uploadForm) {
        uploadForm.addEventListener('submit', handleFormSubmit);
    }

    // Initialize file upload display
    updateFileDisplay();
}

function handleFileSelect(event) {
    const file = event.target.files[0];
    const fileWrapper = document.querySelector('.file-upload-wrapper');
    const fileDisplay = document.querySelector('.file-upload-display');

    if (file) {
        // Validate file type
        const allowedTypes = ['.mp4', '.avi', '.mov', '.wmv', '.flv', '.webm', '.mkv'];
        const fileExtension = '.' + file.name.split('.').pop().toLowerCase();

        if (!allowedTypes.includes(fileExtension)) {
            showNotification('Please select a valid video file format', 'error');
            event.target.value = '';
            return;
        }

        // Validate file size (500MB max)
        const maxSize = 500 * 1024 * 1024; // 500MB in bytes
        if (file.size > maxSize) {
            showNotification('File size too large. Maximum size is 500MB', 'error');
            event.target.value = '';
            return;
        }

        // Update display
        fileWrapper.classList.add('file-selected');
        fileDisplay.innerHTML = `
            <i class="fas fa-check-circle"></i>
            <span>Selected: ${file.name}</span>
            <small style="display: block; margin-top: 8px; color: #9ca3af;">
                Size: ${formatFileSize(file.size)}
            </small>
        `;

        showNotification('Video file selected successfully', 'success');
    } else {
        // Reset display
        fileWrapper.classList.remove('file-selected');
        updateFileDisplay();
    }
}

function updateFileDisplay() {
    const fileDisplay = document.querySelector('.file-upload-display');
    if (fileDisplay) {
        fileDisplay.innerHTML = `
            <i class="fas fa-cloud-upload-alt"></i>
            <span>Click to select video</span>
        `;
    }
}

function handleFormSubmit(event) {
    const fileInput = document.getElementById('videoFile');
    const sourceLanguage = document.getElementById('sourceLanguage');
    const targetLanguage = document.getElementById('targetLanguage');

    // Validate form
    if (!fileInput.files[0]) {
        event.preventDefault();
        showNotification('Please select a video file', 'warning');
        return false;
    }

    if (sourceLanguage.value === targetLanguage.value && sourceLanguage.value !== 'auto') {
        event.preventDefault();
        showNotification('Source and target languages cannot be the same', 'warning');
        return false;
    }

    // Show progress
    showProcessingProgress();

    // Disable submit button
    const submitBtn = document.getElementById('submitBtn');
    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = `
            <i class="fas fa-spinner fa-spin me-2"></i>
            Processing...
        `;
    }

    showNotification('Starting video processing...', 'info');
}

function showProcessingProgress() {
    const progressSection = document.getElementById('progressSection');
    if (progressSection) {
        progressSection.style.display = 'block';
        progressSection.scrollIntoView({ behavior: 'smooth' });

        // Animate processing steps
        animateProcessingSteps();
    }
}

function animateProcessingSteps() {
    const steps = document.querySelectorAll('.processing-step');
    const icons = [
        'fas fa-check text-success',
        'fas fa-spinner fa-spin text-warning',
        'fas fa-clock text-muted',
        'fas fa-clock text-muted',
        'fas fa-clock text-muted'
    ];

    steps.forEach((step, index) => {
        setTimeout(() => {
            const icon = step.querySelector('i');
            if (index === 1) {
                // Current step - show spinner
                icon.className = 'fas fa-spinner fa-spin text-warning me-2';
            } else if (index > 1) {
                // Future steps - show clock
                icon.className = 'fas fa-clock text-muted me-2';
            }
        }, index * 1000);
    });
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `alert alert-${getAlertClass(type)} alert-dismissible fade show position-fixed`;
    notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; max-width: 400px;';

    notification.innerHTML = `
        <i class="fas fa-${getNotificationIcon(type)} me-2"></i>
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    document.body.appendChild(notification);

    // Auto remove after 5 seconds
    setTimeout(() => {
        if (notification.parentNode) {
            notification.remove();
        }
    }, 5000);
}

function getAlertClass(type) {
    const classes = {
        'success': 'success',
        'error': 'danger',
        'warning': 'warning',
        'info': 'info'
    };
    return classes[type] || 'info';
}

function getNotificationIcon(type) {
    const icons = {
        'success': 'check-circle',
        'error': 'exclamation-triangle',
        'warning': 'exclamation-circle',
        'info': 'info-circle'
    };
    return icons[type] || 'info-circle';
}

// Language selector enhancement
function initializeLanguageSelectors() {
    const selectors = document.querySelectorAll('.language-select');

    selectors.forEach(selector => {
        selector.addEventListener('change', function () {
            this.style.color = '#ffffff';
        });
    });
}

// Initialize language selectors when DOM is ready
document.addEventListener('DOMContentLoaded', initializeLanguageSelectors);

// File drag and drop enhancement
function initializeDragAndDrop() {
    const fileWrapper = document.querySelector('.file-upload-wrapper');
    const fileInput = document.getElementById('videoFile');

    if (!fileWrapper || !fileInput) return;

    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        fileWrapper.addEventListener(eventName, preventDefaults, false);
    });

    ['dragenter', 'dragover'].forEach(eventName => {
        fileWrapper.addEventListener(eventName, highlight, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        fileWrapper.addEventListener(eventName, unhighlight, false);
    });

    fileWrapper.addEventListener('drop', handleDrop, false);

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    function highlight(e) {
        fileWrapper.classList.add('dragover');
    }

    function unhighlight(e) {
        fileWrapper.classList.remove('dragover');
    }

    function handleDrop(e) {
        const dt = e.dataTransfer;
        const files = dt.files;

        if (files.length > 0) {
            fileInput.files = files;
            handleFileSelect({ target: fileInput });
        }
    }
}

// Initialize drag and drop when DOM is ready
document.addEventListener('DOMContentLoaded', initializeDragAndDrop);