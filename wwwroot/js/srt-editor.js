// SRT Editor JavaScript
let currentSrtFile = null;
let originalSubtitleContent = '';
let translatedSubtitleContent = '';
let isEditing = false;

// Initialize the editor when the page loads
document.addEventListener('DOMContentLoaded', function () {
    initializeSrtEditor();
});

function initializeSrtEditor() {
    const fileUploadArea = document.getElementById('fileUploadArea');
    const srtFileInput = document.getElementById('srtFileInput');

    // File input change handler
    srtFileInput.addEventListener('change', handleFileSelect);

    // Drag and drop handlers
    fileUploadArea.addEventListener('dragover', handleDragOver);
    fileUploadArea.addEventListener('dragleave', handleDragLeave);
    fileUploadArea.addEventListener('drop', handleFileDrop);
    fileUploadArea.addEventListener('click', () => srtFileInput.click());

    // Initialize stats
    updateStats();
    updateFileStatus('Ready');

    // Initialize button states
    updateButtonStates();

    // Show welcome message
    showNotification('SRT Editor initialized. Ready to process your subtitle files!', 'info');
}

// File handling functions
function handleFileSelect(event) {
    const file = event.target.files[0];
    if (file) {
        handleFile(file);
    }
}

function handleDragOver(event) {
    event.preventDefault();
    event.stopPropagation();
    event.currentTarget.classList.add('dragover');
}

function handleDragLeave(event) {
    event.preventDefault();
    event.stopPropagation();
    event.currentTarget.classList.remove('dragover');
}

function handleFileDrop(event) {
    event.preventDefault();
    event.stopPropagation();
    event.currentTarget.classList.remove('dragover');

    const files = event.dataTransfer.files;
    if (files.length > 0) {
        handleFile(files[0]);
    }
}

function handleFile(file) {
    // Validate file type
    if (!isValidSubtitleFile(file)) {
        showNotification('Please select a valid subtitle file (.srt, .vtt, .ass)', 'error');
        return;
    }

    // Check file size (max 10MB)
    if (file.size > 10 * 1024 * 1024) {
        showNotification('File size too large. Maximum size is 10MB.', 'error');
        return;
    }

    currentSrtFile = file;
    loadSubtitleFile(file);
}

function isValidSubtitleFile(file) {
    const validExtensions = ['.srt', '.vtt', '.ass', '.ssa'];
    const fileName = file.name.toLowerCase();
    return validExtensions.some(ext => fileName.endsWith(ext));
}

function loadSubtitleFile(file) {
    const reader = new FileReader();
    reader.onload = function (e) {
        originalSubtitleContent = e.target.result;

        // Validate the content with the server
        validateWithServer(file, originalSubtitleContent);
    };
    reader.onerror = function () {
        showNotification('Error reading file. Please try again.', 'error');
    };
    reader.readAsText(file);
}

function validateWithServer(file, content) {
    const formData = new FormData();
    formData.append('srtFile', file);

    fetch('/SrtEditor/ValidateSrtFile', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                document.getElementById('originalSubtitleText').value = content;
                updateFileUploadUI(file.name);
                updateButtonStates();
                updateStats();
                updateFileStatus('Ready');
                showNotification(`Successfully loaded: ${file.name} (${data.entryCount} entries)`, 'success');
            } else {
                showNotification(data.message || 'Invalid subtitle file format', 'error');
                clearFile();
            }
        })
        .catch(error => {
            console.error('Validation error:', error);
            // Still allow the file if validation fails due to network issues
            document.getElementById('originalSubtitleText').value = content;
            updateFileUploadUI(file.name);
            updateButtonStates();
            updateStats();
            updateFileStatus('Loaded');
            showNotification(`File loaded: ${file.name} (validation skipped)`, 'warning');
        });
}

function updateFileUploadUI(fileName) {
    const uploadArea = document.getElementById('fileUploadArea');
    uploadArea.innerHTML = `
        <div class="upload-icon"><i class="fas fa-check-circle" style="color: #10b981;"></i></div>
        <p class="upload-text">File loaded: <strong>${fileName}</strong></p>
        <button type="button" class="btn btn-outline-secondary btn-sm" onclick="clearFile()">
            <i class="fas fa-times me-1"></i> Remove File
        </button>
    `;
}

