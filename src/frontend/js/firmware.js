// Global state
let allFirmware = [];
let filteredFirmware = [];
let deviceCountMap = {};
let currentEditingFirmwareId = null;

// Initialize page
async function initFirmwarePage() {
    try {
        initLayout('firmware', 'Firmware');
        const pageContent = document.getElementById('page-content');
        
        // Build page HTML
        pageContent.innerHTML = `
            <div id="alerts-container"></div>
            
            <div class="controls-bar">
                <div class="search-box">
                    <input 
                        type="text" 
                        id="searchInput" 
                        placeholder="Search by version or device type..."
                        onkeyup="debounce(() => handleSearch(), 300)"
                    >
                    <button onclick="handleSearch()">Search</button>
                    <button onclick="resetFilters()" style="background: var(--text-muted);">Reset</button>
                </div>
                <div class="filter-group">
                    <select id="typeFilter" onchange="applyFilters()">
                        <option value="">All Device Types</option>
                    </select>
                </div>
                <div>
                    <button class="btn-primary" onclick="openFirmwareModal()">+ New Firmware</button>
                </div>
            </div>

            <div class="table-container">
                <div class="table-scroll">
                    <table id="firmwareTable">
                        <thead>
                            <tr>
                                <th>Device Type</th>
                                <th>Version</th>
                                <th>Release Date</th>
                                <th>Devices Using</th>
                                <th>Notes</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody id="firmwareTableBody">
                            <tr><td colspan="6" class="empty-state">Loading firmware...</td></tr>
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Firmware Form Modal -->
            <div class="modal" id="firmwareFormModal">
                <div class="modal-content">
                    <div class="modal-header">
                        <h3 id="formTitle">Add New Firmware</h3>
                        <button class="modal-close" onclick="closeModal('firmwareFormModal')">&times;</button>
                    </div>
                    <form id="firmwareForm" onsubmit="submitFirmwareForm(event)">
                        <div class="form-group">
                            <label for="deviceTypeId">Device Type *</label>
                            <select id="deviceTypeId" required></select>
                        </div>
                        <div class="form-group">
                            <label for="firmwareVersion">Version *</label>
                            <input type="text" id="firmwareVersion" placeholder="e.g. v1.2.3" required>
                        </div>
                        <div class="form-group">
                            <label for="releaseDate">Release Date *</label>
                            <input type="date" id="releaseDate" required>
                        </div>
                        <div class="form-group">
                            <label for="firmwareNotes">Notes</label>
                            <textarea id="firmwareNotes" placeholder="Version notes, improvements, etc."></textarea>
                        </div>
                        <div class="form-actions">
                            <button type="button" class="btn-secondary" onclick="closeModal('firmwareFormModal')">Cancel</button>
                            <button type="submit" class="btn-primary">Save Firmware</button>
                        </div>
                    </form>
                </div>
            </div>
        `;
        
        await loadFirmware();
        await loadDeviceTypes();
        await countDevicesPerFirmware();
        
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

// Load all firmware
async function loadFirmware() {
    try {
        const response = await apiRequest('/firmware');
        allFirmware = response || [];
        filteredFirmware = [...allFirmware];
        renderTable();
    } catch (error) {
        showAlert('alerts-container', `Failed to load firmware: ${error.message}`, 'error');
    }
}

// Load device types for filter and form
async function loadDeviceTypes() {
    try {
        const response = await apiRequest('/firmware');
        const uniqueTypes = [...new Set(response?.map(f => ({ 
            id: f.deviceTypeId, 
            name: f.deviceTypeName 
        })) || [])];

        // Populate type filter
        const typeFilter = document.getElementById('typeFilter');
        uniqueTypes.forEach(type => {
            const option = document.createElement('option');
            option.value = type.id;
            option.textContent = type.name;
            typeFilter.appendChild(option);
        });

        // Populate device type select in form
        const deviceTypeSelect = document.getElementById('deviceTypeId');
        uniqueTypes.forEach(type => {
            const option = document.createElement('option');
            option.value = type.id;
            option.textContent = type.name;
            deviceTypeSelect.appendChild(option);
        });
    } catch (error) {
        console.error('Error loading device types:', error);
    }
}

// Count devices using each firmware version
async function countDevicesPerFirmware() {
    try {
        const response = await apiRequest('/device?pageSize=10000');
        const devices = response.data || [];

        allFirmware.forEach(firmware => {
            const count = devices.filter(d => d.FirmwareId === firmware.FirmwareId).length;
            deviceCountMap[firmware.FirmwareId] = count;
        });

        renderTable();
    } catch (error) {
        console.error('Error counting devices:', error);
    }
}

// Render table
function renderTable() {
    const tbody = document.getElementById('firmwareTableBody');
    
    if (filteredFirmware.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" class="empty-state"><div>No firmware found</div></td></tr>';
        return;
    }

    tbody.innerHTML = filteredFirmware.map(fw => `
        <tr>
            <td><span class="badge badge-primary">${escapeHtml(fw.deviceTypeName)}</span></td>
            <td><code>v${escapeHtml(fw.version)}</code></td>
            <td>${formatDate(fw.releaseDate)}</td>
            <td>
                <span class="device-count">
                     ${deviceCountMap[fw.FirmwareId] || 0}
                </span>
            </td>
            <td>${fw.notes ? escapeHtml(fw.notes) : '-'}</td>
            <td>
                <div class="device-actions">
                    <button class="btn-icon btn-edit" onclick="editFirmware(${fw.FirmwareId})" title="Edit">Edit</button>
                    <button class="btn-icon btn-delete" onclick="deleteFirmware(${fw.FirmwareId})" title="Delete">Delete</button>
                </div>
            </td>
        </tr>
    `).join('');
}

// Search functionality
function handleSearch() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    
    if (!searchTerm) {
        applyFilters();
        return;
    }

    filteredFirmware = allFirmware.filter(fw =>
        fw.version.toLowerCase().includes(searchTerm) ||
        fw.deviceTypeName.toLowerCase().includes(searchTerm) ||
        (fw.notes && fw.notes.toLowerCase().includes(searchTerm))
    );

    renderTable();
}

// Apply filters
function applyFilters() {
    const typeId = document.getElementById('typeFilter').value;
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();

    filteredFirmware = allFirmware.filter(fw => {
        // Type filter
        if (typeId && fw.deviceTypeId != typeId) return false;

        // Search filter
        if (searchTerm) {
            return fw.version.toLowerCase().includes(searchTerm) ||
                   fw.deviceTypeName.toLowerCase().includes(searchTerm) ||
                   (fw.notes && fw.notes.toLowerCase().includes(searchTerm));
        }

        return true;
    });

    renderTable();
}

// Reset filters
function resetFilters() {
    document.getElementById('searchInput').value = '';
    document.getElementById('typeFilter').value = '';
    filteredFirmware = [...allFirmware];
    renderTable();
}

// Open firmware modal for creation
function openFirmwareModal() {
    currentEditingFirmwareId = null;
    document.getElementById('formTitle').textContent = 'Add New Firmware';
    document.getElementById('firmwareForm').reset();
    openModal('firmwareFormModal');
}

// Edit firmware
async function editFirmware(FirmwareId) {
    try {
        const fw = allFirmware.find(f => f.FirmwareId === FirmwareId);
        if (!fw) return;

        currentEditingFirmwareId = FirmwareId;
        document.getElementById('formTitle').textContent = 'Edit Firmware';
        document.getElementById('deviceTypeId').value = fw.deviceTypeId;
        document.getElementById('firmwareVersion').value = fw.version;
        document.getElementById('releaseDate').value = fw.releaseDate?.split('T')[0] || '';
        document.getElementById('firmwareNotes').value = fw.notes || '';
        
        openModal('firmwareFormModal');
    } catch (error) {
        showAlert('alerts-container', `Failed to load firmware: ${error.message}`, 'error');
    }
}

// Submit firmware form
async function submitFirmwareForm(event) {
    event.preventDefault();

    try {
        const formData = {
            deviceTypeId: parseInt(document.getElementById('deviceTypeId').value),
            version: document.getElementById('firmwareVersion').value,
            releaseDate: document.getElementById('releaseDate').value,
            notes: document.getElementById('firmwareNotes').value || null
        };

        if (currentEditingFirmwareId) {
            // Update firmware
            await apiRequest(`/firmware/${currentEditingFirmwareId}`, {
                method: 'PUT',
                body: JSON.stringify(formData)
            });
            showAlert('alerts-container', 'Firmware updated successfully!', 'success');
        } else {
            // Create firmware
            await apiRequest('/firmware', {
                method: 'POST',
                body: JSON.stringify(formData)
            });
            showAlert('alerts-container', 'Firmware created successfully!', 'success');
        }

        closeModal('firmwareFormModal');
        currentEditingFirmwareId = null;
        await loadFirmware();
        await countDevicesPerFirmware();
    } catch (error) {
        showAlert('alerts-container', `Failed to save firmware: ${error.message}`, 'error');
    }
}

// Delete firmware
async function deleteFirmware(FirmwareId) {
    const fw = allFirmware.find(f => f.FirmwareId === FirmwareId);
    const deviceCount = deviceCountMap[FirmwareId] || 0;

    if (deviceCount > 0) {
        showAlert('alerts-container', `Cannot delete firmware in use by ${deviceCount} device(s)`, 'error');
        return;
    }

    if (!confirm('Are you sure you want to delete this firmware version?')) return;

    try {
        // Note: Backend may not have delete endpoint yet
        showAlert('alerts-container', 'Firmware deletion not yet implemented in backend', 'info');
    } catch (error) {
        showAlert('alerts-container', `Failed to delete firmware: ${error.message}`, 'error');
    }
}

// Modal management
function openModal(modalId) {
    document.getElementById(modalId).classList.add('active');
}

function closeModal(modalId) {
    document.getElementById(modalId).classList.remove('active');
    if (modalId === 'firmwareFormModal') {
        currentEditingFirmwareId = null;
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
    document.addEventListener('DOMContentLoaded', initFirmwarePage);
} else {
    initFirmwarePage();
}
