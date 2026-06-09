/**
 * notification.js
 * Hệ thống thông báo kiểu Momo – biến động số dư & cảnh báo chi tiêu
 */

(function () {
    'use strict';

    // ─── Refs ──────────────────────────────────────────────────────────
    const btn       = document.getElementById('notifBtn');
    const badge     = document.getElementById('notifBadge');
    const panel     = document.getElementById('notifPanel');
    const list      = document.getElementById('notifList');
    const markAllBtn = document.getElementById('notifMarkAll');
    const cntSpan   = document.getElementById('notifCnt');
    const toastContainer = _ensureToastContainer();

    if (!btn || !panel) return; // layout không có chuông → thoát

    // ─── State ─────────────────────────────────────────────────────────
    let lastSeenTs = parseInt(localStorage.getItem('notif_last_ts') || '0', 10);
    let pollTimer  = null;

    // ─── Helpers ───────────────────────────────────────────────────────
    function _ensureToastContainer() {
        let c = document.getElementById('notifToastContainer');
        if (!c) {
            c = document.createElement('div');
            c.id = 'notifToastContainer';
            c.className = 'notif-toast-container';
            document.body.appendChild(c);
        }
        return c;
    }

    function _timeAgo(ts) {
        const diff = Math.floor(Date.now() / 1000) - ts;
        if (diff < 60)   return 'Vừa xong';
        if (diff < 3600) return Math.floor(diff / 60) + ' phút trước';
        if (diff < 86400) return Math.floor(diff / 3600) + ' giờ trước';
        return Math.floor(diff / 86400) + ' ngày trước';
    }

    function _iconClass(item) {
        if (item.loai === 'GiaoDich') {
            return item.bieuTuong === '💰' ? 'type-GiaoDich-thu' : 'type-GiaoDich-chi';
        }
        return 'type-' + item.loai;
    }

    function _toastClass(loai) {
        if (loai === 'SapHetTien') return 'warning';
        if (loai === 'ChiTieuLon') return 'caution';
        return '';
    }

    // ─── Render panel list ─────────────────────────────────────────────
    function _renderList(items) {
        list.innerHTML = '';
        if (!items || items.length === 0) {
            list.innerHTML = `
                <div class="notif-empty">
                    <i class="bi bi-bell-slash"></i>
                    <span>Chưa có thông báo nào</span>
                </div>`;
            return;
        }

        items.forEach(item => {
            const el = document.createElement('div');
            el.className = 'notif-item' + (item.daDoc ? '' : ' unread');
            el.dataset.id = item.id;
            el.innerHTML = `
                <div class="notif-icon ${_iconClass(item)}">${item.bieuTuong || '🔔'}</div>
                <div class="notif-body">
                    <div class="notif-title">${_esc(item.tieuDe)}</div>
                    <div class="notif-content">${_esc(item.noiDung)}</div>
                    <div class="notif-time">${_timeAgo(item.ngayTaoTs)}</div>
                </div>`;
            el.addEventListener('click', () => _markOne(item.id, el));
            list.appendChild(el);
        });
    }

    function _esc(str) {
        return (str || '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
    }

    // ─── Badge update ──────────────────────────────────────────────────
    function _updateBadge(count) {
        if (count > 0) {
            badge.textContent = count > 99 ? '99+' : count;
            badge.classList.remove('hidden');
        } else {
            badge.classList.add('hidden');
        }
        if (cntSpan) cntSpan.textContent = count > 0 ? count : '';
    }

    // ─── Fetch count (polling every 30s) ──────────────────────────────
    async function _fetchCount() {
        try {
            const r = await fetch('/Notification/GetCount');
            if (!r.ok) return;
            const d = await r.json();
            _updateBadge(d.count || 0);
        } catch (_) { /* offline – ignore */ }
    }

    // ─── Fetch & render full list ──────────────────────────────────────
    async function _fetchList() {
        list.innerHTML = `
            <div class="notif-empty">
                <i class="bi bi-arrow-clockwise" style="animation:spin 1s linear infinite"></i>
                <span>Đang tải...</span>
            </div>`;
        try {
            const r = await fetch('/Notification/GetList');
            if (!r.ok) throw new Error();
            const d = await r.json();
            _renderList(d.items || []);

            // Show toasts for NEW items since last open
            const newItems = (d.items || []).filter(i => i.ngayTaoTs > lastSeenTs);
            if (newItems.length > 0) {
                // Only toast the newest 3 to avoid flooding
                newItems.slice(0, 3).forEach(i => _showToast(i));
                lastSeenTs = newItems[0].ngayTaoTs;
                localStorage.setItem('notif_last_ts', lastSeenTs);
            }
        } catch (_) {
            list.innerHTML = '<div class="notif-empty"><span>Không thể tải thông báo</span></div>';
        }
    }

    // ─── Mark single ──────────────────────────────────────────────────
    async function _markOne(id, el) {
        if (el.classList.contains('unread')) {
            el.classList.remove('unread');
            try { await fetch('/Notification/MarkRead/' + id, { method: 'POST' }); } catch (_) {}
            const cur = parseInt(badge.textContent, 10) || 0;
            _updateBadge(Math.max(0, cur - 1));
        }
    }

    // ─── Mark all ─────────────────────────────────────────────────────
    markAllBtn && markAllBtn.addEventListener('click', async (e) => {
        e.stopPropagation();
        try { await fetch('/Notification/MarkAllRead', { method: 'POST' }); } catch (_) {}
        list.querySelectorAll('.notif-item.unread').forEach(el => el.classList.remove('unread'));
        _updateBadge(0);
    });

    // ─── Toggle panel ─────────────────────────────────────────────────
    btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const isOpen = panel.classList.toggle('open');
        if (isOpen) _fetchList();
    });

    document.addEventListener('click', (e) => {
        if (!panel.contains(e.target) && e.target !== btn) {
            panel.classList.remove('open');
        }
    });

    // ─── Toast popup ──────────────────────────────────────────────────
    function _showToast(item) {
        const cls = _toastClass(item.loai);
        const t = document.createElement('div');
        t.className = 'notif-toast' + (cls ? ' ' + cls : '');
        t.innerHTML = `
            <div class="notif-toast-icon">${item.bieuTuong || '🔔'}</div>
            <div class="notif-toast-body">
                <div class="notif-toast-title">${_esc(item.tieuDe)}</div>
                <div class="notif-toast-msg">${_esc(item.noiDung)}</div>
            </div>
            <button class="notif-toast-close" aria-label="Đóng">×</button>`;
        t.querySelector('.notif-toast-close').addEventListener('click', () => _dismissToast(t));
        toastContainer.appendChild(t);
        setTimeout(() => _dismissToast(t), 5000);
    }

    function _dismissToast(el) {
        if (!el.parentNode) return;
        el.classList.add('fade-out');
        setTimeout(() => el.remove(), 300);
    }

    // ─── Auto-show toasts for brand-new notifications ─────────────────
    // Runs once on page load to surface latest unread items as toasts
    async function _initToasts() {
        try {
            const r = await fetch('/Notification/GetList');
            if (!r.ok) return;
            const d = await r.json();
            const items = d.items || [];
            const fresh = items.filter(i => !i.daDoc && i.ngayTaoTs > lastSeenTs);
            // Show max 3 toasts on load (most recent first)
            fresh.slice(0, 3).forEach(i => _showToast(i));
            if (fresh.length > 0) {
                lastSeenTs = fresh[0].ngayTaoTs;
                localStorage.setItem('notif_last_ts', lastSeenTs);
            }
            _updateBadge(items.filter(i => !i.daDoc).length);
        } catch (_) {}
    }

    // ─── Spin keyframes (for loading icon) ────────────────────────────
    const styleEl = document.createElement('style');
    styleEl.textContent = '@keyframes spin{to{transform:rotate(360deg)}}';
    document.head.appendChild(styleEl);

    // ─── Boot ─────────────────────────────────────────────────────────
    _fetchCount();
    _initToasts();
    pollTimer = setInterval(_fetchCount, 30_000); // poll every 30s

})();