function clearFile() {
    currentSrtFile = null;
    originalSubtitleContent = '';
    document.getElementById('originalSubtitleText').value = '';

    // Reset upload area
    const uploadArea = document.getElementById('fileUploadArea');
    uploadArea.innerHTML = `
        <div class="upload-icon"><i class="fas fa-film"></i></div>
        <p class="upload-text">Drop subtitle file here or click to browse</p>
        <input type="file" id="srtFileInput" accept=".srt,.vtt,.ass,.ssa,.sub" style="display: none;" />
        <button type="button" class="btn btn-outline-primary btn-sm" onclick="document.getElementById('srtFileInput').click()">
            <i class="fas fa-upload me-1"></i> Choose File
        </button>
    `;

    // Reattach event listeners
    const srtFileInput = document.getElementById('srtFileInput');
    srtFileInput.addEventListener('change', handleFileSelect);

    updateButtonStates();
    showNotification('File cleared', 'info');
}

// Main editor functions
function startEditing() {
    if (!originalSubtitleContent) {
        showNotification('Please load a subtitle file first', 'warning');
        return;
    }

    isEditing = true;

    // Enable translated textarea for editing
    const translatedTextarea = document.getElementById('translatedSubtitleText');
    translatedTextarea.readOnly = false;
    translatedTextarea.value = translatedSubtitleContent || originalSubtitleContent;
    translatedTextarea.focus();

    updateButtonStates();
    showNotification('Editing mode enabled. You can now modify the translated text.', 'info');
}

function clearAll() {
    if (confirm('Are you sure you want to clear all content? This action cannot be undone.')) {
        // Clear all content
        currentSrtFile = null;
        originalSubtitleContent = '';
        translatedSubtitleContent = '';
        isEditing = false;

        document.getElementById('originalSubtitleText').value = '';
        document.getElementById('translatedSubtitleText').value = '';

        // Reset upload area
        clearFile();

        // Reset download section
        resetDownloadSection();

        // Reset stats
        document.getElementById('entryCount').textContent = '0';
        document.getElementById('totalDuration').textContent = '00:00';
        document.getElementById('charCount').textContent = '0';
        updateFileStatus('Ready');

        updateButtonStates();
        showNotification('All content cleared', 'info');
    }
}

function translateSubtitle() {
    if (!originalSubtitleContent) {
        showNotification('Please load a subtitle file first', 'warning');
        return;
    }

    // Show language selection modal
    showLanguageSelectionModal();
}

function showLanguageSelectionModal() {
    // Create modal for language selection
    const modal = document.createElement('div');
    modal.className = 'modal fade';
    modal.id = 'languageModal';
    modal.innerHTML = `
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content" style="background: linear-gradient(145deg, rgba(26, 26, 46, 0.95), rgba(22, 33, 62, 0.95)); border: 1px solid rgba(255, 255, 255, 0.1); backdrop-filter: blur(15px);">
                <div class="modal-header" style="border-bottom: 1px solid rgba(255, 255, 255, 0.1);">
                    <h5 class="modal-title text-white"><i class="fas fa-language me-2"></i>Select Translation Languages</h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <div class="mb-3">
                        <label class="form-label text-white">Source Language:</label>
                        <select id="modalSourceLanguage" class="form-select" style="background: rgba(0,0,0,0.3); color: white; border: 1px solid rgba(255,255,255,0.2);">
                            <option value="auto">Auto Detect</option>
                            <option value="en">English</option>
                            <option value="vi">Vietnamese</option>
                            <option value="zh">Chinese (Simplified)</option>
                            <option value="ja">Japanese</option>
                            <option value="ko">Korean</option>
                            <option value="fr">French</option>
                            <option value="es">Spanish</option>
                            <option value="de">German</option>
                            <option value="it">Italian</option>
                            <option value="pt">Portuguese</option>
                            <option value="ru">Russian</option>
                            <option value="ar">Arabic</option>
                            <option value="hi">Hindi</option>
                            <option value="th">Thai</option>
                            <option value="ms">Malay</option>
                            <option value="id">Indonesian</option>
                        </select>
                    </div>
                    <div class="mb-3">
                        <label class="form-label text-white">Target Language:</label>
                        <select id="modalTargetLanguage" class="form-select" style="background: rgba(0,0,0,0.3); color: white; border: 1px solid rgba(255,255,255,0.2);">
                            <option value="vi">Vietnamese</option>
                            <option value="en">English</option>
                            <option value="zh">Chinese (Simplified)</option>
                            <option value="ja">Japanese</option>
                            <option value="ko">Korean</option>
                            <option value="fr">French</option>
                            <option value="es">Spanish</option>
                            <option value="de">German</option>
                            <option value="it">Italian</option>
                            <option value="pt">Portuguese</option>
                            <option value="ru">Russian</option>
                            <option value="ar">Arabic</option>
                            <option value="hi">Hindi</option>
                            <option value="th">Thai</option>
                            <option value="ms">Malay</option>
                            <option value="id">Indonesian</option>
                        </select>
                    </div>
                </div>
                <div class="modal-footer" style="border-top: 1px solid rgba(255, 255, 255, 0.1);">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-primary" onclick="startTranslation()">
                        <i class="fas fa-language me-2"></i>Start Translation
                    </button>
                </div>
            </div>
        </div>
    `;

    document.body.appendChild(modal);
    const bootstrapModal = new bootstrap.Modal(modal);
    bootstrapModal.show();

    // Remove modal from DOM when hidden
    modal.addEventListener('hidden.bs.modal', function () {
        document.body.removeChild(modal);
    });
}

