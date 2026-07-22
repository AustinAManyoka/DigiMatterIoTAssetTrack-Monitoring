// Global state
let allGroups = [];
let filteredGroups = [];
let selectedGroupId = null;
let currentEditingGroupId = null;

// Initialize page
async function initGroupsPage() {
    try {
        initLayout('groups', 'Groups');
        const pageContent = document.getElementById('page-content');
        
        // Build page HTML
        pageContent.innerHTML = `
            <div id="alerts-container"></div>
            
            <div class="controls">
                <button class="btn-primary" onclick="openGroupModal()">+ New Group</button>
                <input type="text" id="searchInput" placeholder="Search groups..." 
                       onkeyup="debounce(() => handleSearch(), 300)" style="padding: 0.75rem 1rem; border: 1px solid var(--border); border-radius: var(--radius); flex: 1; max-width: 300px;">
            </div>

            <div class="groups-container">
                <div class="group-tree" id="groupTree"></div>
                <div class="group-panel">
                    <div id="groupContent">
                        <div class="empty-state">
                            <p>Select a group to view details</p>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Group Form Modal -->
            <div class="modal" id="groupFormModal">
                <div class="modal-content">
                    <div class="modal-header">
                        <h3 id="formTitle">Add New Group</h3>
                        <button class="modal-close" onclick="closeModal('groupFormModal')">&times;</button>
                    </div>
                    <form id="groupForm" onsubmit="submitGroupForm(event)">
                        <div class="form-group">
                            <label for="groupName">Group Name *</label>
                            <input type="text" id="groupName" required>
                        </div>
                        <div class="form-group">
                            <label for="parentGroupId">Parent Group</label>
                            <select id="parentGroupId"></select>
                        </div>
                        <div class="form-group">
                            <label for="groupDescription">Description</label>
                            <textarea id="groupDescription"></textarea>
                        </div>
                        <div class="form-actions">
                            <button type="button" class="btn-secondary" onclick="closeModal('groupFormModal')">Cancel</button>
                            <button type="submit" class="btn-primary">Save Group</button>
                        </div>
                    </form>
                </div>
            </div>
        `;
        
        await loadGroups();
        
        // Add modal backdrop click handler
        pageContent.addEventListener('click', (e) => {
            if (e.target.classList.contains('modal')) {
                e.target.classList.remove('active');
            }
        });
    } catch (error) {
        showAlert('alerts-container', `Failed to initialize page: ${error.message}`, 'error');
    }
}

// Load all groups
async function loadGroups() {
    try {
        const response = await apiRequest('/group');
        allGroups = response || [];
        filteredGroups = [...allGroups];
        renderGroupTree();
        populateParentGroupSelect();
    } catch (error) {
        showAlert('alerts-container', `Failed to load groups: ${error.message}`, 'error');
    }
}

// Render group tree
function renderGroupTree() {
    const treeContainer = document.getElementById('groupTree');
    if (!treeContainer) return;

    if (filteredGroups.length === 0) {
        treeContainer.innerHTML = '<div class="empty-state">No groups found</div>';
        return;
    }

    const rootGroups = filteredGroups.filter(g => !g.parentGroupId);
    
    treeContainer.innerHTML = rootGroups.map(group => 
        renderGroupTreeItem(group, 0)
    ).join('');
}

// Render individual tree item
function renderGroupTreeItem(group, level) {
    const childGroups = filteredGroups.filter(g => g.parentGroupId === group.groupId);
    const hasChildren = childGroups.length > 0;
    
    let html = `
        <div class="tree-item ${selectedGroupId === group.groupId ? 'active' : ''}" 
             onclick="selectGroup(${group.groupId})" 
             style="${level > 0 ? `margin-left: ${level * 1.5}rem;` : ''}">
            ${hasChildren ? '📁' : '📄'} ${escapeHtml(group.name)}
            <span style="color: var(--text-muted); font-size: 0.85rem; margin-left: 0.5rem;">
                (${group.deviceCount || 0})
            </span>
        </div>
    `;

    if (hasChildren) {
        html += childGroups.map(child => renderGroupTreeItem(child, level + 1)).join('');
    }

    return html;
}

// Select a group
async function selectGroup(groupId) {
    selectedGroupId = groupId;
    renderGroupTree();
    
    const group = allGroups.find(g => g.groupId === groupId);
    if (group) {
        displayGroupDetails(group);
    }
}

