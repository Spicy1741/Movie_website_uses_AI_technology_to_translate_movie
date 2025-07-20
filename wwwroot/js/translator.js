// Translator JavaScript - Wireframe Design with Whisper AI Integration

// Global variables
let uploadedMovieFile = null;
let uploadedSubtitleFile = null;
let originalSubtitleText = '';
let translatedSubtitleText = '';
let isTranslating = false;

// Language names mapping
const languageNames = {
    'en': 'English',
    'es': 'Spanish',
    'fr': 'French',
    'de': 'German',
    'it': 'Italian',
    'pt': 'Portuguese',
    'zh': 'Chinese',
    'ja': 'Japanese',
    'ko': 'Korean',
    'vi': 'Vietnamese'
};

// DOM elements
const movieUploadArea = document.getElementById('movieUploadArea');
const movieFileInput = document.getElementById('movieFileInput');
const originalSubtitleUpload = document.getElementById('originalSubtitleUpload');
const originalSubtitleInput = document.getElementById('originalSubtitleInput');
const originalText = document.getElementById('originalText');
const translatedText = document.getElementById('translatedText');
const sourceLanguageSelect = document.getElementById('sourceLanguage');
const targetLanguageSelect = document.getElementById('targetLanguage');
const translateBtn = document.getElementById('translateBtn');
const clearAllBtn = document.getElementById('clearAllBtn');
const swapLanguagesBtn = document.getElementById('swapLanguages');
const saveEditsBtn = document.getElementById('saveEditsBtn');
const autoFormatBtn = document.getElementById('autoFormatBtn');
const downloadBtn = document.getElementById('downloadBtn');
const loadingModal = document.getElementById('loadingModal');
const loadingText = document.getElementById('loadingText');
const notificationToast = document.getElementById('notificationToast');
const toastIcon = document.getElementById('toastIcon');
const toastMessage = document.getElementById('toastMessage');
const translationPlaceholder = document.getElementById('translationPlaceholder');
const downloadPlaceholder = document.getElementById('downloadPlaceholder');
const downloadInfo = document.getElementById('downloadInfo');
const downloadFileName = document.getElementById('downloadFileName');
const downloadFileSize = document.getElementById('downloadFileSize');
const originalLanguageTag = document.getElementById('originalLanguageTag');
const translatedLanguageTag = document.getElementById('translatedLanguageTag');

// Initialize
document.addEventListener('DOMContentLoaded', function () {
    initializeEventListeners();
    updateLanguageTags();
});

// Event listeners
function initializeEventListeners() {
    // Movie file upload
    movieUploadArea.addEventListener('click', () => movieFileInput.click());
    movieUploadArea.addEventListener('dragover', handleDragOver);
    movieUploadArea.addEventListener('drop', handleMovieFileDrop);
    movieFileInput.addEventListener('change', handleMovieFileSelect);

    // Subtitle file upload
    originalSubtitleUpload.addEventListener('click', () => originalSubtitleInput.click());
    originalSubtitleUpload.addEventListener('dragover', handleDragOver);
    originalSubtitleUpload.addEventListener('drop', handleSubtitleFileDrop);
    originalSubtitleInput.addEventListener('change', handleSubtitleFileSelect);

    // Control buttons
    translateBtn.addEventListener('click', startTranslation);
    clearAllBtn.addEventListener('click', clearAll);
    swapLanguagesBtn.addEventListener('click', swapLanguages);
    saveEditsBtn.addEventListener('click', saveEdits);
    autoFormatBtn.addEventListener('click', autoFormat);
    downloadBtn.addEventListener('click', downloadEditedFile);

    // Language selects
    sourceLanguageSelect.addEventListener('change', updateLanguageTags);
    targetLanguageSelect.addEventListener('change', updateLanguageTags);

    // Text area changes
    translatedText.addEventListener('input', handleTranslatedTextChange);
}

// Drag and drop handlers
function handleDragOver(e) {
    e.preventDefault();
    e.currentTarget.classList.add('dragover');
}

function handleMovieFileDrop(e) {
    e.preventDefault();
    e.currentTarget.classList.remove('dragover');
    const files = e.dataTransfer.files;
    if (files.length > 0) {
        handleMovieFile(files[0]);
    }
}

