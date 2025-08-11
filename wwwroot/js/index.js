// Toggle between main admin view and user management view
function showUserManagement() {
    document.getElementById('mainAdminSection').style.display = 'none';
    document.getElementById('userManagementSection').style.display = 'block';

    // Smooth scroll to top
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function backToMainAdmin() {
    document.getElementById('userManagementSection').style.display = 'none';
    document.getElementById('mainAdminSection').style.display = 'block';

    // Smooth scroll to top
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

// User management functionality (only loaded when user management is shown)
document.addEventListener('DOMContentLoaded', function () {
    // Search functionality
    const searchInput = document.getElementById('userSearch');
    const roleFilter = document.getElementById('roleFilter');
    const userRows = document.querySelectorAll('.user-row');

    function filterUsers() {
        const searchTerm = searchInput?.value.toLowerCase() || '';
        const selectedRole = roleFilter?.value || '';

        userRows.forEach(row => {
            const text = row.textContent.toLowerCase();
            const roleColumn = row.querySelector('td:nth-child(4)')?.textContent || '';

            const matchesSearch = text.includes(searchTerm);
            const matchesRole = !selectedRole || roleColumn.includes(selectedRole);

            if (matchesSearch && matchesRole) {
                row.classList.remove('filtered-out');
            } else {
                row.classList.add('filtered-out');
            }
        });

        updatePaginationInfo();
    }

    if (searchInput) searchInput.addEventListener('input', filterUsers);
    if (roleFilter) roleFilter.addEventListener('change', filterUsers);

    // Select all functionality
    const selectAllCheckbox = document.getElementById('selectAll');
    const userCheckboxes = document.querySelectorAll('.user-checkbox');

    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function () {
            userCheckboxes.forEach(checkbox => {
                checkbox.checked = this.checked;
            });
        });
    }

    function updatePaginationInfo() {
        const visibleRows = document.querySelectorAll('.user-row:not(.filtered-out)').length;
        const showingEndElement = document.getElementById('showingEnd');
        const totalEntriesElement = document.getElementById('totalEntries');

        if (showingEndElement) showingEndElement.textContent = visibleRows;
        if (totalEntriesElement) totalEntriesElement.textContent = visibleRows;
    }
});

// User action functions
function viewUser(userId) {
    // Find user data from the table row
    const userRow = document.querySelector(`tr[data-user-id="${userId}"]`);
    if (!userRow) return;

    // Extract user information from the table row
    const userInfo = userRow.querySelector('td:nth-child(2)');
    const contactInfo = userRow.querySelector('td:nth-child(3)');
    const roleInfo = userRow.querySelector('td:nth-child(4)');
    const statusInfo = userRow.querySelector('td:nth-child(5)');
    const joinedInfo = userRow.querySelector('td:nth-child(6)');

    // Get user details
    const fullName = userInfo.querySelector('h6').textContent.trim();
    const username = userInfo.querySelector('small').textContent.trim();
    const email = contactInfo.querySelector('.fa-envelope').parentNode.textContent.replace('✉', '').trim();
    const phone = contactInfo.querySelector('.fa-phone')?.parentNode.textContent.replace('📞', '').trim() || 'Not provided';
    const roles = Array.from(roleInfo.querySelectorAll('.badge')).map(badge => badge.textContent.trim()).join(', ');
    const status = statusInfo.querySelector('.badge').textContent.trim();
    const joinDate = joinedInfo.textContent.trim();

    // Populate modal - Header section
    document.getElementById('viewUserName').textContent = fullName;
    document.getElementById('viewUserUsername').textContent = username;
    document.getElementById('viewUserRoles').innerHTML = roleInfo.innerHTML;

    // Populate modal - Information cards
    document.getElementById('viewUserName2').textContent = fullName;
    document.getElementById('viewUserUsername2').textContent = username;
    document.getElementById('viewUserEmail').textContent = email;
    document.getElementById('viewUserPhone').textContent = phone;
    document.getElementById('viewUserStatus').innerHTML = statusInfo.innerHTML;
    document.getElementById('viewUserRoles2').innerHTML = roleInfo.innerHTML;
    document.getElementById('viewUserJoined').textContent = joinDate;
    document.getElementById('viewUserId').textContent = userId;

    // Set avatar initials
    const avatarText = userInfo.querySelector('.avatar-circle span').textContent;
    document.getElementById('viewUserAvatar').textContent = avatarText;

    // Store current user ID for modal actions
    document.getElementById('viewUserModal').setAttribute('data-user-id', userId);

    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('viewUserModal'));
    modal.show();
}