function startTranslation() {
    const sourceLanguage = document.getElementById('modalSourceLanguage').value;
    const targetLanguage = document.getElementById('modalTargetLanguage').value;

    if (sourceLanguage === targetLanguage) {
        showNotification('Source and target languages cannot be the same', 'warning');
        return;
    }

    // Close modal
    const modal = bootstrap.Modal.getInstance(document.getElementById('languageModal'));
    modal.hide();

    // Show loading
    showLoading('Translating subtitle...', 'Please wait while we translate your subtitle file.');

    // Prepare form data
    const formData = new FormData();
    formData.append('subtitleContent', originalSubtitleContent);
    formData.append('sourceLanguage', sourceLanguage);
    formData.append('targetLanguage', targetLanguage);

    // Call translation API using new SrtEditor controller
    fetch('/SrtEditor/TranslateSubtitle', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            hideLoading();

            if (data.success) {
                translatedSubtitleContent = data.translatedContent;
                document.getElementById('translatedSubtitleText').value = translatedSubtitleContent;

                updateButtonStates();
                updateStats();
                showNotification('Translation completed successfully!', 'success');
            } else {
                showNotification(data.message || 'Translation failed. Please try again.', 'error');
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Translation error:', error);
            showNotification('Translation failed. Please check your connection and try again.', 'error');
        });
}

function previewSubtitle() {
    const content = document.getElementById('translatedSubtitleText').value || originalSubtitleContent;

    if (!content.trim()) {
        showNotification('No content to preview', 'warning');
        return;
    }

    // Create preview modal
    const modal = document.createElement('div');
    modal.className = 'modal fade';
    modal.id = 'previewModal';
    modal.innerHTML = `
        <div class="modal-dialog modal-lg">
            <div class="modal-content" style="background: linear-gradient(145deg, rgba(26, 26, 46, 0.95), rgba(22, 33, 62, 0.95)); border: 1px solid rgba(255, 255, 255, 0.1); backdrop-filter: blur(15px);">
                <div class="modal-header" style="border-bottom: 1px solid rgba(255, 255, 255, 0.1);">
                    <h5 class="modal-title text-white"><i class="fas fa-eye me-2"></i>Subtitle Preview</h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <div class="preview-area" style="background: rgba(0,0,0,0.3); border-radius: 10px; padding: 20px; max-height: 400px; overflow-y: auto;">
                        <pre style="color: #4ecdc4; font-family: 'JetBrains Mono', monospace; white-space: pre-wrap; margin: 0;">${content}</pre>
                    </div>
                </div>
                <div class="modal-footer" style="border-top: 1px solid rgba(255, 255, 255, 0.1);">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    `;

    document.body.appendChild(modal);
    const bootstrapModal = new bootstrap.Modal(modal);
    bootstrapModal.show();

    // Remove modal from DOM when hidden
    modal.addEventListener('hidden.bs.modal', function () {
        document.body.removeChild(modal);
    });
}