function handleSubtitleFileDrop(e) {
    e.preventDefault();
    e.currentTarget.classList.remove('dragover');
    const files = e.dataTransfer.files;
    if (files.length > 0) {
        handleSubtitleFile(files[0]);
    }
}

function handleMovieFileSelect(e) {
    const file = e.target.files[0];
    if (file) {
        handleMovieFile(file);
    }
}

function handleSubtitleFileSelect(e) {
    const file = e.target.files[0];
    if (file) {
        handleSubtitleFile(file);
    }
}

// File handlers
function handleMovieFile(file) {
    if (!isValidVideoFile(file)) {
        showNotification('Please select a valid video file', 'error');
        return;
    }

    if (file.size > 524288000) { // 500MB limit
        showNotification('File size too large. Maximum size is 500MB.', 'error');
        return;
    }

    uploadedMovieFile = file;
    updateMovieUploadDisplay();
    showNotification('Video file loaded successfully', 'success');
}

function handleSubtitleFile(file) {
    if (!isValidSubtitleFile(file)) {
        showNotification('Please select a valid subtitle file (.srt or .vtt)', 'error');
        return;
    }

    uploadSubtitleFile(file);
}

async function uploadSubtitleFile(file) {
    showLoading('Uploading subtitle file...');

    try {
        const formData = new FormData();
        formData.append('srtFile', file);

        const response = await fetch('/api/Translator/upload-srt', {
            method: 'POST',
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            uploadedSubtitleFile = file;
            originalSubtitleText = result.content;
            originalText.value = result.content;
            updateSubtitleUploadDisplay();
            showNotification('Subtitle file loaded successfully', 'success');
        } else {
            showNotification(result.message || 'Failed to upload subtitle file', 'error');
        }
    } catch (error) {
        console.error('Error uploading subtitle file:', error);
        showNotification('Error uploading subtitle file', 'error');
    } finally {
        hideLoading();
    }
}

// Validation functions
function isValidVideoFile(file) {
    const allowedTypes = ['video/mp4', 'video/avi', 'video/quicktime', 'video/x-msvideo'];
    const allowedExtensions = ['.mp4', '.avi', '.mov', '.mkv', '.wmv', '.flv'];
    const fileExtension = '.' + file.name.split('.').pop().toLowerCase();

    return allowedTypes.includes(file.type) || allowedExtensions.includes(fileExtension);
}

function isValidSubtitleFile(file) {
    const allowedExtensions = ['.srt', '.vtt'];
    const fileExtension = '.' + file.name.split('.').pop().toLowerCase();

    return allowedExtensions.includes(fileExtension);
}

// Display updates
function updateMovieUploadDisplay() {
    const uploadContent = movieUploadArea.querySelector('.upload-placeholder');
    uploadContent.innerHTML = `
        <i class="fas fa-check-circle" style="color: #28a745;"></i>
        <p style="color: #28a745; font-weight: 600;">Movie File Uploaded</p>
        <p style="font-size: 0.9rem; color: #6c757d;">${uploadedMovieFile.name}</p>
        <p style="font-size: 0.8rem; color: #6c757d;">${formatFileSize(uploadedMovieFile.size)}</p>
    `;
    movieUploadArea.classList.add('file-uploaded');
}

function updateSubtitleUploadDisplay() {
    const uploadContent = originalSubtitleUpload.querySelector('.upload-placeholder');
    uploadContent.innerHTML = `
        <i class="fas fa-check-circle" style="color: #28a745;"></i>
        <p style="color: #28a745; font-weight: 600;">Subtitle Uploaded</p>
        <p style="font-size: 0.9rem; color: #6c757d;">${uploadedSubtitleFile.name}</p>
    `;
    originalSubtitleUpload.classList.add('file-uploaded');
}

// Translation process
async function startTranslation() {
    // Check if we have either a movie file or subtitle text
    if (!uploadedMovieFile && !originalSubtitleText.trim()) {
        showNotification('Please upload a video file or subtitle file first', 'warning');
        return;
    }

    if (sourceLanguageSelect.value === targetLanguageSelect.value) {
        showNotification('Source and target languages cannot be the same', 'warning');
        return;
    }

    if (isTranslating) {
        showNotification('Translation is already in progress', 'warning');
        return;
    }

    isTranslating = true;
    showLoading('Processing your file with AI...');

    try {
        let result;

        if (uploadedMovieFile) {
            // Transcribe video file using Whisper AI
            result = await transcribeVideoFile();
        } else if (originalSubtitleText.trim()) {
            // Translate existing subtitle text
            result = await translateSubtitleText();
        }

        if (result && result.success) {
            // Display original text if transcribed
            if (result.originalText && !originalSubtitleText) {
                originalSubtitleText = result.originalText;
                originalText.value = result.originalText;
            }

            // Display translated text
            if (result.translatedText || result.originalText) {
                translatedSubtitleText = result.translatedText || result.originalText;
                showTranslatedText();
                enableDownload();
                showNotification('Translation completed successfully!', 'success');
            }
        } else {
            showNotification(result?.message || 'Translation failed', 'error');
        }
    } catch (error) {
        console.error('Translation error:', error);
        showNotification('Translation failed. Please try again.', 'error');
    } finally {
        isTranslating = false;
        hideLoading();
    }
}

async function transcribeVideoFile() {
    const formData = new FormData();
    formData.append('VideoFile', uploadedMovieFile);
    formData.append('SourceLanguage', sourceLanguageSelect.value);
    formData.append('TargetLanguage', targetLanguageSelect.value);
    formData.append('EnableTranslation', 'true');

    const response = await fetch('/api/Translator/transcribe', {
        method: 'POST',
        body: formData
    });

    if (!response.ok) {
        throw new Error('Network response was not ok');
    }

    return await response.json();
}

async function translateSubtitleText() {
    // Mock translation for subtitle text - replace with actual translation API
    const translatedText = mockTranslate(originalSubtitleText, sourceLanguageSelect.value, targetLanguageSelect.value);

    return {
        success: true,
        originalText: originalSubtitleText,
        translatedText: translatedText
    };
}

function mockTranslate(text, sourceLanguage, targetLanguage) {
    const lines = text.split('\n');
    const translatedLines = [];

    for (const line of lines) {
        if (!line.trim()) {
            translatedLines.push(line);
            continue;
        }

        // Keep timestamps and sequence numbers as is
        if (line.includes('-->') || line.match(/^\d+$/)) {
            translatedLines.push(line);
            continue;
        }

        // Mock translation based on target language
        const translatedLine = `[${targetLanguage.toUpperCase()}] ${line}`;
        translatedLines.push(translatedLine);
    }

    return translatedLines.join('\n');
}

// UI control functions
function showTranslatedText() {
    translationPlaceholder.style.display = 'none';
    translatedText.style.display = 'block';
    translatedText.value = translatedSubtitleText;
}

function updateLanguageTags() {
    const sourceLang = sourceLanguageSelect.value;
    const targetLang = targetLanguageSelect.value;

    originalLanguageTag.textContent = languageNames[sourceLang] || '1st Language';
    translatedLanguageTag.textContent = languageNames[targetLang] || '2nd Language';
}

function swapLanguages() {
    const sourceValue = sourceLanguageSelect.value;
    const targetValue = targetLanguageSelect.value;

    sourceLanguageSelect.value = targetValue;
    targetLanguageSelect.value = sourceValue;

    // Swap text content if both exist
    if (originalSubtitleText && translatedSubtitleText) {
        const tempText = originalText.value;
        originalText.value = translatedText.value;
        translatedText.value = tempText;

        originalSubtitleText = originalText.value;
        translatedSubtitleText = translatedText.value;
    }

    updateLanguageTags();
    showNotification('Languages swapped successfully', 'info');
}

function handleTranslatedTextChange() {
    translatedSubtitleText = translatedText.value;
}

async function saveEdits() {
    if (!translatedSubtitleText.trim()) {
        showNotification('No translated text to save', 'warning');
        return;
    }

    try {
        const response = await fetch('/api/Translator/save-edited', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                content: translatedSubtitleText,
                fileName: 'edited_subtitle.srt'
            })
        });

        const result = await response.json();

        if (result.success) {
            showNotification('Edits saved successfully', 'success');
            updateDownloadInfo(result.fileName, translatedSubtitleText.length);
        } else {
            showNotification(result.message || 'Failed to save edits', 'error');
        }
    } catch (error) {
        console.error('Error saving edits:', error);
        showNotification('Error saving edits', 'error');
    }
}

