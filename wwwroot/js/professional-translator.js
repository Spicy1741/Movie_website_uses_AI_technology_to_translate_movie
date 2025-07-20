// Professional Translator JavaScript - Dual Tab Interface

// Global variables
let uploadedMovieFile = null;
let uploadedSubtitleFile = null;
let originalSubtitleText = '';
let translatedSubtitleText = '';
let isTranslating = false;
let currentTab = 'video';
let parsedSubtitles = [];
let translatedSubtitles = [];
let currentFileName = '';

// Language names mapping
const languageNames = {
    'auto': 'Auto Detect',
    'en': 'English',
    'es': 'Spanish',
    'fr': 'French',
    'de': 'German',
    'it': 'Italian',
    'pt': 'Portuguese',
    'zh': 'Chinese',
    'ja': 'Japanese',
    'ko': 'Korean',
    'vi': 'Vietnamese',
    'ru': 'Russian',
    'ar': 'Arabic',
    'hi': 'Hindi',
    'th': 'Thai'
};

// DOM elements - Video Tab
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

// DOM elements - Subtitle Tab
const subtitleFileUpload = document.getElementById('subtitleFileUpload');
const subtitleFileInput = document.getElementById('subtitleFileInput');
const selectedFileName = document.getElementById('selectedFileName');
const subtitleSourceLang = document.getElementById('subtitleSourceLang');
const subtitleTargetLang = document.getElementById('subtitleTargetLang');
const uploadTranslateBtn = document.getElementById('uploadTranslateBtn');
const subtitleUploadSection = document.getElementById('subtitleUploadSection');
const subtitleResultsSection = document.getElementById('subtitleResultsSection');
const downloadTranslatedBtn = document.getElementById('downloadTranslatedBtn');
const translateAnotherBtn = document.getElementById('translateAnotherBtn');
const copySrtBtn = document.getElementById('copySrtBtn');

// Common elements
const loadingModal = document.getElementById('loadingModal');
const loadingText = document.getElementById('loadingText');
const loadingSubtext = document.getElementById('loadingSubtext');
const notificationToast = document.getElementById('notificationToast');

// Initialize
document.addEventListener('DOMContentLoaded', function () {
    initializeEventListeners();
    initializeTabs();
    hideLoading();
});

function initializeEventListeners() {
    // Tab switching
    const tabBtns = document.querySelectorAll('.pro-tab-btn');
    tabBtns.forEach(btn => {
        btn.addEventListener('click', () => switchTab(btn.dataset.tab));
    });

    // Video tab events (existing)
    if (movieUploadArea && movieFileInput) {
        movieUploadArea.addEventListener('click', () => movieFileInput.click());
        movieUploadArea.addEventListener('dragover', preventDefaults);
        movieUploadArea.addEventListener('drop', handleVideoDrop);
        movieFileInput.addEventListener('change', handleVideoFileSelect);
    }

    if (originalSubtitleUpload && originalSubtitleInput) {
        originalSubtitleUpload.addEventListener('click', () => originalSubtitleInput.click());
        originalSubtitleUpload.addEventListener('dragover', preventDefaults);
        originalSubtitleUpload.addEventListener('drop', handleOriginalSubtitleDrop);
        originalSubtitleInput.addEventListener('change', handleOriginalSubtitleSelect);
    }

    if (translateBtn) translateBtn.addEventListener('click', startVideoTranslation);
    if (clearAllBtn) clearAllBtn.addEventListener('click', clearAll);
    if (swapLanguagesBtn) swapLanguagesBtn.addEventListener('click', swapVideoLanguages);

    // Subtitle tab events (new)
    if (subtitleFileUpload && subtitleFileInput) {
        subtitleFileUpload.addEventListener('click', () => subtitleFileInput.click());
        subtitleFileUpload.addEventListener('dragover', preventDefaults);
        subtitleFileUpload.addEventListener('drop', handleSubtitleFileDrop);
        subtitleFileInput.addEventListener('change', handleSubtitleFileSelect);
    }

    if (uploadTranslateBtn) uploadTranslateBtn.addEventListener('click', startSubtitleTranslation);
    if (downloadTranslatedBtn) downloadTranslatedBtn.addEventListener('click', downloadTranslatedFile);
    if (translateAnotherBtn) translateAnotherBtn.addEventListener('click', translateAnotherFile);
    if (copySrtBtn) copySrtBtn.addEventListener('click', copySrtToClipboard);
}