function validateContent() {
    const content = document.getElementById('translatedSubtitleText').value || originalSubtitleContent;

    if (!content.trim()) {
        showNotification('No content to validate', 'warning');
        return;
    }

    // Basic SRT validation
    const lines = content.split('\n');
    let issues = [];
    let entryCount = 0;

    // Simple validation logic
    for (let i = 0; i < lines.length; i++) {
        const line = lines[i].trim();

        if (/^\d+$/.test(line)) {
            entryCount++;
        } else if (line.includes('-->')) {
            // Check timestamp format
            if (!/\d{2}:\d{2}:\d{2},\d{3} --> \d{2}:\d{2}:\d{2},\d{3}/.test(line)) {
                issues.push(`Line ${i + 1}: Invalid timestamp format`);
            }
        }
    }

    if (issues.length === 0) {
        showNotification(`Validation passed! Found ${entryCount} subtitle entries.`, 'success');
        updateFileStatus('Valid');
    } else {
        showNotification(`Validation found ${issues.length} issues. Check console for details.`, 'warning');
        console.log('Validation issues:', issues);
        updateFileStatus('Issues');
    }
}

function updateStats() {
    if (!originalSubtitleContent && !translatedSubtitleContent) {
        return;
    }

    const content = translatedSubtitleContent || originalSubtitleContent;
    const lines = content.split('\n');

    // Count entries
    let entryCount = 0;
    let totalChars = 0;
    let timestamps = [];

    for (const line of lines) {
        const trimmed = line.trim();
        if (/^\d+$/.test(trimmed)) {
            entryCount++;
        } else if (trimmed.includes('-->')) {
            timestamps.push(trimmed);
        } else if (trimmed && !/^\d+$/.test(trimmed) && !trimmed.includes('-->')) {
            totalChars += trimmed.length;
        }
    }

    // Calculate duration
    let duration = '00:00';
    if (timestamps.length > 0) {
        const lastTimestamp = timestamps[timestamps.length - 1];
        const match = lastTimestamp.match(/(\d{2}:\d{2}:\d{2})/g);
        if (match && match.length > 1) {
            duration = match[1];
        }
    }

    // Update UI
    document.getElementById('entryCount').textContent = entryCount;
    document.getElementById('totalDuration').textContent = duration;
    document.getElementById('charCount').textContent = totalChars.toLocaleString();
}

function updateFileStatus(status) {
    const statusElement = document.getElementById('fileStatus');
    statusElement.textContent = status;

    // Update status color
    statusElement.className = 'stat-value';
    switch (status.toLowerCase()) {
        case 'ready':
        case 'valid':
            statusElement.classList.add('status-ready');
            break;
        case 'processing':
            statusElement.style.color = '#f39c12';
            break;
        case 'issues':
            statusElement.style.color = '#e74c3c';
            break;
        default:
            statusElement.style.color = '#4ecdc4';
    }
}

function saveEdits() {
    const translatedTextarea = document.getElementById('translatedSubtitleText');
    translatedSubtitleContent = translatedTextarea.value;

    if (translatedSubtitleContent.trim() === '') {
        showNotification('Cannot save empty content', 'warning');
        return;
    }

    // Update download section
    updateDownloadSection();
    updateButtonStates();

    showNotification('Edits saved successfully!', 'success');
}

function autoFormat() {
    const translatedTextarea = document.getElementById('translatedSubtitleText');
    let content = translatedTextarea.value;

    if (!content.trim()) {
        showNotification('No content to format', 'warning');
        return;
    }

    // Call server-side formatting for better accuracy
    const formData = new FormData();
    formData.append('content', content);

    fetch('/SrtEditor/FormatSrtContent', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                translatedTextarea.value = data.formattedContent;
                showNotification('Content formatted successfully!', 'success');
            } else {
                // Fallback to client-side formatting
                content = formatSrtContentClientSide(content);
                translatedTextarea.value = content;
                showNotification('Content formatted (basic formatting applied)', 'info');
            }
        })
        .catch(error => {
            console.error('Format error:', error);
            // Fallback to client-side formatting
            content = formatSrtContentClientSide(content);
            translatedTextarea.value = content;
            showNotification('Content formatted (offline mode)', 'info');
        });
}

