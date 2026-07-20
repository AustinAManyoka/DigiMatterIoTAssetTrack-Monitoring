const API_BASE = '/api';

async function apiRequest(endpoint, options = {}) {
    const response = await fetch(`${API_BASE}${endpoint}`, {
        headers: {
            'Content-Type': 'application/json',
            ...options.headers
        },
        ...options
    });

    if (response.status === 204) return null;

    const data = await response.json().catch(() => null);

    if (!response.ok) {
        const message = data?.message || data?.title || `Request failed (${response.status})`;
        throw new Error(message);
    }

    return data;
}

function showAlert(containerId, message, type = 'error') {
    const container = document.getElementById(containerId);
    if (!container) return;

    container.innerHTML = `<div class="alert alert-${type}">${escapeHtml(message)}</div>`;
    setTimeout(() => { container.innerHTML = ''; }, 5000);
}

function escapeHtml(text) {
    if (text == null) return '';
    const div = document.createElement('div');
    div.textContent = String(text);
    return div.innerHTML;
}

function formatDate(dateString) {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString(undefined, {
        year: 'numeric', month: 'short', day: 'numeric'
    });
}

function formatCoord(value) {
    return Number(value).toFixed(4);
}

function debounce(fn, delay = 300) {
    let timer;
    return (...args) => {
        clearTimeout(timer);
        timer = setTimeout(() => fn(...args), delay);
    };
}

function buildQueryString(params) {
    const query = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
        if (value !== null && value !== undefined && value !== '') {
            query.set(key, value);
        }
    });
    const str = query.toString();
    return str ? `?${str}` : '';
}

function openModal(modalId) {
    document.getElementById(modalId)?.classList.add('active');
}

function closeModal(modalId) {
    document.getElementById(modalId)?.classList.remove('active');
}

document.addEventListener('click', (e) => {
    if (e.target.classList.contains('modal-overlay')) {
        e.target.classList.remove('active');
    }
});

const DEVICE_TYPE_COLORS = {
    'Griffin Air': '#2563eb',
    'Yabby3 LoRaWAN': '#10b981',
    'Barra Edge': '#f59e0b'
};

function getDeviceTypeColor(typeName) {
    return DEVICE_TYPE_COLORS[typeName] || '#64748b';
}

function createMarkerIcon(color, isActive = true) {
    const opacity = isActive ? 1 : 0.5;
    return L.divIcon({
        className: 'custom-marker',
        html: `<div style="
            width:28px;height:28px;
            background:${color};
            border:3px solid white;
            border-radius:50% 50% 50% 0;
            transform:rotate(-45deg);
            box-shadow:0 2px 6px rgba(0,0,0,0.3);
            opacity:${opacity};
        "></div>`,
        iconSize: [28, 28],
        iconAnchor: [14, 28],
        popupAnchor: [0, -28]
    });
}

function buildDevicePopup(device) {
    return `
        <div class="device-popup">
            <h4>${escapeHtml(device.name)}</h4>
            <p><strong>Serial:</strong> ${escapeHtml(device.serialNumber)}</p>
            <p><strong>Type:</strong> ${escapeHtml(device.deviceTypeName)}</p>
            <p><strong>Group:</strong> ${escapeHtml(device.groupName || 'Unassigned')}</p>
            <p><strong>Firmware:</strong> ${escapeHtml(device.firmwareVersion)}</p>
            <p><strong>Status:</strong> ${device.isActive ? 'Active' : 'Inactive'}</p>
        </div>
    `;
}
