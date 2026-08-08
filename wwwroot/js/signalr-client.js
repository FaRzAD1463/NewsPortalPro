const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/news')
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

// ── Breaking News ─────────────────────────────────────────
connection.on('BreakingNews', function (data) {
    // Update ticker
    const tickerItem = `<a href="${escapeHtml(data.link)}">${escapeHtml(data.title)}</a>`;
    const ticker = document.getElementById('ticker-content');
    if (ticker) {
        ticker.insertAdjacentHTML('afterbegin', tickerItem);
        document.getElementById('breaking-ticker')?.classList.remove('d-none');
    }

    // Toast notification
    toastr.warning(
        `<strong>ব্রেকিং:</strong> <a href="${escapeHtml(data.link)}" class="text-white">${escapeHtml(data.title)}</a>`,
        'ব্রেকিং নিউজ',
        { timeOut: 8000, extendedTimeOut: 3000 }
    );
});

// ── Personal Notification ─────────────────────────────────
connection.on('ReceiveNotification', function (data) {
    const count = parseInt($('#notif-count').text() || '0') + 1;
    $('#notif-count').text(count).removeClass('d-none');

    toastr.info(
        data.message || '',
        data.title,
        { timeOut: 5000 }
    );

    // Keep the dropdown list itself in sync too, not just the badge
    // count — otherwise opening the bell right after a push shows a
    // stale list until the 5-minute fallback poll in site.js catches up.

    if (typeof window.loadNotifications === 'function') {
        window.loadNotifications();
    }
});

// ── Admin Broadcast ───────────────────────────────────────

connection.on('ReceiveBroadcast', function (data) {
    toastr.success(escapeHtml(data.message), 'সিস্টেম বিজ্ঞপ্তি', { timeOut: 6000 });
});

// ── Connect (with capped, jittered backoff for reconnect storms) ──

let startAttempts = 0;

async function startSignalR() {
    try {
        await connection.start();
        console.log('SignalR connected');
        startAttempts = 0;

        // Join current category if on category page

        const categorySlug = document.body.dataset.category;
        if (categorySlug) {
            await connection.invoke('JoinCategory', categorySlug);
        }
    } catch (err) {
        console.warn('SignalR connection failed:', err);
        startAttempts++;
        // Exponential backoff capped at 60s, with jitter so a large
        // number of clients reconnecting after a server restart don't
        // all retry in the same instant and hammer it at once.

        const base = Math.min(60000, 5000 * Math.pow(2, startAttempts - 1));
        const jitter = Math.random() * 1000;
        setTimeout(startSignalR, base + jitter);
    }
}

connection.onreconnecting(() => {
    console.log('SignalR reconnecting...');
});

connection.onreconnected(() => {
    console.log('SignalR reconnected');

    // Re-join the category group — group membership doesn't survive
    // a reconnect with a new connection ID, so this was silently lost
    // before: users would stop receiving category-scoped pushes after
    // any network blip until a full page reload.

    const categorySlug = document.body.dataset.category;
    if (categorySlug) {
        connection.invoke('JoinCategory', categorySlug).catch(function () { });
    }
    // Also refresh notifications in case anything was missed while
    // disconnected — SignalR pushes during a gap are simply lost.

    if (typeof window.loadNotifications === 'function') {
        window.loadNotifications();
    }
});

// Small helper so server-supplied title/message/link strings from the
// hub can't inject markup into toastr/ticker HTML.
function escapeHtml(str) {
    if (str == null) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

startSignalR();