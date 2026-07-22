// Global state
let currentView = 'table';
let allDevices = [];
let filteredDevices = [];
let selectedDevice = null;
let map = null;
let markers = new Map();
let currentEditingDeviceId = null;

// Initialize page
async function initDevicesPage() {
    try {
        initLayout('devices', 'Devices');
        const pageContent = document.getElementById('page-content');
        
        // Build page HTML
        pageContent.innerHTML = `
            <div id="alerts-container"></div>
            
            <div class="stats-grid">
                <div class="stat-card">
                    <div class="stat-label">Total Devices</div>
                    <div class="stat-value" id="stat-total">-</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Active Devices</div>
                    <div class="stat-value" id="stat-active">-</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Groups</div>
                    <div class="stat-value" id="stat-groups">-</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Device Types</div>
                    <div class="stat-value" id="stat-types">-</div>
                </div>
            </div>

            <div class="controls-bar">
                <div class="search-box">
                    <input 
                        type="text" 
                        id="searchInput" 
                        placeholder="Search by serial number, name, or coordinates..."
                    >
                    <button onclick="handleSearch()">Search</button>
                    <button onclick="resetFilters()" style="background: var(--text-muted);">Reset</button>
                </div>
                <div class="filter-group">
                    <select id="groupFilter" onchange="applyFilters()">
                        <option value="">All Groups</option>
                    </select>
                    <select id="typeFilter" onchange="applyFilters()">
                        <option value="">All Types</option>
                    </select>
                    <select id="statusFilter" onchange="applyFilters()">
                        <option value="">All Status</option>
                        <option value="active">Active</option>
                        <option value="inactive">Inactive</option>
                    </select>
                </div>
                <div class="filter-group">
                    <button class="btn-primary" onclick="openDeviceModal()">+ New Device</button>
                </div>
                <div class="view-toggle">
                    <button class="active" onclick="switchView('table')">Table View</button>
                    <button onclick="switchView('map')">🗺️ Map</button>
                </div>
            </div>

            <div id="mapView" style="display: none;">
                <div class="map-container">
                    <div id="map"></div>
                    <div id="map-info" style="margin-top: 1rem; padding: 1rem; background: var(--bg); border-radius: var(--radius); display: none;">
                        <h4 id="map-device-name"></h4>
                        <p><strong>Serial:</strong> <span id="map-device-serial"></span></p>
                        <p><strong>Type:</strong> <span id="map-device-type"></span></p>
                        <p><strong>Firmware:</strong> <span id="map-device-firmware"></span></p>
                        <p><strong>Group:</strong> <span id="map-device-group"></span></p>
                        <p><strong>Coordinates:</strong> <span id="map-device-coords"></span></p>
                        <button class="btn-primary" onclick="viewDeviceDetails()" style="margin-top: 1rem; width: 100%;">View Details</button>
                    </div>
                </div>
            </div>

            <div id="tableView">
                <div class="table-container">
                    <div class="table-scroll">
                        <table id="devicesTable">
                            <thead>
                                <tr>
                                    <th class="sortable" onclick="sortTable('name')">Name</th>
                                    <th class="sortable" onclick="sortTable('serialNumber')">Serial Number</th>
                                    <th>Type</th>
                                    <th>Firmware</th>
                                    <th>Group</th>
                                    <th>Location</th>
                                    <th class="sortable" onclick="sortTable('isActive')">Status</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody id="devicesTableBody">
                                <tr><td colspan="8" class="empty-state">Loading devices...</td></tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>

            <!-- Device Details Modal -->
            <div class="modal" id="deviceDetailsModal">
                <div class="modal-content">
                    <div class="modal-header">
                        <h3>Device Details</h3>
                        <button class="modal-close" onclick="closeModal('deviceDetailsModal')">&times;</button>
                    </div>
                    <div id="deviceDetailsContent"></div>
                </div>
            </div>

            <!-- Device Form Modal -->
            <div class="modal" id="deviceFormModal">
                <div class="modal-content">
                    <div class="modal-header">
                        <h3 id="formTitle">Add New Device</h3>
                        <button class="modal-close" onclick="closeModal('deviceFormModal')">&times;</button>
                    </div>
                    <form id="deviceForm" onsubmit="submitDeviceForm(event)">
                        <div class="form-group">
                            <label for="deviceName">Device Name *</label>
                            <input type="text" id="deviceName" required>
                        </div>
                        <div class="form-group">
                            <label for="serialNumber">Serial Number *</label>
                            <input type="text" id="serialNumber" required readonly>
                        </div>
                        <div class="form-group">
                            <label for="deviceType">Device Type *</label>
                            <select id="deviceType" required></select>
                        </div>
                        <div class="form-group">
                            <label for="firmwareId">Firmware *</label>
                            <select id="firmwareId" required></select>
                        </div>
                        <div class="form-group">
                            <label for="groupId">Group</label>
                            <select id="groupId"></select>
                        </div>
                        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1rem;">
                            <div class="form-group">
                                <label for="latitude">Latitude *</label>
                                <input type="number" id="latitude" step="0.0001" required>
                            </div>
                            <div class="form-group">
                                <label for="longitude">Longitude *</label>
                                <input type="number" id="longitude" step="0.0001" required>
                            </div>
                        </div>
                        <div class="form-group">
                            <label for="isActive">
                                <input type="checkbox" id="isActive" checked> Active
                            </label>
                        </div>
                        <div class="form-actions">
                            <button type="button" class="btn-secondary" onclick="closeModal('deviceFormModal')">Cancel</button>
                            <button type="submit" class="btn-primary">Save Device</button>
                        </div>
                    </form>
                </div>
            </div>
        `;
        
        // Load data
        await loadStatistics();
        await loadFilterOptions();
        await loadDevices();
        
        // Add search debouncing
        document.getElementById('searchInput').addEventListener('keyup', debounce(() => handleSearch(), 300));
        
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

// Load statistics
async function loadStatistics() {
    try {
        const [devices, groups] = await Promise.all([
            apiRequest('/device?page=1&pageSize=1000'),
            apiRequest('/group')
        ]);

        //device count
        const totalDevices = devices.totalRecords || devices.data?.length || 0;
        const activeDevices = devices.data?.filter(d => d.isActive).length || 0;

        document.getElementById('stat-total').textContent = totalDevices;
        document.getElementById('stat-active').textContent = activeDevices;
        document.getElementById('stat-groups').textContent = groups?.length || 0;
        
        const deviceTypes = new Set(devices.data?.map(d => d.deviceTypeName) || []);
        document.getElementById('stat-types').textContent = deviceTypes.size;
    } catch (error) {
        console.error('Error loading statistics:', error);
    }
}

// Load filter options
async function loadFilterOptions() {
    try {
        const [groups, firmware] = await Promise.all([
            apiRequest('/group'),
            apiRequest('/firmware')
        ]);

        // Populate groups filter
        const groupFilter = document.getElementById('groupFilter');
        groups?.forEach(group => {
            const option = document.createElement('option');
            option.value = group.groupId;
            option.textContent = group.name;
            groupFilter.appendChild(option);
        });

        // Populate group in form
        const groupSelect = document.getElementById('groupId');
        const emptyOption = document.createElement('option');
        emptyOption.value = '';
        emptyOption.textContent = 'No Group';
        groupSelect.appendChild(emptyOption);
        groups?.forEach(group => {
            const option = document.createElement('option');
            option.value = group.groupId;
            option.textContent = group.name;
            groupSelect.appendChild(option);
        });

        // Populate device types and firmware in form
        const firmware_map = firmware?.reduce((acc, f) => {
            if (!acc[f.deviceTypeId]) acc[f.deviceTypeId] = [];
            acc[f.deviceTypeId].push(f);
            return acc;
        }, {}) || {};

        const deviceTypeSelect = document.getElementById('deviceType');
        Object.keys(firmware_map).forEach(typeId => {
            // Get type name from first firmware
            const typeName = firmware_map[typeId][0]?.deviceTypeName;
            if (typeName) {
                const option = document.createElement('option');
                option.value = typeId;
                option.textContent = typeName;
                deviceTypeSelect.appendChild(option);
            }
        });

        // Handle device type change to update firmware options
        document.getElementById('deviceType').addEventListener('change', (e) => {
            updateFirmwareOptions(e.target.value);
        });

        // Populate firmware filter
        const typeFilter = document.getElementById('typeFilter');
        const deviceTypes = new Set(firmware?.map(f => f.deviceTypeName) || []);
        deviceTypes.forEach(type => {
            const option = document.createElement('option');
            option.value = type;
            option.textContent = type;
            typeFilter.appendChild(option);
        });
    } catch (error) {
        console.error('Error loading filter options:', error);
    }
}

// Update firmware options based on device type
function updateFirmwareOptions(deviceTypeId) {
    const firmwareSelect = document.getElementById('firmwareId');
    firmwareSelect.innerHTML = '<option value="">Select Firmware</option>';
    
    if (!deviceTypeId) return;

    apiRequest('/firmware').then(firmware => {
        firmware?.filter(f => f.deviceTypeId == deviceTypeId).forEach(f => {
            const option = document.createElement('option');
            option.value = f.firmwareId;
            option.textContent = `${f.deviceTypeName} v${f.version}`;
            firmwareSelect.appendChild(option);
        });
    });
}

// Load all devices
async function loadDevices() {
    try {
        showLoadingState();
        const response = await apiRequest('/device?page=1&pageSize=1000');
        allDevices = response.data || [];
        filteredDevices = [...allDevices];
        renderTableView();
    } catch (error) {
        showAlert('alerts-container', `Failed to load devices: ${error.message}`, 'error');
    }
}

// Show loading state
function showLoadingState() {
    const tbody = document.getElementById('devicesTableBody');
    if (tbody) {
        tbody.innerHTML = '<tr><td colspan="8" class="loading"><div class="spinner"></div></td></tr>';
    }
}

// Render table view
function renderTableView() {
    const tbody = document.getElementById('devicesTableBody');
    
    if (filteredDevices.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" class="empty-state"><div>No devices found</div></td></tr>';
        return;
    }

    tbody.innerHTML = filteredDevices.map(device => `
        <tr>
            <td>${escapeHtml(device.name)}</td>
            <td><code>${escapeHtml(device.serialNumber)}</code></td>
            <td>${escapeHtml(device.deviceTypeName)}</td>
            <td>${escapeHtml(device.firmwareVersion)}</td>
            <td>${device.groupName ? escapeHtml(device.groupName) : '-'}</td>
            <td>
                <span class="location-link" onclick="focusOnMap(${device.deviceId})">
                     ${formatCoord(device.latitude)}, ${formatCoord(device.longitude)}
                </span>
            </td>
            <td>
                <span class="status-badge ${device.isActive ? 'status-active' : 'status-inactive'}">
                    ${device.isActive ? 'Active' : 'Inactive'}
                </span>
            </td>
            <td>
                <div class="device-actions">
                    <button class="btn-icon btn-view" onclick="viewDeviceDetailsModal(${device.deviceId})" title="View">View</button>
                    <button class="btn-icon btn-edit" onclick="editDevice(${device.deviceId})" title="Edit">Edit</button>
                    <button class="btn-icon btn-delete" onclick="deleteDevice(${device.deviceId})" title="Delete">Delete</button>
                </div>
            </td>
        </tr>
    `).join('');
}

// Render map view
async function renderMapView() {
    if (!map) {
        initMap();
    }
    
    // Clear existing markers
    markers.forEach(marker => map.removeLayer(marker));
    markers.clear();

    // Add markers for filtered devices
    filteredDevices.forEach(device => {
        const marker = L.marker([device.latitude, device.longitude], {
            title: device.name,
            icon: L.icon({
                iconUrl: getMarkerColor(device.isActive),
                iconSize: [30, 40],
                iconAnchor: [15, 40],
                popupAnchor: [0, -40]
            })
        });

        marker.bindPopup(`
            <div style="font-size: 0.9rem;">
                <strong>${escapeHtml(device.name)}</strong><br/>
                <small>${escapeHtml(device.serialNumber)}</small><br/>
                <small><strong>${device.deviceTypeName}</strong></small><br/>
                <small>v${device.firmwareVersion}</small>
            </div>
        `);

        marker.addEventListener('click', () => {
            selectedDevice = device;
            showMapDeviceInfo(device);
        });

        marker.addTo(map);
        markers.set(device.deviceId, marker);
    });

    // Fit bounds if devices exist
    if (filteredDevices.length > 0) {
        const group = L.featureGroup(Array.from(markers.values()));
        map.fitBounds(group.getBounds(), { padding: [50, 50] });
    }
}

// Get marker color based on status
function getMarkerColor(isActive) {
    if (!isActive) {
        return 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDgiIGhlaWdodD0iNDgiIHZpZXdCb3g9IjAgMCA0OCA0OCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48Y2lyY2xlIGN4PSIyNCIgY3k9IjI0IiByPSIyMiIgZmlsbD0iI2VmNDQ0NCIgc3Ryb2tlPSJ3aGl0ZSIgc3Ryb2tlLXdpZHRoPSIyIi8+PHRleHQgeD0iMjQiIHk9IjMwIiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmb250LXNpemU9IjI0IiBmaWxsPSJ3aGl0ZSI+4pyTPC90ZXh0Pjwvc3ZnPg==';
    }
    return 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDgiIGhlaWdodD0iNDgiIHZpZXdCb3g9IjAgMCA0OCA0OCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48Y2lyY2xlIGN4PSIyNCIgY3k9IjI0IiByPSIyMiIgZmlsbD0iIzEwYjk4MSIgc3Ryb2tlPSJ3aGl0ZSIgc3Ryb2tlLXdpZHRoPSIyIi8+PHRleHQgeD0iMjQiIHk9IjMwIiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmb250LXNpemU9IjI0IiBmaWxsPSJ3aGl0ZSI+4pyTPC90ZXh0Pjwvc3ZnPg==';
}

// Initialize map
function initMap() {
    map = L.map('map').setView([20, 0], 2);
    
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors',
        maxZoom: 19
    }).addTo(map);
}