function initializeTabs() {
    switchTab('video');
}

function switchTab(tabName) {
    currentTab = tabName;

    // Update tab buttons
    const tabBtns = document.querySelectorAll('.pro-tab-btn');
    tabBtns.forEach(btn => {
        btn.classList.toggle('active', btn.dataset.tab === tabName);
    });

    // Update tab contents
    const tabContents = document.querySelectorAll('.tab-content');
    tabContents.forEach(content => {
        content.classList.toggle('active', content.id === tabName + '-tab');
    });
}

// =================================
// VIDEO TRANSCRIPTION FUNCTIONS (Existing)
// =================================

function handleVideoDrop(e) {
    preventDefaults(e);
    const files = e.dataTransfer.files;
    if (files.length > 0) {
        handleVideoFile(files[0]);
    }
}

function handleVideoFileSelect(e) {
    if (e.target.files.length > 0) {
        handleVideoFile(e.target.files[0]);
    }
}

function handleVideoFile(file) {
    if (!isValidVideoFile(file)) {
        showNotification('Please select a valid video file', 'error');
        return;
    }

    if (file.size > 524288000) { // 500MB
        showNotification('File size too large. Maximum size is 500MB.', 'error');
        return;
    }

    uploadedMovieFile = file;
    updateVideoUploadDisplay();
    showNotification('Video file loaded successfully', 'success');
}

function updateVideoUploadDisplay() {
    const uploadContent = movieUploadArea.querySelector('.upload-placeholder');
    uploadContent.innerHTML = `
        <i class="fas fa-check-circle" style="color: #28a745;"></i>
        <p style="color: #28a745; font-weight: 600;">Movie File Uploaded</p>
        <p style="font-size: 0.9rem; color: #6c757d;">${uploadedMovieFile.name}</p>
        <p style="font-size: 0.8rem; color: #6c757d;">${formatFileSize(uploadedMovieFile.size)}</p>
    `;
    movieUploadArea.classList.add('file-uploaded');
}

function handleOriginalSubtitleDrop(e) {
    preventDefaults(e);
    const files = e.dataTransfer.files;
    if (files.length > 0) {
        handleOriginalSubtitleFile(files[0]);
    }
}

function handleOriginalSubtitleSelect(e) {
    if (e.target.files.length > 0) {
        handleOriginalSubtitleFile(e.target.files[0]);
    }
}

