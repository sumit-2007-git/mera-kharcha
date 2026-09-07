// Mera Kharcha - Core UI & Real-Time Engine
document.addEventListener('DOMContentLoaded', function () {
    // 1. Theme Management (Dark / Light Mode)
    const themeToggleBtn = document.getElementById('themeToggleBtn');
    const themeIcon = document.getElementById('themeIcon');
    const currentTheme = localStorage.getItem('mk_theme') || 'light';

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        localStorage.setItem('mk_theme', theme);
        if (themeIcon) {
            themeIcon.className = theme === 'dark' ? 'bi bi-sun-fill text-warning' : 'bi bi-moon-stars-fill text-secondary';
        }
    }

    applyTheme(currentTheme);

    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', function () {
            const active = document.documentElement.getAttribute('data-bs-theme');
            const next = active === 'dark' ? 'light' : 'dark';
            applyTheme(next);
        });
    }

    // 2. Mobile Sidebar Toggle
    const sidebarToggleBtn = document.getElementById('sidebarToggleBtn');
    const appSidebar = document.querySelector('.app-sidebar');
    if (sidebarToggleBtn && appSidebar) {
        sidebarToggleBtn.addEventListener('click', function () {
            appSidebar.classList.toggle('show');
        });

        // Close sidebar on clicking outside in mobile view
        document.addEventListener('click', function (e) {
            if (window.innerWidth < 992 &&
                !appSidebar.contains(e.target) &&
                !sidebarToggleBtn.contains(e.target) &&
                appSidebar.classList.contains('show')) {
                appSidebar.classList.remove('show');
            }
        });
    }

    // 3. Notification Dropdown Loader
    const notificationDropdown = document.getElementById('notificationDropdown');
    if (notificationDropdown) {
        loadNotifications();
        // Refresh every 30 seconds
        setInterval(loadNotifications, 30000);
    }

    // 4. Global Copy to Clipboard Handler
    document.querySelectorAll('[data-copy]').forEach(el => {
        el.addEventListener('click', function () {
            const text = this.getAttribute('data-copy');
            if (text && navigator.clipboard) {
                navigator.clipboard.writeText(text).then(() => {
                    showToast('Copied to clipboard!', 'success');
                });
            }
        });
    });

    // 5. SignalR Client Connection
    initializeSignalR();
});

// Real-Time SignalR Engine
let hubConnection = null;

function initializeSignalR() {
    if (typeof signalR === 'undefined') return;

    hubConnection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/group')
        .withAutomaticReconnect()
        .build();

    hubConnection.on('ReceiveNotification', function (data) {
        showToast(`🔔 ${data.title}: ${data.message}`, 'info');
        loadNotifications();
    });

    hubConnection.on('ExpenseAdded', function (data) {
        showToast(`💰 New Expense: "${data.title}" of ₹${Number(data.amount).toLocaleString('en-IN')} added by ${data.paidBy}!`, 'success');
    });

    hubConnection.on('SettlementUpdated', function (data) {
        showToast(`🤝 Settlement updated!`, 'info');
    });

    hubConnection.on('MemberJoined', function (data) {
        showToast(`👋 ${data.message}`, 'primary');
    });

    hubConnection.start()
        .then(() => {
            const currentGroupId = document.body.getAttribute('data-group-id');
            if (currentGroupId && hubConnection) {
                hubConnection.invoke('JoinGroup', currentGroupId);
            }
            const currentUserId = document.body.getAttribute('data-user-id');
            if (currentUserId && hubConnection) {
                hubConnection.invoke('JoinUserNotifications', currentUserId);
            }
        })
        .catch(err => console.log('SignalR connection pending/offline:', err));
}

// In-app Notification Loader
function loadNotifications() {
    const listEl = document.getElementById('notificationList');
    const badgeEl = document.getElementById('notificationBadge');
    if (!listEl) return;

    fetch('/Notifications/GetRecent')
        .then(res => res.ok ? res.json() : null)
        .then(data => {
            if (!data) return;

            if (badgeEl) {
                if (data.unreadCount > 0) {
                    badgeEl.innerText = data.unreadCount > 9 ? '9+' : data.unreadCount;
                    badgeEl.classList.remove('d-none');
                } else {
                    badgeEl.classList.add('d-none');
                }
            }

            if (!data.notifications || data.notifications.length === 0) {
                listEl.innerHTML = `
                    <div class="text-center py-4 text-muted">
                        <i class="bi bi-bell-slash fs-3 d-block mb-1"></i>
                        <small>No new notifications</small>
                    </div>`;
                return;
            }

            let html = '';
            data.notifications.forEach(n => {
                const unreadClass = !n.isRead ? 'bg-light-subtle fw-semibold' : '';
                html += `
                    <a href="${n.linkUrl || '#'}" class="dropdown-item py-2 px-3 border-bottom ${unreadClass}" onclick="markNotificationRead(${n.id})">
                        <div class="d-flex justify-content-between align-items-center mb-1">
                            <span class="badge bg-primary-subtle text-primary" style="font-size: 0.7rem;">${n.title}</span>
                            <small class="text-muted" style="font-size: 0.7rem;">${n.timeAgo}</small>
                        </div>
                        <div class="text-wrap text-secondary small" style="max-width: 280px;">${n.message}</div>
                    </a>`;
            });
            listEl.innerHTML = html;
        })
        .catch(() => {});
}

function markNotificationRead(id) {
    fetch('/Notifications/MarkAsRead?id=' + id, { method: 'POST' });
}

function markAllNotificationsRead() {
    fetch('/Notifications/MarkAllAsRead', { method: 'POST' })
        .then(() => loadNotifications());
}

// Universal Toast Notifier
function showToast(message, type = 'info') {
    let container = document.getElementById('toastContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        container.style.zIndex = '9999';
        document.body.appendChild(container);
    }

    const toastId = 'toast_' + Date.now();
    const bgClass = type === 'success' ? 'text-bg-success' :
                    type === 'danger' ? 'text-bg-danger' :
                    type === 'warning' ? 'text-bg-warning' : 'text-bg-primary';

    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center ${bgClass} border-0 shadow-lg" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body fw-medium">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>`;

    container.insertAdjacentHTML('beforeend', toastHtml);
    const toastEl = document.getElementById(toastId);
    if (toastEl && typeof bootstrap !== 'undefined') {
        const bsToast = new bootstrap.Toast(toastEl, { delay: 4000 });
        bsToast.show();
        toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
    }
}