// Show device info on map
function showMapDeviceInfo(device) {
    const info = document.getElementById('map-info');
    document.getElementById('map-device-name').textContent = device.name;
    document.getElementById('map-device-serial').textContent = device.serialNumber;
    document.getElementById('map-device-type').textContent = device.deviceTypeName;
    document.getElementById('map-device-firmware').textContent = device.firmwareVersion;
    document.getElementById('map-device-group').textContent = device.groupName || '-';
    document.getElementById('map-device-coords').textContent = `${formatCoord(device.latitude)}, ${formatCoord(device.longitude)}`;
    info.style.display = 'block';
}

// Focus on device in map
function focusOnMap(deviceId) {
    switchView('map');
    setTimeout(() => {
        const device = allDevices.find(d => d.deviceId === deviceId);
        if (device) {
            selectedDevice = device;
            showMapDeviceInfo(device);
            if (map) {
                map.setView([device.latitude, device.longitude], 15);
                const marker = markers.get(deviceId);
                if (marker) marker.openPopup();
            }
        }
    }, 100);
}

// Switch between views
function switchView(view) {
    currentView = view;
    document.querySelectorAll('.view-toggle button').forEach(btn => btn.classList.remove('active'));
    event?.target?.classList.add('active');

    const tableView = document.getElementById('tableView');
    const mapView = document.getElementById('mapView');

    if (view === 'map') {
        tableView.style.display = 'none';
        mapView.style.display = 'block';
        renderMapView();
    } else {
        tableView.style.display = 'block';
        mapView.style.display = 'none';
        renderTableView();
    }
}