// Modal action functions
function editUserFromModal() {
    const userId = document.getElementById('viewUserModal').getAttribute('data-user-id');
    // Close the view modal first
    const viewModal = bootstrap.Modal.getInstance(document.getElementById('viewUserModal'));
    if (viewModal) {
        viewModal.hide();
    }

    // Then call edit function
    setTimeout(() => {
        editUser(userId);
    }, 300);
}

function viewUserActivity() {
    const userId = document.getElementById('viewUserModal').getAttribute('data-user-id');

    // Close the user details modal
    const userModal = bootstrap.Modal.getInstance(document.getElementById('viewUserModal'));
    if (userModal) {
        userModal.hide();
    }

    // Load activities and show activity modal
    setTimeout(() => {
        loadUserActivities(userId);
    }, 300);
}

// Load and display user activities
async function loadUserActivities(userId) {
    try {
        // Show loading state
        document.getElementById('activityLoadingSpinner').style.display = 'block';
        document.getElementById('activityContent').style.display = 'none';
        document.getElementById('activityErrorMessage').style.display = 'none';

        // Show the modal
        const activityModal = new bootstrap.Modal(document.getElementById('userActivityModal'));
        activityModal.show();

        // Fetch activities
        const response = await fetch(`/Admin/GetUserActivities?userId=${userId}`);

        if (!response.ok) {
            throw new Error('Failed to load activities');
        }

        const data = await response.json();

        // Hide loading and show content
        document.getElementById('activityLoadingSpinner').style.display = 'none';
        document.getElementById('activityContent').style.display = 'block';

        // Populate user info
        if (data.user) {
            document.getElementById('activityUserName').textContent = data.user.name;
            document.getElementById('activityUserEmail').textContent = data.user.email;
            document.getElementById('activityUserUsername').textContent = data.user.username;
            document.getElementById('activityTotalCount').textContent = data.totalCount;
        }

        // Populate activities
        const activitiesContainer = document.getElementById('activitiesContainer');
        activitiesContainer.innerHTML = '';

        if (data.activities && data.activities.length > 0) {
            data.activities.forEach(activity => {
                const activityElement = createActivityElement(activity);
                activitiesContainer.appendChild(activityElement);
            });
        } else {
            activitiesContainer.innerHTML = `
                    <div class="text-center py-4">
                        <i class="fas fa-history text-muted" style="font-size: 3rem;"></i>
                        <h5 class="mt-3 text-muted">No Activities Found</h5>
                        <p class="text-muted">This user has no recorded activities yet.</p>
                    </div>
                `;
        }

    } catch (error) {
        console.error('Error loading activities:', error);

        // Hide loading and show error
        document.getElementById('activityLoadingSpinner').style.display = 'none';
        document.getElementById('activityContent').style.display = 'none';
        document.getElementById('activityErrorMessage').style.display = 'block';
    }
}

// Create activity element
function createActivityElement(activity) {
    const div = document.createElement('div');
    div.className = 'activity-item';

    // Get activity icon and color based on type
    const { icon, color } = getActivityIconAndColor(activity.activityType);

    div.innerHTML = `
    <div class="d-flex align-items-start">
        <div class="activity-icon ${color}">
            <i class="fas ${icon}"></i>
        </div>
        <div class="activity-details flex-grow-1">
            <div class="d-flex justify-content-between align-items-start">
                <div>
                    <h6 class="activity-title">${activity.activityType}</h6>
                    <p class="activity-description">${activity.description}</p>
                    <div class="activity-meta">
                        <span class="meta-item">
                            <i class="fas fa-clock me-1"></i>${activity.createdAt}
                        </span>
                        ${activity.ipAddress ? `
                                    <span class="meta-item">
                                        <i class="fas fa-globe me-1"></i>${activity.ipAddress}
                                    </span>
                                ` : ''}
                        <span class="meta-item">
                            <i class="fas fa-map-marker-alt me-1"></i>${activity.location}
                        </span>
                    </div>
                </div>
                <span class="badge bg-secondary">${activity.activityType}</span>
            </div>
        </div>
    </div>
    `;

    return div;
}