// Display group details
async function displayGroupDetails(group) {
    const content = document.getElementById('groupContent');
    
    try {
        // Get devices in this group
        const devices = await apiRequest(`/device?groupId=${group.groupId}&pageSize=1000`);
        const deviceList = devices.data || [];

        const childGroups = allGroups.filter(g => g.parentGroupId === group.groupId);
        
        content.innerHTML = `
            <div class="group-header">
                <h3 style="margin: 0;">${escapeHtml(group.name)}</h3>
                <div class="btn-group">
                    <button class="btn-icon" onclick="editGroup(${group.groupId})">Edit</button>
                    <button class="btn-icon" onclick="deleteGroup(${group.groupId})" style="background: #fee2e2; color: var(--danger);">Delete</button>
                </div>
            </div>

            <div class="group-details">
                <div class="detail-item">
                    <div class="detail-label">Total Devices</div>
                    <div class="detail-value">${deviceList.length}</div>
                </div>
                <div class="detail-item">
                    <div class="detail-label">Sub-Groups</div>
                    <div class="detail-value">${childGroups.length}</div>
                </div>
                <div class="detail-item">
                    <div class="detail-label">Created</div>
                    <div class="detail-value">${formatDate(group.createdDate)}</div>
                </div>
                <div class="detail-item">
                    <div class="detail-label">Parent Group</div>
                    <div class="detail-value">${group.parentGroupId ? (allGroups.find(g => g.groupId === group.parentGroupId)?.name || '-') : 'None'}</div>
                </div>
            </div>

            ${group.description ? `
                <div style="margin-bottom: 2rem; padding: 1rem; background: var(--bg); border-radius: var(--radius);">
                    <h4 style="margin-top: 0;">Description</h4>
                    <p>${escapeHtml(group.description)}</p>
                </div>
            ` : ''}

            ${childGroups.length > 0 ? `
                <div style="margin-bottom: 2rem;">
                    <h4>Sub-Groups</h4>
                    ${childGroups.map(child => `
                        <div class="device-item" onclick="selectGroup(${child.groupId})">
                            <div class="device-item-info">
                                <div class="device-item-name">📁 ${escapeHtml(child.name)}</div>
                                <div class="device-item-serial">${child.deviceCount || 0} device(s)</div>
                            </div>
                            <button class="btn-icon" onclick="event.stopPropagation(); selectGroup(${child.groupId})">→</button>
                        </div>
                    `).join('')}
                </div>
            ` : ''}

            <div class="devices-in-group">
                <h4>Devices in this Group</h4>
                ${deviceList.length > 0 ? 
                    deviceList.map(device => `
                        <div class="device-item">
                            <div class="device-item-info">
                                <div class="device-item-name">${escapeHtml(device.name)}</div>
                                <div class="device-item-serial">${escapeHtml(device.serialNumber)}</div>
                            </div>
                            <span class="status-badge ${device.isActive ? 'status-active' : 'status-inactive'}">
                                ${device.isActive ? 'Active' : 'Inactive'}
                            </span>
                        </div>
                    `).join('')
                    : '<p style="color: var(--text-muted);">No devices in this group</p>'}
            </div>
        `;
    } catch (error) {
        content.innerHTML = `<div class="alert alert-error">Failed to load group details: ${error.message}</div>`;
    }
}

// Populate parent group select in form
function populateParentGroupSelect() {
    const select = document.getElementById('parentGroupId');
    if (!select) return;

    select.innerHTML = '<option value="">No Parent (Root Group)</option>';
    
    allGroups.forEach(group => {
        // Don't allow a group to be its own parent
        if (group.groupId !== currentEditingGroupId) {
            const option = document.createElement('option');
            option.value = group.groupId;
            option.textContent = group.name;
            select.appendChild(option);
        }
    });
}

// Search functionality
function handleSearch() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    
    filteredGroups = allGroups.filter(group =>
        group.name.toLowerCase().includes(searchTerm) ||
        (group.description && group.description.toLowerCase().includes(searchTerm))
    );

    renderGroupTree();
}

// Open group modal for creation
function openGroupModal() {
    currentEditingGroupId = null;
    document.getElementById('formTitle').textContent = 'Add New Group';
    document.getElementById('groupForm').reset();
    populateParentGroupSelect();
    openModal('groupFormModal');
}

// Edit group
async function editGroup(groupId) {
    try {
        const group = allGroups.find(g => g.groupId === groupId);
        if (!group) return;

        currentEditingGroupId = groupId;
        document.getElementById('formTitle').textContent = 'Edit Group';
        document.getElementById('groupName').value = group.name;
        document.getElementById('parentGroupId').value = group.parentGroupId || '';
        document.getElementById('groupDescription').value = group.description || '';
        
        populateParentGroupSelect();
        openModal('groupFormModal');
    } catch (error) {
        showAlert('alerts-container', `Failed to load group: ${error.message}`, 'error');
    }
}

// Submit group form
async function submitGroupForm(event) {
    event.preventDefault();

    try {
        const formData = {
            name: document.getElementById('groupName').value,
            parentGroupId: document.getElementById('parentGroupId').value ? 
                parseInt(document.getElementById('parentGroupId').value) : null,
            description: document.getElementById('groupDescription').value || null
        };

        if (currentEditingGroupId) {
            // Update group
            await apiRequest(`/group/${currentEditingGroupId}`, {
                method: 'PUT',
                body: JSON.stringify(formData)
            });
            showAlert('alerts-container', 'Group updated successfully!', 'success');
        } else {
            // Create group
            await apiRequest('/group', {
                method: 'POST',
                body: JSON.stringify(formData)
            });
            showAlert('alerts-container', 'Group created successfully!', 'success');
        }

        closeModal('groupFormModal');
        currentEditingGroupId = null;
        await loadGroups();
    } catch (error) {
        showAlert('alerts-container', `Failed to save group: ${error.message}`, 'error');
    }
}

// Delete group
async function deleteGroup(groupId) {
    if (!confirm('Are you sure you want to delete this group? Child groups will remain but be unassigned.')) return;

    try {
        // Note: delete endpoint, 
        await apiRequest(`/group/${groupId}`, { method: 'DELETE' });
        showAlert('alerts-container', 'Group deleted successfully!', 'success');
        selectedGroupId = null;
        document.getElementById('groupContent').innerHTML = '<div class="empty-state"><p>Select a group to view details</p></div>';
        await loadGroups();
    } catch (error) {
        showAlert('alerts-container', `Failed to delete group: ${error.message}`, 'error');
    }
}

// Modal management
function openModal(modalId) {
    document.getElementById(modalId).classList.add('active');
}

function closeModal(modalId) {
    document.getElementById(modalId).classList.remove('active');
    if (modalId === 'groupFormModal') {
        currentEditingGroupId = null;
    }
}

// Close modal on backdrop click
document.addEventListener('click', (e) => {
    if (e.target.classList.contains('modal')) {
        e.target.classList.remove('active');
    }
});

// Initialize on page load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initGroupsPage);
} else {
    initGroupsPage();
}