// Search functionality
function handleSearch() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    
    if (!searchTerm) {
        applyFilters();
        return;
    }

    filteredDevices = allDevices.filter(device =>
        device.serialNumber.toLowerCase().includes(searchTerm) ||
        device.name.toLowerCase().includes(searchTerm) ||
        device.deviceTypeName.toLowerCase().includes(searchTerm) ||
        device.firmwareVersion.toLowerCase().includes(searchTerm) ||
        (device.groupName && device.groupName.toLowerCase().includes(searchTerm)) ||
        device.latitude.toString().includes(searchTerm) ||
        device.longitude.toString().includes(searchTerm)
    );

    if (currentView === 'table') {
        renderTableView();
    } else {
        renderMapView();
    }
}

// Apply filters
function applyFilters() {
    const groupId = document.getElementById('groupFilter').value;
    const deviceType = document.getElementById('typeFilter').value;
    const status = document.getElementById('statusFilter').value;
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();

    filteredDevices = allDevices.filter(device => {
        // Group filter
        if (groupId && device.groupId != groupId) return false;

        // Device type filter
        if (deviceType && device.deviceTypeName !== deviceType) return false;

        // Status filter
        if (status === 'active' && !device.isActive) return false;
        if (status === 'inactive' && device.isActive) return false;

        // Search filter
        if (searchTerm) {
            return device.serialNumber.toLowerCase().includes(searchTerm) ||
                   device.name.toLowerCase().includes(searchTerm) ||
                   device.deviceTypeName.toLowerCase().includes(searchTerm) ||
                   device.firmwareVersion.toLowerCase().includes(searchTerm) ||
                   (device.groupName && device.groupName.toLowerCase().includes(searchTerm));
        }

        return true;
    });

    if (currentView === 'table') {
        renderTableView();
    } else {
        renderMapView();
    }
}