// Get icon and color for activity type
function getActivityIconAndColor(activityType) {
    const iconMap = {
        'Login': { icon: 'fa-sign-in-alt', color: 'bg-success' },
        'Logout': { icon: 'fa-sign-out-alt', color: 'bg-secondary' },
        'Register': { icon: 'fa-user-plus', color: 'bg-primary' },
        'Profile Update': { icon: 'fa-user-edit', color: 'bg-info' },
        'Password Change': { icon: 'fa-key', color: 'bg-warning' },
        'Password Reset': { icon: 'fa-unlock-alt', color: 'bg-warning' },
        'Role Changed': { icon: 'fa-shield-alt', color: 'bg-danger' },
        'Admin Access': { icon: 'fa-crown', color: 'bg-purple' },
        'Data Export': { icon: 'fa-download', color: 'bg-dark' },
        'Movie Viewed': { icon: 'fa-play', color: 'bg-success' }
    };

    return iconMap[activityType] || { icon: 'fa-circle', color: 'bg-secondary' };
}

function editUser(userId) {
    console.log('Edit user:', userId);
    alert('Edit user functionality will be implemented in the next step');
}

function deleteUser(userId) {
    if (confirm('Are you sure you want to delete this user?')) {
        console.log('Delete user:', userId);
        alert('Delete user functionality will be implemented in the next step');
    }
}

function exportUsers() {
    console.log('Export users');
    alert('Export functionality will be implemented in the next step');
}

// Additional activity modal functions
function refreshActivities() {
    const userId = document.getElementById('viewUserModal')?.getAttribute('data-user-id');
    if (userId) {
        loadUserActivities(userId);
    }
}

function exportUserActivities() {
    const userId = document.getElementById('viewUserModal')?.getAttribute('data-user-id');
    console.log('Export activities for user:', userId);
    alert('Export activities functionality will be implemented in a future update');
}

// ========== FIXED ADMIN SEARCH FUNCTIONALITY ========== 
// Replace the existing search functionality in your Admin/Index.cshtml with this corrected version

