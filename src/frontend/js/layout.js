const NAV_ITEMS = [
    { page: 'dashboard', href: 'index.html', icon: '<img width="32" height="32" src="public/images/Home-icons8.png" alt="smart-home"/>', label: 'Home' },
    { page: 'devices', href: 'devices.html', icon: '<img width="32" height="32" src="public/images/Devices-icons8.png" alt="infrared-beam-sending"/>', label: 'Devices' },
    { page: 'groups', href: 'groups.html', icon: '<img width="32" height="32" src="public/images/Groups-icons8.png" alt="stacked-organizational-chart"/>', label: 'Groups' },
    { page: 'firmware', href: 'firmware.html', icon: '<img width="32" height="32" src="public/images/Firmware-icons8.png" alt="raspberry-pi-zero"/>', label: 'Firmware' }
];

function initLayout(activePage, pageTitle) {
    document.body.innerHTML = `
        <div class="sidebar-backdrop" id="sidebar-backdrop"></div>
        <div class="app-shell">
            <aside class="sidebar" id="sidebar">
                <div class="sidebar-brand">
                <img width="32" height="32" src="public/images/Radio-Waves-icons8.png" alt="radio-waves"/>
                <div class="brand-text">
                 <h1>DigiMatter</h1>
                 <p>IoT Asset Tracking</p>
                    </div>
                    </div>
                <nav class="sidebar-nav" id="sidebar-nav">
                    ${NAV_ITEMS.map(item => `
                        <a href="${item.href}" data-page="${item.page}" class="${item.page === activePage ? 'active' : ''}">
                            <span class="nav-icon">${item.icon}</span>
                            ${item.label}
                        </a>
                    `).join('')}
                </nav>
                <div class="sidebar-footer">
                    &copy; 2026 DigiMatter IoT Asset Tracking. All rights reserved.
                </div>
            </aside>
            <div class="app-main">
                <header class="topbar">
                    <div style="display:flex;align-items:center;gap:0.75rem;">
                        <button class="menu-toggle" id="menu-toggle" aria-label="Toggle menu">☰</button>
                        <h2 id="page-title">${pageTitle}</h2>
                    </div>
                    <div class="topbar-actions" id="topbar-actions"></div>
                </header>
                <div class="page-content" id="page-content"></div>
            </div>
        </div>
    `;

    document.getElementById('menu-toggle')?.addEventListener('click', toggleSidebar);
    document.getElementById('sidebar-backdrop')?.addEventListener('click', closeSidebar);
}

function toggleSidebar() {
    document.getElementById('sidebar')?.classList.toggle('open');
    document.getElementById('sidebar-backdrop')?.classList.toggle('open');
}

function closeSidebar() {
    document.getElementById('sidebar')?.classList.remove('open');
    document.getElementById('sidebar-backdrop')?.classList.remove('open');
}

function getPageContent() {
    return document.getElementById('page-content');
}

function setTopbarActions(html) {
    const el = document.getElementById('topbar-actions');
    if (el) el.innerHTML = html;
}