// Reset filters
function resetFilters() {
    document.getElementById('searchInput').value = '';
    document.getElementById('groupFilter').value = '';
    document.getElementById('typeFilter').value = '';
    document.getElementById('statusFilter').value = '';
    filteredDevices = [...allDevices];
    
    if (currentView === 'table') {
        renderTableView();
    } else {
        renderMapView();
    }
}

// Sort table
function sortTable(column) {
    const isAsc = allDevices[0]?.[column] !== undefined;
    filteredDevices.sort((a, b) => {
        let aVal = a[column];
        let bVal = b[column];
        
        if (typeof aVal === 'string') {
            return isAsc ? aVal.localeCompare(bVal) : bVal.localeCompare(aVal);
        }
        return isAsc ? aVal - bVal : bVal - aVal;
    });
    
    renderTableView();
}

// View device details
async function viewDeviceDetailsModal(deviceId) {
    try {
        const device = await apiRequest(`/device/${deviceId}`);
        viewDeviceDetails(device);
    } catch (error) {
        showAlert('alerts-container', `Failed to load device details: ${error.message}`, 'error');
    }
}

// Display device details
function viewDeviceDetails(device = selectedDevice) {
    if (!device) return;
    
    const content = document.getElementById('deviceDetailsContent');
    content.innerHTML = `
        <div style="margin-bottom: 1.5rem;">
            <h4 style="margin-bottom: 1rem;">Basic Information</h4>
            <dl style="display: grid; grid-template-columns: 1fr 1fr; gap: 1rem;">
                <div>
                    <dt style="color: var(--text-muted); font-size: 0.9rem;">Name</dt>
                    <dd style="font-weight: 500;">${escapeHtml(device.name)}</dd>
                </div>
                <div>
                    <dt style="color: var(--text-muted); font-size: 0.9rem;">Serial Number</dt>
                    <dd><code>${escapeHtml(device.serialNumber)}</code></dd>
                </div>
                <div>
                    <dt style="color: var(--text-muted); font-size: 0.9rem;">Device Type</dt>
                    <dd style="font-weight: 500;">${escapeHtml(device.deviceTypeName)}</dd>
                </div>
                <div>
                    <dt style="color: var(--text-muted); font-size: 0.9rem;">Firmware</dt>
                    <dd style="font-weight: 500;">v${escapeHtml(device.firmwareVersion)}</dd>
                </div>
                <div>
                    <dt style="color: var(--text-muted); font-size: 0.9rem;">Group</dt>
                    <dd style="font-weight: 500;">${device.groupName || '-'}</dd>
                </div>
                <div>
                    <dt style="color: var(--text-muted); font-size: 0.9rem;">Status</dt>
                    <dd>
                        <span class="status-badge ${device.isActive ? 'status-active' : 'status-inactive'}">
                            ${device.isActive ? 'Active' : 'Inactive'}
                        </span>
                    </dd>
                </div>
            </dl>
        </div>

        <div style="margin-bottom: 1.5rem;">
            <h4 style="margin-bottom: 1rem;">Location</h4>
            <dl style="display: grid; grid-template-columns: 1fr 1fr; gap: 1rem;">
                <div>
                    <dt style="color: var(--text-muted); font-size: 0.9rem;">Latitude</dt>
                    <dd style="font-weight: 500;">${formatCoord(device.latitude)}</dd>
                </div>
                <div>
                    <dt style="color: var(--text-muted); font-size: 0.9rem;">Longitude</dt>
                    <dd style="font-weight: 500;">${formatCoord(device.longitude)}</dd>
                </div>
            </dl>
            <button class="btn-primary" onclick="focusOnMap(${device.deviceId})" style="margin-top: 1rem; width: 100%;">View on Map</button>
        </div>

        <div>
            <h4 style="margin-bottom: 1rem;">Metadata</h4>
            <dl style="display: grid; grid-template-columns: 1fr 1fr; gap: 1rem;">
                <div>
                    <dt style="color: var(--text-muted); font-size: 0.9rem;">Created</dt>
                    <dd style="font-weight: 500;">${formatDate(device.createdDate)}</dd>
                </div>
                <div>
                    <dt style="color: var(--text-muted); font-size: 0.9rem;">Last Modified</dt>
                    <dd style="font-weight: 500;">${formatDate(device.lastModified) || '-'}</dd>
                </div>
            </dl>
        </div>

        <div style="margin-top: 2rem; display: flex; gap: 1rem;">
            <button class="btn-primary" onclick="editDevice(${device.deviceId})" style="flex: 1;">Edit</button>
            <button class="btn-secondary" onclick="closeModal('deviceDetailsModal')" style="flex: 1;">Close</button>
        </div>
    `;
    
    openModal('deviceDetailsModal');
}