document.addEventListener('DOMContentLoaded', function () {
    // Search functionality - FIXED VERSION
    const searchInput = document.getElementById('userSearch');
    const roleFilter = document.getElementById('roleFilter');
    const userRows = document.querySelectorAll('.user-row');

    console.log('Search elements found:', {
        searchInput: !!searchInput,
        roleFilter: !!roleFilter,
        userRowsCount: userRows.length
    });

    function filterUsers() {
        const searchTerm = searchInput?.value.toLowerCase().trim() || '';
        const selectedRole = roleFilter?.value.toLowerCase().trim() || '';

        console.log('Filtering with:', { searchTerm, selectedRole });

        let visibleCount = 0;

        userRows.forEach((row, index) => {
            try {
                // Get text content from specific cells instead of entire row
                const nameCell = row.querySelector('td:nth-child(2)');
                const emailCell = row.querySelector('td:nth-child(3)');
                const roleCell = row.querySelector('td:nth-child(4)');

                // Extract text safely
                const userName = nameCell?.textContent?.toLowerCase().trim() || '';
                const userEmail = emailCell?.textContent?.toLowerCase().trim() || '';
                const userRole = roleCell?.textContent?.toLowerCase().trim() || '';

                // Combine searchable text (name + email + username if available)
                const searchableText = `${userName} ${userEmail}`.toLowerCase();

                // Check search term match
                const matchesSearch = searchTerm === '' ||
                    userName.includes(searchTerm) ||
                    userEmail.includes(searchTerm) ||
                    searchableText.includes(searchTerm);

                // Check role filter match
                const matchesRole = selectedRole === '' ||
                    userRole.includes(selectedRole);

                console.log(`Row ${index}:`, {
                    userName,
                    userEmail,
                    userRole,
                    searchTerm,
                    selectedRole,
                    matchesSearch,
                    matchesRole
                });

                // Show or hide row based on matches
                if (matchesSearch && matchesRole) {
                    row.style.display = '';
                    row.classList.remove('filtered-out');
                    visibleCount++;
                } else {
                    row.style.display = 'none';
                    row.classList.add('filtered-out');
                }

            } catch (error) {
                console.error('Error filtering row:', error, row);
                // In case of error, show the row to be safe
                row.style.display = '';
                row.classList.remove('filtered-out');
            }
        });

        console.log('Visible rows after filter:', visibleCount);
        updatePaginationInfo(visibleCount);
    }

    // Enhanced updatePaginationInfo function
    function updatePaginationInfo(visibleCount = null) {
        if (visibleCount === null) {
            visibleCount = document.querySelectorAll('.user-row:not(.filtered-out)').length;
        }

        const showingEndElement = document.getElementById('showingEnd');
        const totalEntriesElement = document.getElementById('totalEntries');
        const showingStartElement = document.getElementById('showingStart');

        if (showingEndElement) showingEndElement.textContent = visibleCount;
        if (totalEntriesElement) totalEntriesElement.textContent = visibleCount;
        if (showingStartElement) showingStartElement.textContent = visibleCount > 0 ? '1' : '0';

        // Update the "showing X of Y entries" text
        const paginationInfo = document.querySelector('.dataTables_info');
        if (paginationInfo) {
            const totalUsers = userRows.length;
            paginationInfo.textContent = `Showing ${visibleCount > 0 ? '1' : '0'} to ${visibleCount} of ${visibleCount} entries (filtered from ${totalUsers} total entries)`;
        }
    }

    // Attach event listeners with debouncing for better performance
    if (searchInput) {
        let searchTimeout;
        searchInput.addEventListener('input', function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(filterUsers, 300); // Debounce for 300ms
        });

        // Also trigger on Enter key
        searchInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                clearTimeout(searchTimeout);
                filterUsers();
            }
        });
    }

    if (roleFilter) {
        roleFilter.addEventListener('change', filterUsers);
    }

    // Clear search functionality
    function clearSearch() {
        if (searchInput) searchInput.value = '';
        if (roleFilter) roleFilter.value = '';
        filterUsers();
    }

    // Add clear button if it doesn't exist
    if (searchInput && !document.getElementById('clearSearchBtn')) {
        const clearBtn = document.createElement('button');
        clearBtn.id = 'clearSearchBtn';
        clearBtn.type = 'button';
        clearBtn.className = 'btn btn-outline-secondary btn-sm';
        clearBtn.innerHTML = '<i class="fas fa-times"></i>';
        clearBtn.title = 'Clear search';
        clearBtn.onclick = clearSearch;

        // Insert clear button after search input
        const searchParent = searchInput.parentElement;
        if (searchParent) {
            searchParent.style.position = 'relative';
            clearBtn.style.cssText = `
                position: absolute;
                right: 10px;
                top: 50%;
                transform: translateY(-50%);
                border: none;
                background: transparent;
                color: #6c757d;
                padding: 0.25rem 0.5rem;
                z-index: 10;
            `;
            searchParent.appendChild(clearBtn);
        }
    }

    // Select all functionality - IMPROVED
    const selectAllCheckbox = document.getElementById('selectAll');
    const userCheckboxes = document.querySelectorAll('.user-checkbox');

    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function () {
            const visibleCheckboxes = Array.from(userCheckboxes).filter(checkbox => {
                const row = checkbox.closest('.user-row');
                return row && !row.classList.contains('filtered-out') && row.style.display !== 'none';
            });

            visibleCheckboxes.forEach(checkbox => {
                checkbox.checked = this.checked;
            });
        });

        // Update select all checkbox based on visible selections
        userCheckboxes.forEach(checkbox => {
            checkbox.addEventListener('change', function () {
                const visibleCheckboxes = Array.from(userCheckboxes).filter(checkbox => {
                    const row = checkbox.closest('.user-row');
                    return row && !row.classList.contains('filtered-out') && row.style.display !== 'none';
                });

                const checkedVisible = visibleCheckboxes.filter(cb => cb.checked);
                selectAllCheckbox.checked = visibleCheckboxes.length > 0 && checkedVisible.length === visibleCheckboxes.length;
                selectAllCheckbox.indeterminate = checkedVisible.length > 0 && checkedVisible.length < visibleCheckboxes.length;
            });
        });
    }

    // Initialize pagination info on page load
    updatePaginationInfo();

    // Add search status indicator
    function addSearchStatus() {
        if (!document.getElementById('searchStatus')) {
            const statusDiv = document.createElement('div');
            statusDiv.id = 'searchStatus';
            statusDiv.className = 'mt-2 text-muted small';
            statusDiv.style.cssText = 'min-height: 20px;';

            const searchContainer = searchInput?.closest('.col-md-6');
            if (searchContainer) {
                searchContainer.appendChild(statusDiv);
            }
        }
    }

    // Update search status
    function updateSearchStatus(searchTerm, visibleCount, totalCount) {
        const statusDiv = document.getElementById('searchStatus');
        if (statusDiv) {
            if (searchTerm) {
                statusDiv.innerHTML = `<i class="fas fa-info-circle me-1"></i>Found ${visibleCount} result(s) for "${searchTerm}"`;
                statusDiv.className = 'mt-2 text-info small';
            } else {
                statusDiv.innerHTML = '';
                statusDiv.className = 'mt-2 text-muted small';
            }
        }
    }

    addSearchStatus();

    // Enhanced filter function with status updates
    const originalFilterUsers = filterUsers;
    filterUsers = function () {
        const searchTerm = searchInput?.value.toLowerCase().trim() || '';
        originalFilterUsers();
        const visibleCount = document.querySelectorAll('.user-row:not(.filtered-out)').length;
        updateSearchStatus(searchTerm, visibleCount, userRows.length);
    };
});