function autoFormat() {
    if (!translatedSubtitleText.trim()) {
        showNotification('No text to format', 'warning');
        return;
    }

    // Basic SRT formatting
    let formattedText = translatedSubtitleText
        .replace(/\r\n/g, '\n')
        .replace(/\n{3,}/g, '\n\n')
        .trim();

    translatedText.value = formattedText;
    translatedSubtitleText = formattedText;
    showNotification('Text formatted successfully', 'success');
}

function enableDownload() {
    downloadPlaceholder.style.display = 'none';
    downloadInfo.style.display = 'flex';
    downloadBtn.style.display = 'flex';

    const fileName = 'translated_subtitle.srt';
    const fileSize = new Blob([translatedSubtitleText]).size;

    updateDownloadInfo(fileName, fileSize);
}

function updateDownloadInfo(fileName, size) {
    downloadFileName.textContent = fileName;
    downloadFileSize.textContent = formatFileSize(size);
}

function downloadEditedFile() {
    if (!translatedSubtitleText.trim()) {
        showNotification('No translated text to download', 'error');
        return;
    }

    try {
        const blob = new Blob([translatedSubtitleText], { type: 'text/plain' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `translated_subtitle_${Date.now()}.srt`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);

        showNotification('File downloaded successfully', 'success');
    } catch (error) {
        console.error('Download error:', error);
        showNotification('Download failed', 'error');
    }
}

function clearAll() {
    if (confirm('Are you sure you want to clear all data? This action cannot be undone.')) {
        // Reset file uploads
        uploadedMovieFile = null;
        uploadedSubtitleFile = null;
        movieFileInput.value = '';
        originalSubtitleInput.value = '';

        // Reset text areas
        originalSubtitleText = '';
        translatedSubtitleText = '';
        originalText.value = '';
        translatedText.value = '';

        // Reset UI
        resetMovieUploadDisplay();
        resetSubtitleUploadDisplay();
        hideTranslatedText();
        hideDownload();

        showNotification('All data cleared', 'info');
    }
}

function resetMovieUploadDisplay() {
    const uploadContent = movieUploadArea.querySelector('.upload-placeholder');
    uploadContent.innerHTML = `
        <i class="fas fa-cloud-upload-alt"></i>
        <p>Input or drag movie file</p>
    `;
    movieUploadArea.classList.remove('file-uploaded');
}

function resetSubtitleUploadDisplay() {
    const uploadContent = originalSubtitleUpload.querySelector('.upload-placeholder');
    uploadContent.innerHTML = `
        <i class="fas fa-file-alt"></i>
        <p>input or drag srt file</p>
    `;
    originalSubtitleUpload.classList.remove('file-uploaded');
}

function hideTranslatedText() {
    translationPlaceholder.style.display = 'flex';
    translatedText.style.display = 'none';
}

function hideDownload() {
    downloadPlaceholder.style.display = 'block';
    downloadInfo.style.display = 'none';
    downloadBtn.style.display = 'none';
}

// Utility functions
function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function showLoading(message = 'Processing...') {
    loadingText.textContent = message;
    loadingModal.style.display = 'flex';
}

function hideLoading() {
    loadingModal.style.display = 'none';
}

function showNotification(message, type = 'info') {
    toastMessage.textContent = message;

    // Remove existing type classes
    notificationToast.className = 'notification-toast';
    notificationToast.classList.add(type);

    // Set icon based on type
    const icons = {
        success: 'fas fa-check-circle',
        error: 'fas fa-exclamation-circle',
        warning: 'fas fa-exclamation-triangle',
        info: 'fas fa-info-circle'
    };

    toastIcon.className = icons[type] || icons.info;

    // Show toast
    notificationToast.style.display = 'block';

    // Hide after 3 seconds
    setTimeout(() => {
        notificationToast.style.display = 'none';
    }, 3000);
}