// Open device modal for creation
function openDeviceModal() {
    currentEditingDeviceId = null;
    document.getElementById('formTitle').textContent = 'Add New Device';
    document.getElementById('deviceForm').reset();
    document.getElementById('deviceType').dispatchEvent(new Event('change'));
    openModal('deviceFormModal');
}

// Edit device
async function editDevice(deviceId) {
    try {
        const device = await apiRequest(`/device/${deviceId}`);
        currentEditingDeviceId = deviceId;
        document.getElementById('formTitle').textContent = 'Edit Device';
        document.getElementById('deviceName').value = device.name;
        document.getElementById('serialNumber').value = device.serialNumber;
        document.getElementById('serialNumber').disabled = true; // Serial number shouldn't change
        document.getElementById('latitude').value = device.latitude;
        document.getElementById('longitude').value = device.longitude;
        document.getElementById('isActive').checked = device.isActive;
        
        // Set device type and firmware
        document.getElementById('deviceType').value = ''; // Will be set via firmware
        document.getElementById('firmwareId').value = ''; // Will be fetched
        document.getElementById('groupId').value = device.groupId || '';
       // document.getElementById('serialNumber').disabled = false; // Serial number should be editable only when creating a new device  
        
        // Populate firmware with the selected one
        const firmware = await apiRequest('/firmware');
        const deviceFirmware = firmware.find(f => f.firmwareId == device.firmwareId);
        if (deviceFirmware) {
            document.getElementById('deviceType').value = deviceFirmware.deviceTypeId;
            document.getElementById('deviceType').dispatchEvent(new Event('change'));
            setTimeout(() => {
                document.getElementById('firmwareId').value = device.firmwareId;
            }, 100);
        }
        
        openModal('deviceFormModal');
    } catch (error) {
        showAlert('alerts-container', `Failed to load device: ${error.message}`, 'error');
    }
}