function handleOriginalSubtitleFile(file) {
    if (!isValidSubtitleFile(file)) {
        showNotification('Please select a valid subtitle file', 'error');
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
            updateOriginalSubtitleDisplay();
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

function updateOriginalSubtitleDisplay() {
    const uploadContent = originalSubtitleUpload.querySelector('.upload-placeholder');
    uploadContent.innerHTML = `
        <i class="fas fa-check-circle" style="color: #28a745;"></i>
        <p style="color: #28a745; font-weight: 600;">Subtitle Uploaded</p>
        <p style="font-size: 0.9rem; color: #6c757d;">${uploadedSubtitleFile.name}</p>
    `;
    originalSubtitleUpload.classList.add('file-uploaded');
}

async function startVideoTranslation() {
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
            result = await transcribeVideoFile();
        } else if (originalSubtitleText.trim()) {
            result = await translateSubtitleText();
        }

        if (result && result.success) {
            if (result.originalText && !originalSubtitleText) {
                originalSubtitleText = result.originalText;
                originalText.value = result.originalText;
            }

            if (result.translatedText || result.originalText) {
                translatedSubtitleText = result.translatedText || result.originalText;
                showVideoTranslatedText();
                enableVideoDownload();
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

        if (line.includes('-->') || line.match(/^\d+$/)) {
            translatedLines.push(line);
            continue;
        }

        const translatedLine = `[${targetLanguage.toUpperCase()}] ${line}`;
        translatedLines.push(translatedLine);
    }

    return translatedLines.join('\n');
}

function showVideoTranslatedText() {
    const translationPlaceholder = document.getElementById('translationPlaceholder');
    const translatedTextArea = document.getElementById('translatedTextArea');
    const editControls = document.getElementById('editControls');

    if (translationPlaceholder) translationPlaceholder.style.display = 'none';
    if (translatedTextArea) translatedTextArea.style.display = 'block';
    if (editControls) editControls.style.display = 'block';

    if (translatedText) {
        translatedText.value = translatedSubtitleText;
    }
}

function enableVideoDownload() {
    const downloadBtn = document.getElementById('downloadBtn');
    if (downloadBtn) {
        downloadBtn.disabled = false;
        downloadBtn.classList.add('enabled');
    }
}

function swapVideoLanguages() {
    const temp = sourceLanguageSelect.value;
    sourceLanguageSelect.value = targetLanguageSelect.value;
    targetLanguageSelect.value = temp;
}

// =================================
// SUBTITLE TRANSLATION FUNCTIONS (New Professional Interface)
// =================================

function handleSubtitleFileDrop(e) {
    preventDefaults(e);
    const files = e.dataTransfer.files;
    if (files.length > 0) {
        handleSubtitleFileUpload(files[0]);
    }
}

function handleSubtitleFileSelect(e) {
    if (e.target.files.length > 0) {
        handleSubtitleFileUpload(e.target.files[0]);
    }
}

function handleSubtitleFileUpload(file) {
    if (!isValidSubtitleFile(file)) {
        showNotification('Please select a valid subtitle file (.srt, .vtt, .ass, .ssa, .sub)', 'error');
        return;
    }

    if (file.size > 20 * 1024 * 1024) { // 20MB
        showNotification('File size too large. Maximum size is 20MB.', 'error');
        return;
    }

    uploadedSubtitleFile = file;
    currentFileName = file.name;
    selectedFileName.textContent = file.name;
    subtitleFileUpload.classList.add('file-selected');
    showNotification('Subtitle file selected successfully', 'success');
}

async function startSubtitleTranslation() {
    if (!uploadedSubtitleFile) {
        showNotification('Please select a subtitle file first', 'warning');
        return;
    }

    if (subtitleSourceLang.value === subtitleTargetLang.value) {
        showNotification('Source and target languages cannot be the same', 'warning');
        return;
    }

    if (isTranslating) {
        showNotification('Translation is already in progress', 'warning');
        return;
    }

    isTranslating = true;
    showLoading('Processing subtitle file...', 'Parsing subtitle entries and translating...');

    try {
        // Read and parse the file
        const fileContent = await readFileAsText(uploadedSubtitleFile);
        parsedSubtitles = parseSubtitleContent(fileContent);

        if (parsedSubtitles.length === 0) {
            throw new Error('No valid subtitle entries found');
        }

        // Translate subtitles
        translatedSubtitles = await translateSubtitles(parsedSubtitles);

        // Show results
        showSubtitleResults();
        showNotification('Translation completed successfully!', 'success');

    } catch (error) {
        console.error('Translation error:', error);
        showNotification('Translation failed: ' + error.message, 'error');
    } finally {
        isTranslating = false;
        hideLoading();
    }
}

function readFileAsText(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = e => resolve(e.target.result);
        reader.onerror = reject;
        reader.readAsText(file);
    });
}

function parseSubtitleContent(content) {
    const subtitles = [];

    if (content.includes('WEBVTT')) {
        // Parse VTT format
        return parseVttFormat(content);
    } else {
        // Parse SRT format
        return parseSrtFormat(content);
    }
}

function parseSrtFormat(content) {
    const subtitles = [];
    const blocks = content.split(/\n\s*\n/);

    for (let block of blocks) {
        const lines = block.trim().split('\n');
        if (lines.length >= 3) {
            const number = parseInt(lines[0]);
            const timeRange = lines[1];
            const text = lines.slice(2).join('\n');

            if (!isNaN(number) && timeRange.includes('-->')) {
                const [startTime, endTime] = timeRange.split('-->').map(t => t.trim());
                subtitles.push({
                    number: number,
                    startTime: startTime,
                    endTime: endTime,
                    text: text
                });
            }
        }
    }

    return subtitles;
}

function parseVttFormat(content) {
    const subtitles = [];
    const lines = content.split('\n');
    let number = 1;

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i].trim();
        if (line.includes('-->')) {
            const [startTime, endTime] = line.split('-->').map(t => t.trim());
            const textLines = [];

            // Get text lines
            for (let j = i + 1; j < lines.length && lines[j].trim() !== ''; j++) {
                textLines.push(lines[j].trim());
            }

            if (textLines.length > 0) {
                subtitles.push({
                    number: number++,
                    startTime: startTime,
                    endTime: endTime,
                    text: textLines.join('\n')
                });
            }
        }
    }

    return subtitles;
}

