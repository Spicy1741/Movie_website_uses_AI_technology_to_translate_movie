// Custom File Upload Handler for EditMovie
document.addEventListener('DOMContentLoaded', function () {
    // Initialize all file upload containers
    initializeFileUploads();

    function initializeFileUploads() {
        const fileUploads = document.querySelectorAll('.file-upload-container');

        fileUploads.forEach(container => {
            const input = container.querySelector('input[type="file"]');
            const removeBtn = container.querySelector('.remove-file-btn');

            if (!input) return;

            // Setup event listeners
            setupFileUploadEvents(container, input, removeBtn);
        });
    }

    function setupFileUploadEvents(container, input, removeBtn) {
        // Click to upload
        container.addEventListener('click', (e) => {
            if (e.target !== removeBtn) {
                input.click();
            }
        });

        // File selection
        input.addEventListener('change', (e) => {
            handleFileSelection(container, input);
        });

        // Remove file
        if (removeBtn) {
            removeBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                removeFile(container, input);
            });
        }

        // Drag and drop
        setupDragAndDrop(container, input);

        // Validation
        input.addEventListener('change', () => {
            validateFileUpload(container, input);
        });
    }

    function handleFileSelection(container, input) {
        const file = input.files[0];
        const fileInfo = container.querySelector('.file-selected-info');
        const fileName = fileInfo?.querySelector('.file-name');
        const fileSize = fileInfo?.querySelector('.file-size');

        if (file) {
            container.classList.add('has-file');

            if (fileName) {
                fileName.textContent = file.name;
            }

            if (fileSize) {
                fileSize.textContent = formatFileSize(file.size);
            }

            // Update container text if no separate info element
            if (!fileInfo) {
                const textElement = container.querySelector('.file-upload-text');
                if (textElement) {
                    textElement.innerHTML = `📁 ${file.name}<br><small>${formatFileSize(file.size)}</small>`;
                }
            }

            // Show preview for images
            if (file.type.startsWith('image/')) {
                showImagePreview(container, file);
            }
        }
    }

    function removeFile(container, input) {
        input.value = '';
        container.classList.remove('has-file', 'invalid');

        // Reset text
        const textElement = container.querySelector('.file-upload-text');
        const originalText = container.getAttribute('data-original-text');

        if (textElement && originalText) {
            textElement.textContent = originalText;
        }

        // Remove preview
        const preview = container.querySelector('.file-preview');
        if (preview) {
            preview.remove();
        }

        // Clear validation message
        const validationMsg = container.querySelector('.file-validation-message');
        if (validationMsg) {
            validationMsg.style.opacity = '0';
        }
    }

    function setupDragAndDrop(container, input) {
        // Prevent default drag behaviors
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            container.addEventListener(eventName, preventDefaults, false);
            document.body.addEventListener(eventName, preventDefaults, false);
        });

        // Highlight drop area when item is dragged over it
        ['dragenter', 'dragover'].forEach(eventName => {
            container.addEventListener(eventName, () => {
                container.classList.add('drag-over');
            });
        });

        ['dragleave', 'drop'].forEach(eventName => {
            container.addEventListener(eventName, () => {
                container.classList.remove('drag-over');
            });
        });

        // Handle dropped files
        container.addEventListener('drop', (e) => {
            const dt = e.dataTransfer;
            const files = dt.files;

            if (files.length > 0) {
                input.files = files;
                handleFileSelection(container, input);
                validateFileUpload(container, input);
            }
        });
    }

    function validateFileUpload(container, input) {
        const file = input.files[0];
        const validationMsg = container.querySelector('.file-validation-message');

        if (!file) return;

        let isValid = true;
        let message = '';

        // Get upload type from container classes
        const uploadType = getUploadType(container);

        // Validate based on upload type
        switch (uploadType) {
            case 'movie':
                isValid = validateMovieFile(file);
                message = 'Please select a valid video file (MP4, AVI, MKV, MOV)';
                break;
            case 'thumbnail':
                isValid = validateImageFile(file);
                message = 'Please select a valid image file (JPG, PNG, WEBP)';
                break;
            case 'subtitle':
                isValid = validateSubtitleFile(file);
                message = 'Please select a valid subtitle file (SRT, VTT)';
                break;
        }

        // File size validation (50MB for videos, 5MB for images, 1MB for subtitles)
        const maxSizes = {
            movie: 50 * 1024 * 1024, // 50MB
            thumbnail: 5 * 1024 * 1024, // 5MB
            subtitle: 1 * 1024 * 1024 // 1MB
        };

        if (file.size > maxSizes[uploadType]) {
            isValid = false;
            message = `File size must be less than ${formatFileSize(maxSizes[uploadType])}`;
        }

        // Update UI based on validation
        if (isValid) {
            container.classList.remove('invalid');
            if (validationMsg) {
                validationMsg.style.opacity = '0';
            }
        } else {
            container.classList.add('invalid');
            if (validationMsg) {
                validationMsg.textContent = message;
                validationMsg.style.opacity = '1';
            }
        }

        return isValid;
    }

    function getUploadType(container) {
        if (container.classList.contains('movie-file-upload')) return 'movie';
        if (container.classList.contains('thumbnail-upload')) return 'thumbnail';
        if (container.classList.contains('subtitle-upload')) return 'subtitle';
        return 'unknown';
    }

    function validateMovieFile(file) {
        const allowedTypes = ['video/mp4', 'video/avi', 'video/x-msvideo', 'video/quicktime', 'video/x-matroska'];
        const allowedExtensions = ['.mp4', '.avi', '.mkv', '.mov', '.wmv'];

        return allowedTypes.includes(file.type) ||
            allowedExtensions.some(ext => file.name.toLowerCase().endsWith(ext));
    }

    function validateImageFile(file) {
        const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
        return allowedTypes.includes(file.type);
    }

    function validateSubtitleFile(file) {
        const allowedExtensions = ['.srt', '.vtt'];
        return allowedExtensions.some(ext => file.name.toLowerCase().endsWith(ext));
    }

    function showImagePreview(container, file) {
        const reader = new FileReader();

        reader.onload = function (e) {
            // Remove existing preview
            const existingPreview = container.querySelector('.file-preview');
            if (existingPreview) {
                existingPreview.remove();
            }

            // Create new preview
            const preview = document.createElement('div');
            preview.className = 'file-preview';
            preview.innerHTML = `
                <img src="${e.target.result}" alt="Preview" style="
                    max-width: 80px;
                    max-height: 60px;
                    border-radius: 4px;
                    object-fit: cover;
                    margin-top: 0.5rem;
                ">
            `;

            const fileInfo = container.querySelector('.file-selected-info');
            if (fileInfo) {
                fileInfo.appendChild(preview);
            } else {
                container.appendChild(preview);
            }
        };

        reader.readAsDataURL(file);
    }

    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';

        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));

        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    // Simulate upload progress (optional)
    function simulateUploadProgress(container) {
        const progressBar = container.querySelector('.upload-progress-bar');
        const progress = container.querySelector('.upload-progress');

        if (!progressBar || !progress) return;

        container.classList.add('uploading');
        progress.style.display = 'block';

        let width = 0;
        const interval = setInterval(() => {
            width += Math.random() * 10;
            progressBar.style.width = Math.min(width, 100) + '%';

            if (width >= 100) {
                clearInterval(interval);
                setTimeout(() => {
                    container.classList.remove('uploading');
                    progress.style.display = 'none';
                    progressBar.style.width = '0%';
                }, 500);
            }
        }, 100);
    }
});