// Submit device form
async function submitDeviceForm(event) {
    event.preventDefault();

    try {
        const formData = {
            name: document.getElementById('deviceName').value,
            serialNumber: document.getElementById('serialNumber').value,
            latitude: parseFloat(document.getElementById('latitude').value),
            longitude: parseFloat(document.getElementById('longitude').value),
            isActive: document.getElementById('isActive').checked,
            firmwareId: parseInt(document.getElementById('firmwareId').value),
            groupId: document.getElementById('groupId').value ? parseInt(document.getElementById('groupId').value) : null
        };

        if (currentEditingDeviceId) {
            // Update device (exclude serialNumber)
            const updateData = { ...formData };
            delete updateData.serialNumber;
            await apiRequest(`/device/${currentEditingDeviceId}`, {
                method: 'PUT',
                body: JSON.stringify(updateData)
            });
            showAlert('alerts-container', 'Device updated successfully!', 'success');
        } else {
            // Create device
            await apiRequest('/device', {
                method: 'POST',
                body: JSON.stringify(formData)
            });
            showAlert('alerts-container', 'Device created successfully!', 'success');
        }

        closeModal('deviceFormModal');
        currentEditingDeviceId = null;
        await loadDevices();
    } catch (error) {
        showAlert('alerts-container', `Failed to save device: ${error.message}`, 'error');
    }
}

// Delete device
async function deleteDevice(deviceId) {
    if (!confirm('Are you sure you want to delete this device?')) return;

    try {
        // Note: device deletion endpoint
        await apiRequest(`/device/${deviceId}`,{method: 'DELETE'});
        showAlert('alerts-container','Device deleted successfully!','success');
        await loadDevices();
        await loadStatistics();
    } catch (error) {
        showAlert('alerts-container', `Failed to delete device: ${error.message}`, 'error');
    }
}

// Modal management
function openModal(modalId) {
    document.getElementById(modalId).classList.add('active');
}

function closeModal(modalId) {
    document.getElementById(modalId).classList.remove('active');
    if (modalId === 'deviceFormModal') {
        currentEditingDeviceId = null;
        document.getElementById('serialNumber').disabled = false;
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
    document.addEventListener('DOMContentLoaded', initDevicesPage);
} else {
    initDevicesPage();
}