// ========== ADDITIONAL CSS FOR BETTER SEARCH UX ========== 
// Add this CSS to your admin styles

const searchStyles = document.createElement('style');
searchStyles.textContent = `
    /* Enhanced search input styling */
    #userSearch {
        padding-right: 40px !important;
        transition: all 0.3s ease;
    }
    
    #userSearch:focus {
        box-shadow: 0 0 0 0.2rem rgba(220, 38, 38, 0.25);
        border-color: #dc2626;
    }
    
    /* Clear button styling */
    #clearSearchBtn:hover {
        color: #dc2626 !important;
        background: rgba(220, 38, 38, 0.1) !important;
    }
    
    /* Filtered out rows */
    .filtered-out {
        display: none !important;
    }
    
    /* Search status styling */
    #searchStatus {
        transition: all 0.3s ease;
    }
    
    /* Highlight search matches */
    .search-highlight {
        background-color: rgba(255, 255, 0, 0.3);
        padding: 2px 4px;
        border-radius: 3px;
    }
    
    /* Loading state for search */
    .search-loading {
        position: relative;
    }
    
    .search-loading::after {
        content: '';
        position: absolute;
        right: 40px;
        top: 50%;
        transform: translateY(-50%);
        width: 16px;
        height: 16px;
        border: 2px solid #f3f3f3;
        border-top: 2px solid #dc2626;
        border-radius: 50%;
        animation: spin 1s linear infinite;
    }
    
    @keyframes spin {
        0% { transform: translateY(-50%) rotate(0deg); }
        100% { transform: translateY(-50%) rotate(360deg); }
    }
    
    /* Responsive search improvements */
    @media (max-width: 768px) {
        #userSearch {
            font-size: 16px; /* Prevents zoom on iOS */
        }
        
        .search-container {
            margin-bottom: 1rem;
        }
    }
`;

document.head.appendChild(searchStyles);

// ========== DEBUGGING HELPER FUNCTIONS ========== 
// Add these functions to help debug search issues

window.debugSearch = function () {
    const searchInput = document.getElementById('userSearch');
    const userRows = document.querySelectorAll('.user-row');

    console.log('=== SEARCH DEBUG INFO ===');
    console.log('Search input value:', searchInput?.value);
    console.log('Total user rows:', userRows.length);
    console.log('Visible rows:', document.querySelectorAll('.user-row:not(.filtered-out)').length);
    console.log('Hidden rows:', document.querySelectorAll('.user-row.filtered-out').length);

    userRows.forEach((row, index) => {
        const nameCell = row.querySelector('td:nth-child(2)');
        const emailCell = row.querySelector('td:nth-child(3)');
        const isVisible = !row.classList.contains('filtered-out') && row.style.display !== 'none';

        console.log(`Row ${index}:`, {
            name: nameCell?.textContent?.trim(),
            email: emailCell?.textContent?.trim(),
            isVisible: isVisible,
            classes: row.className,
            display: row.style.display
        });
    });
};

// Call this function in browser console if you need to debug:
// debugSearch();