function formatSrtContentClientSide(content) {
    // Basic SRT formatting rules (fallback)
    const lines = content.split('\n');
    let formatted = [];
    let subtitleNumber = 1;

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i].trim();

        if (line === '') continue;

        // Check if it's a timestamp line
        if (line.includes('-->')) {
            formatted.push(subtitleNumber.toString());
            formatted.push(line);
            subtitleNumber++;
        } else if (!isNaN(parseInt(line))) {
            // Skip existing subtitle numbers
            continue;
        } else {
            formatted.push(line);
            formatted.push(''); // Add empty line after text
        }
    }

    return formatted.join('\n');
}

function downloadEditedSubtitle() {
    if (!translatedSubtitleContent) {
        showNotification('No content to download', 'warning');
        return;
    }

    const fileName = currentSrtFile ?
        currentSrtFile.name.replace(/\.[^/.]+$/, '_edited.srt') :
        'edited_subtitle.srt';

    const blob = new Blob([translatedSubtitleContent], { type: 'text/plain' });
    const url = window.URL.createObjectURL(blob);

    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();

    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);

    showNotification(`Successfully exported: ${fileName}`, 'success');
}

// UI Helper functions
function updateButtonStates() {
    const hasFile = !!originalSubtitleContent;
    const hasTranslation = !!translatedSubtitleContent;

    document.getElementById('startEditBtn').disabled = !hasFile;
    document.getElementById('clearAllBtn').disabled = !hasFile && !hasTranslation;
    document.getElementById('saveEditsBtn').disabled = !isEditing && !hasTranslation;
    document.getElementById('autoFormatBtn').disabled = !hasTranslation;
    document.getElementById('downloadBtn').disabled = !hasTranslation;

    // Update action buttons if they exist
    const translateBtn = document.querySelector('[onclick="translateSubtitle()"]');
    const previewBtn = document.querySelector('[onclick="previewSubtitle()"]');
    const validateBtn = document.querySelector('[onclick="validateContent()"]');

    if (translateBtn) translateBtn.disabled = !hasFile;
    if (previewBtn) previewBtn.disabled = !hasFile && !hasTranslation;
    if (validateBtn) validateBtn.disabled = !hasFile && !hasTranslation;
}

function updateDownloadSection() {
    const downloadStatus = document.getElementById('downloadStatus');
    const fileName = currentSrtFile ?
        currentSrtFile.name.replace(/\.[^/.]+$/, '_edited.srt') :
        'edited_subtitle.srt';

    downloadStatus.innerHTML = `
        <p class="text-success mb-0">
            <i class="fas fa-check-circle me-2"></i>
            File ready: <strong>${fileName}</strong>
        </p>
    `;
    downloadStatus.classList.add('ready');
}

function resetDownloadSection() {
    const downloadStatus = document.getElementById('downloadStatus');
    downloadStatus.innerHTML = `<p class="text-muted">Your edited subtitle file will be ready for download here</p>`;
    downloadStatus.classList.remove('ready');
}

function showLoading(title, message) {
    const modal = document.getElementById('loadingModal');
    modal.querySelector('h5').textContent = title;
    modal.querySelector('p').textContent = message;

    const bootstrapModal = new bootstrap.Modal(modal);
    bootstrapModal.show();
}

function hideLoading() {
    const modal = document.getElementById('loadingModal');
    const bootstrapModal = bootstrap.Modal.getInstance(modal);
    if (bootstrapModal) {
        bootstrapModal.hide();
    }
}

function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `alert alert-${getBootstrapAlertClass(type)} alert-dismissible fade show position-fixed`;
    notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; max-width: 400px;';

    notification.innerHTML = `
        <i class="fas fa-${getNotificationIcon(type)} me-2"></i>
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    document.body.appendChild(notification);

    // Auto remove after 5 seconds
    setTimeout(() => {
        if (notification.parentNode) {
            notification.remove();
        }
    }, 5000);
}

function getBootstrapAlertClass(type) {
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