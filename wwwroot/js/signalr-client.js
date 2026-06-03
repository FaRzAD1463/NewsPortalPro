
const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/news')
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

// ── Breaking News ─────────────────────────────────────────
connection.on('BreakingNews', function (data) {
    // Update ticker
    const tickerItem = `<a href="${data.link}">${data.title}</a>`;
    const ticker = document.getElementById('ticker-content');
    if (ticker) {
        ticker.insertAdjacentHTML('afterbegin', tickerItem);
        document.getElementById('breaking-ticker')?.classList.remove('d-none');
    }

    // Toast notification
    toastr.warning(
        `<strong>ব্রেকিং:</strong> <a href="${data.link}" class="text-white">${data.title}</a>`,
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
});

// ── Admin Broadcast ───────────────────────────────────────
connection.on('ReceiveBroadcast', function (data) {
    toastr.success(data.message, 'সিস্টেম বিজ্ঞপ্তি', { timeOut: 6000 });
});

// ── Connect ───────────────────────────────────────────────
async function startSignalR() {
    try {
        await connection.start();
        console.log('SignalR connected');

        // Join current category if on category page
        const categorySlug = document.body.dataset.category;
        if (categorySlug) {
            await connection.invoke('JoinCategory', categorySlug);
        }
    } catch (err) {
        console.warn('SignalR connection failed:', err);
        setTimeout(startSignalR, 5000);
    }
}

connection.onreconnecting(() => {
    console.log('SignalR reconnecting...');
});

connection.onreconnected(() => {
    console.log('SignalR reconnected');
});

startSignalR();