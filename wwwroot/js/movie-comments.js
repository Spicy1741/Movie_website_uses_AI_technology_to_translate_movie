// Movie Comments JavaScript Functions
let currentMovieId = null;
let currentPage = 1;
const commentsPerPage = 10;
let isLoading = false;

// Initialize comments when page loads
document.addEventListener('DOMContentLoaded', function () {
    currentMovieId = getMovieIdFromPage();
    if (currentMovieId) {
        // Initial comments are already loaded from server
        initializeCommentFeatures();
    }
});

// Get movie ID from page data
function getMovieIdFromPage() {
    const movieIdMeta = document.querySelector('meta[name="movie-id"]');
    const movieSection = document.querySelector('[data-movie-id]');

    if (movieIdMeta) {
        return parseInt(movieIdMeta.getAttribute('content'));
    } else if (movieSection) {
        return parseInt(movieSection.getAttribute('data-movie-id'));
    }

    // Fallback: try to get from URL
    const urlParams = new URLSearchParams(window.location.search);
    const movieId = urlParams.get('id') || window.location.pathname.split('/').pop();
    return movieId ? parseInt(movieId) : null;
}

// Initialize comment features for existing comments
function initializeCommentFeatures() {
    // Add event listeners for existing comment action buttons
    document.querySelectorAll('.comment-action-btn').forEach(btn => {
        if (btn.onclick === null) {
            // Only add listeners if not already set
            const onclickAttr = btn.getAttribute('onclick');
            if (onclickAttr && onclickAttr.includes('likeComment')) {
                const commentId = extractCommentIdFromOnclick(onclickAttr);
                btn.onclick = () => likeComment(commentId);
            }
        }
    });
}

// Extract comment ID from onclick attribute
function extractCommentIdFromOnclick(onclickStr) {
    const match = onclickStr.match(/likeComment\((\d+)\)/);
    return match ? parseInt(match[1]) : null;
}