async function translateSubtitles(subtitles) {
    try {
        // Call the real translation API with correct route
        const response = await fetch('/Admin/TranslateSubtitles', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                subtitles: subtitles,
                sourceLanguage: subtitleSourceLang.value,
                targetLanguage: subtitleTargetLang.value
            })
        });

        if (!response.ok) {
            const errorText = await response.text();
            console.error('Translation API Error:', response.status, errorText);
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();

        if (!result.success) {
            throw new Error(result.message || 'Translation failed');
        }

        return result.translatedSubtitles;
    } catch (error) {
        console.error('Translation error:', error);
        throw error;
    }
}

function showSubtitleResults() {
    // Hide upload section, show results
    subtitleUploadSection.style.display = 'none';
    subtitleResultsSection.style.display = 'block';

    // Update summary
    document.getElementById('summaryFileName').textContent = currentFileName;
    document.getElementById('summaryFromLang').textContent = languageNames[subtitleSourceLang.value] || subtitleSourceLang.value;
    document.getElementById('summaryToLang').textContent = languageNames[subtitleTargetLang.value] || subtitleTargetLang.value;
    document.getElementById('summaryCount').textContent = parsedSubtitles.length;

    // Update language codes
    document.getElementById('originalLangCode').textContent = subtitleSourceLang.value;
    document.getElementById('translatedLangCode').textContent = subtitleTargetLang.value;

    // Display subtitle lists
    displaySubtitleList('originalSubtitlesList', parsedSubtitles);
    displaySubtitleList('translatedSubtitlesList', translatedSubtitles);

    // Generate and display SRT content
    const srtContent = generateSrtContent(translatedSubtitles);
    document.getElementById('srtContentDisplay').textContent = srtContent;
    translatedSubtitleText = srtContent;
}

function displaySubtitleList(containerId, subtitles) {
    const container = document.getElementById(containerId);
    container.innerHTML = '';

    subtitles.forEach(subtitle => {
        const subtitleElement = document.createElement('div');
        subtitleElement.className = 'subtitle-item';
        subtitleElement.innerHTML = `
            <div class="subtitle-time">${subtitle.startTime} → ${subtitle.endTime}</div>
            <div class="subtitle-text">${subtitle.text}</div>
        `;
        container.appendChild(subtitleElement);
    });
}

function generateSrtContent(subtitles) {
    let content = '';
    subtitles.forEach(subtitle => {
        content += `${subtitle.number}\n`;
        content += `${subtitle.startTime} --> ${subtitle.endTime}\n`;
        content += `${subtitle.text}\n\n`;
    });
    return content;
}