// Add a new comment
async function addComment() {
    const commentTextarea = document.getElementById('newComment');
    const content = commentTextarea.value.trim();

    if (!content) {
        showNotification('Please enter a comment', 'warning');
        return;
    }

    if (content.length > 1000) {
        showNotification('Comment cannot exceed 1000 characters', 'error');
        return;
    }

    if (!currentMovieId) {
        showNotification('Movie not found', 'error');
        return;
    }

    try {
        showButtonLoading(true);

        const formData = new FormData();
        formData.append('movieId', currentMovieId);
        formData.append('content', content);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        const response = await fetch('/Movie/AddComment', {
            method: 'POST',
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            // Clear the textarea
            commentTextarea.value = '';

            // Add the new comment to the top of the list
            addCommentToList(result.comment, true);

            // Update comment count
            updateCommentCount(1);

            // Remove "no comments" message if it exists
            const noCommentsMsg = document.querySelector('.no-comments-message');
            if (noCommentsMsg) {
                noCommentsMsg.remove();
            }

            showNotification('Comment added successfully! 💥', 'success');
        } else {
            showNotification(result.message || 'Failed to add comment', 'error');
        }
    } catch (error) {
        console.error('Error adding comment:', error);
        showNotification('An error occurred while adding the comment', 'error');
    } finally {
        showButtonLoading(false);
    }
}

// Cancel comment (clear textarea)
function cancelComment() {
    const commentTextarea = document.getElementById('newComment');
    commentTextarea.value = '';
    commentTextarea.focus();
}

// Load more comments
async function loadMoreComments() {
    if (isLoading || !currentMovieId) return;

    try {
        isLoading = true;
        showCommentsLoading(true);

        const response = await fetch(`/Movie/GetComments?movieId=${currentMovieId}&page=${currentPage + 1}&pageSize=${commentsPerPage}`);
        const result = await response.json();

        if (result.success) {
            // Add comments to the list
            result.comments.forEach(comment => {
                addCommentToList(comment, false);
            });

            // Update pagination
            currentPage++;
            updateLoadMoreButton(result.hasNextPage);
        } else {
            showNotification(result.message || 'Failed to load comments', 'error');
        }
    } catch (error) {
        console.error('Error loading comments:', error);
        showNotification('An error occurred while loading comments', 'error');
    } finally {
        isLoading = false;
        showCommentsLoading(false);
    }
}

// Add comment to the comments list
function addCommentToList(comment, prepend = false) {
    const commentsList = document.getElementById('commentsList');
    const commentElement = createCommentElement(comment);

    if (prepend) {
        commentsList.insertAdjacentHTML('afterbegin', commentElement);
    } else {
        commentsList.insertAdjacentHTML('beforeend', commentElement);
    }
}

// Create HTML for a comment
function createCommentElement(comment) {
    const timeAgo = getTimeAgo(comment.createdAt);
    const editedText = comment.isEdited ? ' (edited)' : '';

    return `
        <div class="comment-item" data-comment-id="${comment.id}">
            <div class="comment-header">
                <div class="comment-avatar">${comment.userInitials}</div>
                <div class="comment-info">
                    <div class="comment-author">${escapeHtml(comment.userDisplayName)}</div>
                    <div class="comment-time">${timeAgo}${editedText}</div>
                </div>
                ${comment.canEdit || comment.canDelete ? `
                    <div class="comment-options">
                        <button class="comment-option-btn" onclick="toggleCommentOptions(${comment.id})">⋮</button>
                        <div class="comment-options-menu" id="options-${comment.id}" style="display: none;">
                            ${comment.canEdit ? `<button onclick="editComment(${comment.id})">Edit</button>` : ''}
                            ${comment.canDelete ? `<button onclick="deleteComment(${comment.id})" class="delete-option">Delete</button>` : ''}
                        </div>
                    </div>
                ` : ''}
            </div>
            <div class="comment-content" id="content-${comment.id}">
                ${escapeHtml(comment.content)}
            </div>
            <div class="comment-actions-inline">
                <button class="comment-action-btn ${comment.isLiked ? 'liked' : ''}" onclick="likeComment(${comment.id})" id="like-btn-${comment.id}">
                    👍 <span id="like-count-${comment.id}">${comment.likeCount}</span>
                </button>
                <button class="comment-action-btn" onclick="replyComment(${comment.id})">↩ Reply</button>
            </div>
            ${comment.repliesCount > 0 ? `
                <div class="replies-container" id="replies-${comment.id}">
                    <button class="load-replies-btn" onclick="loadReplies(${comment.id})">
                        ⬇ Load ${comment.repliesCount} ${comment.repliesCount === 1 ? 'reply' : 'replies'}
                    </button>
                </div>
            ` : ''}
        </div>
    `;
}

// Like/unlike a comment
async function likeComment(commentId) {
    try {
        const likeBtn = document.getElementById(`like-btn-${commentId}`);
        const likeCount = document.getElementById(`like-count-${commentId}`);

        if (!likeBtn || !likeCount) {
            console.error('Like button or count element not found');
            return;
        }

        // Optimistic UI update
        const wasLiked = likeBtn.classList.contains('liked');
        const currentCount = parseInt(likeCount.textContent) || 0;

        likeBtn.classList.toggle('liked');
        likeCount.textContent = wasLiked ? Math.max(0, currentCount - 1) : currentCount + 1;

        const formData = new FormData();
        formData.append('commentId', commentId);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        const response = await fetch('/Movie/LikeComment', {
            method: 'POST',
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            // Update UI with server response
            likeCount.textContent = result.likeCount;
            if (result.liked) {
                likeBtn.classList.add('liked');
            } else {
                likeBtn.classList.remove('liked');
            }
        } else {
            // Revert optimistic update on error
            if (wasLiked) {
                likeBtn.classList.add('liked');
            } else {
                likeBtn.classList.remove('liked');
            }
            likeCount.textContent = currentCount;
            showNotification(result.message || 'Failed to process like', 'error');
        }
    } catch (error) {
        console.error('Error liking comment:', error);
        showNotification('An error occurred while processing the like', 'error');

        // Revert UI on error
        const likeBtn = document.getElementById(`like-btn-${commentId}`);
        const likeCount = document.getElementById(`like-count-${commentId}`);
        if (likeBtn && likeCount) {
            // This is a simplified revert - in a real app you might want to track the original state
            location.reload();
        }
    }
}

// Edit a comment
function editComment(commentId) {
    const contentElement = document.getElementById(`content-${commentId}`);
    const currentContent = contentElement.textContent.trim();

    // Replace content with textarea
    contentElement.innerHTML = `
        <textarea class="edit-comment-textarea" id="edit-textarea-${commentId}" maxlength="1000">${escapeHtml(currentContent)}</textarea>
        <div class="edit-comment-actions">
            <button class="action-btn btn-small" onclick="saveEditComment(${commentId})">💾 Save</button>
            <button class="action-btn btn-small" onclick="cancelEditComment(${commentId}, '${escapeHtml(currentContent).replace(/'/g, "\\'")}')">❌ Cancel</button>
        </div>
    `;

    // Focus on textarea
    const textarea = document.getElementById(`edit-textarea-${commentId}`);
    if (textarea) {
        textarea.focus();
        textarea.setSelectionRange(textarea.value.length, textarea.value.length);
    }

    // Hide options menu
    toggleCommentOptions(commentId, false);
}

// Save edited comment
async function saveEditComment(commentId) {
    const textarea = document.getElementById(`edit-textarea-${commentId}`);
    const newContent = textarea.value.trim();

    if (!newContent) {
        showNotification('Comment content cannot be empty', 'warning');
        return;
    }

    if (newContent.length > 1000) {
        showNotification('Comment cannot exceed 1000 characters', 'error');
        return;
    }

    try {
        const formData = new FormData();
        formData.append('commentId', commentId);
        formData.append('content', newContent);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        const response = await fetch('/Movie/EditComment', {
            method: 'POST',
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            // Update the content
            const contentElement = document.getElementById(`content-${commentId}`);
            contentElement.innerHTML = escapeHtml(result.comment.content);

            // Update timestamp to show edited
            const timeElement = document.querySelector(`[data-comment-id="${commentId}"] .comment-time`);
            if (timeElement && result.comment.updatedAt) {
                timeElement.textContent = getTimeAgo(result.comment.updatedAt) + ' (edited)';
            }

            showNotification('Comment updated successfully! ✏️', 'success');
        } else {
            showNotification(result.message || 'Failed to update comment', 'error');
        }
    } catch (error) {
        console.error('Error editing comment:', error);
        showNotification('An error occurred while updating the comment', 'error');
    }
}

// Cancel edit comment
function cancelEditComment(commentId, originalContent) {
    const contentElement = document.getElementById(`content-${commentId}`);
    contentElement.innerHTML = escapeHtml(originalContent);
}

// Delete a comment
async function deleteComment(commentId) {
    if (!confirm('Are you sure you want to delete this comment? This action cannot be undone.')) {
        return;
    }

    try {
        const formData = new FormData();
        formData.append('commentId', commentId);
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        const response = await fetch('/Movie/DeleteComment', {
            method: 'POST',
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            // Remove comment from DOM
            const commentElement = document.querySelector(`[data-comment-id="${commentId}"]`);
            if (commentElement) {
                commentElement.remove();
            }

            // Update comment count
            updateCommentCount(-1);

            // Show no comments message if no comments left
            const remainingComments = document.querySelectorAll('.comment-item');
            if (remainingComments.length === 0) {
                const commentsList = document.getElementById('commentsList');
                commentsList.innerHTML = `
                    <div class="no-comments-message">
                        <p>🎬 Be the first to share your EPIC thoughts about this movie! 🎬</p>
                    </div>
                `;
            }

            showNotification('Comment deleted successfully! 🗑️', 'success');
        } else {
            showNotification(result.message || 'Failed to delete comment', 'error');
        }
    } catch (error) {
        console.error('Error deleting comment:', error);
        showNotification('An error occurred while deleting the comment', 'error');
    }

    // Hide options menu
    toggleCommentOptions(commentId, false);
}

// Reply to a comment (placeholder - implement as needed)
function replyComment(commentId) {
    showNotification('Reply functionality coming soon! 🚧', 'info');
}

// Toggle comment options menu
function toggleCommentOptions(commentId, show = null) {
    const optionsMenu = document.getElementById(`options-${commentId}`);
    if (optionsMenu) {
        if (show === null) {
            optionsMenu.style.display = optionsMenu.style.display === 'none' ? 'block' : 'none';
        } else {
            optionsMenu.style.display = show ? 'block' : 'none';
        }
    }
}

// Update comment count display
function updateCommentCount(change = 0) {
    const countElement = document.querySelector('.comment-count');
    if (countElement) {
        const currentCount = parseInt(countElement.textContent.replace(/[()]/g, '')) || 0;
        const newCount = Math.max(0, currentCount + change);
        countElement.textContent = `(${newCount})`;
    }
}

// Update load more button visibility
function updateLoadMoreButton(hasNextPage) {
    const loadMoreBtn = document.querySelector('.load-more-container button');
    if (loadMoreBtn) {
        loadMoreBtn.style.display = hasNextPage ? 'block' : 'none';
    }
}

// Show/hide comments loading state
function showCommentsLoading(show) {
    const commentsList = document.getElementById('commentsList');
    if (show) {
        const loadingElement = document.createElement('div');
        loadingElement.className = 'comments-loading';
        loadingElement.innerHTML = '🔄 Loading epic reviews...';
        commentsList.appendChild(loadingElement);
    } else {
        const loadingElement = commentsList?.querySelector('.comments-loading');
        if (loadingElement) {
            loadingElement.remove();
        }
    }
}

// Show/hide button loading state
function showButtonLoading(show) {
    const button = document.querySelector('[onclick*="addComment"]');
    if (button) {
        if (show) {
            button.disabled = true;
            button.innerHTML = '🔄 POSTING...';
        } else {
            button.disabled = false;
            button.innerHTML = '💥 POST REVIEW';
        }
    }
}

// Utility functions
function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (!token) {
        console.warn('Anti-forgery token not found');
        // Try to get from meta tag as fallback
        return document.querySelector('meta[name="__RequestVerificationToken"]')?.content || '';
    }
    return token;
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function getTimeAgo(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diffInSeconds = Math.floor((now - date) / 1000);

    const intervals = [
        { label: 'year', seconds: 31536000 },
        { label: 'month', seconds: 2592000 },
        { label: 'day', seconds: 86400 },
        { label: 'hour', seconds: 3600 },
        { label: 'minute', seconds: 60 }
    ];

    for (const interval of intervals) {
        const count = Math.floor(diffInSeconds / interval.seconds);
        if (count > 0) {
            return `${count} ${interval.label}${count > 1 ? 's' : ''} ago`;
        }
    }

    return 'just now';
}

function showNotification(message, type = 'info') {
    // Remove existing notifications
    document.querySelectorAll('.notification').forEach(notification => {
        notification.remove();
    });

    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <span>${message}</span>
        <button onclick="this.parentElement.remove()">×</button>
    `;

    // Add to page
    document.body.appendChild(notification);

    // Auto-remove after 5 seconds
    setTimeout(() => {
        if (notification.parentElement) {
            notification.remove();
        }
    }, 5000);
}

// Close options menus when clicking outside
document.addEventListener('click', function (event) {
    if (!event.target.closest('.comment-options')) {
        document.querySelectorAll('.comment-options-menu').forEach(menu => {
            menu.style.display = 'none';
        });
    }
});

// Handle textarea auto-resize
document.addEventListener('input', function (event) {
    if (event.target.matches('.comment-textarea, .edit-comment-textarea')) {
        event.target.style.height = 'auto';
        event.target.style.height = event.target.scrollHeight + 'px';
    }
});