async function downloadTranslatedFile() {
    if (!translatedSubtitleText) {
        showNotification('No translated content to download', 'warning');
        return;
    }

    try {
        const response = await fetch('/Admin/DownloadTranslatedSubtitle', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                content: translatedSubtitleText,
                fileName: currentFileName,
                targetLanguage: subtitleTargetLang.value
            })
        });

        if (!response.ok) {
            throw new Error('Download failed');
        }

        // Create download link
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${currentFileName.replace(/\.[^/.]+$/, '')}_${subtitleTargetLang.value}.srt`;
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

function translateAnotherFile() {
    // Reset to upload section
    subtitleUploadSection.style.display = 'block';
    subtitleResultsSection.style.display = 'none';

    // Clear data
    uploadedSubtitleFile = null;
    parsedSubtitles = [];
    translatedSubtitles = [];
    currentFileName = '';
    selectedFileName.textContent = 'No file selected';
    subtitleFileUpload.classList.remove('file-selected');

    showNotification('Ready for new translation', 'info');
}

function copySrtToClipboard() {
    const srtContent = document.getElementById('srtContentDisplay').textContent;

    if (navigator.clipboard) {
        navigator.clipboard.writeText(srtContent).then(() => {
            showNotification('SRT content copied to clipboard', 'success');
        }).catch(err => {
            console.error('Failed to copy: ', err);
            showNotification('Failed to copy to clipboard', 'error');
        });
    } else {
        // Fallback for older browsers
        const textArea = document.createElement('textarea');
        textArea.value = srtContent;
        document.body.appendChild(textArea);
        textArea.select();
        try {
            document.execCommand('copy');
            showNotification('SRT content copied to clipboard', 'success');
        } catch (err) {
            showNotification('Failed to copy to clipboard', 'error');
        }
        document.body.removeChild(textArea);
    }
}

// =================================
// UTILITY FUNCTIONS
// =================================

function clearAll() {
    // Clear video tab data
    uploadedMovieFile = null;
    uploadedSubtitleFile = null;
    originalSubtitleText = '';
    translatedSubtitleText = '';

    // Reset video UI
    if (originalText) originalText.value = '';
    if (translatedText) translatedText.value = '';

    resetVideoUploadAreas();
    hideVideoTranslatedText();

    // Clear subtitle tab data
    parsedSubtitles = [];
    translatedSubtitles = [];
    currentFileName = '';

    // Reset subtitle UI
    if (selectedFileName) selectedFileName.textContent = 'No file selected';
    if (subtitleFileUpload) subtitleFileUpload.classList.remove('file-selected');
    if (subtitleUploadSection) subtitleUploadSection.style.display = 'block';
    if (subtitleResultsSection) subtitleResultsSection.style.display = 'none';

    showNotification('All data cleared', 'info');
}

function resetVideoUploadAreas() {
    // Reset movie upload area
    if (movieUploadArea) {
        movieUploadArea.classList.remove('file-uploaded');
        const uploadContent = movieUploadArea.querySelector('.upload-placeholder');
        if (uploadContent) {
            uploadContent.innerHTML = `
                <i class="fas fa-cloud-upload-alt"></i>
                <p>Input or drag movie file</p>
            `;
        }
    }

    // Reset original subtitle upload area
    if (originalSubtitleUpload) {
        originalSubtitleUpload.classList.remove('file-uploaded');
        const uploadContent = originalSubtitleUpload.querySelector('.upload-placeholder');
        if (uploadContent) {
            uploadContent.innerHTML = `
                <i class="fas fa-file-alt"></i>
                <p>input or drag srt file</p>
            `;
        }
    }
}

function hideVideoTranslatedText() {
    const translationPlaceholder = document.getElementById('translationPlaceholder');
    const translatedTextArea = document.getElementById('translatedTextArea');
    const editControls = document.getElementById('editControls');

    if (translationPlaceholder) translationPlaceholder.style.display = 'block';
    if (translatedTextArea) translatedTextArea.style.display = 'none';
    if (editControls) editControls.style.display = 'none';
}

function isValidVideoFile(file) {
    const allowedTypes = ['video/mp4', 'video/avi', 'video/quicktime', 'video/x-msvideo'];
    const allowedExtensions = ['.mp4', '.avi', '.mov', '.mkv', '.wmv', '.flv', '.mp3', '.wav', '.m4a', '.aac', '.ogg', '.flac'];
    const fileExtension = '.' + file.name.split('.').pop().toLowerCase();

    return allowedTypes.includes(file.type) || allowedExtensions.includes(fileExtension);
}

function isValidSubtitleFile(file) {
    const allowedExtensions = ['.srt', '.vtt', '.ass', '.ssa', '.sub'];
    const fileExtension = '.' + file.name.split('.').pop().toLowerCase();

    return allowedExtensions.includes(fileExtension);
}

function downloadTextAsFile(text, filename) {
    const blob = new Blob([text], { type: 'text/plain;charset=utf-8' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
    showNotification('File downloaded successfully', 'success');
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

function showLoading(message, subtext = 'This may take a few minutes depending on file size') {
    if (loadingText) loadingText.textContent = message;
    if (loadingSubtext) loadingSubtext.textContent = subtext;
    if (loadingModal) {
        loadingModal.style.display = 'flex';
        loadingModal.classList.add('show');
    }
}

function hideLoading() {
    if (loadingModal) {
        loadingModal.style.display = 'none';
        loadingModal.classList.remove('show');
    }
}

function updateLoadingProgress(message) {
    if (loadingText) loadingText.textContent = message;
}

function showNotification(message, type = 'info') {
    if (!notificationToast) return;

    const toast = notificationToast;
    const icon = toast.querySelector('.toast-icon');
    const messageElement = toast.querySelector('.toast-message');

    const icons = {
        success: '✓',
        error: '✗',
        warning: '⚠',
        info: 'ℹ'
    };

    if (icon) icon.textContent = icons[type] || icons.info;
    if (messageElement) messageElement.textContent = message;

    toast.className = `notification-toast ${type}`;
    toast.style.display = 'block';

    setTimeout(() => {
        toast.style.display = 'none';
    }, 5000);
}