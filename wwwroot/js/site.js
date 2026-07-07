(function ($) {
    'use strict';

    // ── AOS Animations ───────────────────────────────────
    AOS.init({ duration: 600, once: true, offset: 60 });

    // ── Current Date (Bengali) ───────────────────────────
    const days = ['রবিবার', 'সোমবার', 'মঙ্গলবার', 'বুধবার',
        'বৃহস্পতিবার', 'শুক্রবার', 'শনিবার'];
    const months = ['জানুয়ারি', 'ফেব্রুয়ারি', 'মার্চ', 'এপ্রিল', 'মে', 'জুন',
        'জুলাই', 'আগস্ট', 'সেপ্টেম্বর', 'অক্টোবর', 'নভেম্বর', 'ডিসেম্বর'];
    const now = new Date();
    const dateStr = days[now.getDay()] + ', ' +
        now.getDate() + ' ' +
        months[now.getMonth()] + ' ' +
        now.getFullYear();
    $('#current-date-header').text(dateStr);

    // ── Load mega menu news on hover ─────────────────────
    $(document).on('mouseenter', '.dropdown-hover', function () {
        const link = $(this).find('.nav-link-custom').first().attr('href') || '';
        if (!link || link === '#') return;
        const slug = link.replace('/category/', '').replace(/\/$/, '');
        if (!slug) return;
        const container = $('#mega-news-' + slug);
        if (!container.length || container.data('loaded') === true) return;
        container.data('loaded', true);

        fetch('/api/news?categorySlug=' + encodeURIComponent(slug) +
            '&pageSize=4&page=1')
            .then(function (r) {
                if (!r.ok) throw new Error();
                return r.json();
            })
            .then(function (data) {
                if (!data.items || !data.items.length) {
                    container.html(
                        '<p class="text-muted small p-2 mb-0">' +
                        'কোনো সংবাদ নেই</p>');
                    return;
                }
                var html = '';
                data.items.forEach(function (n) {
                    html +=
                        '<a href="/news/' + n.slug + '" ' +
                        'class="mega-news-item">' +
                        '<img src="' +
                        (n.featuredImage ||
                            '/images/placeholder.jpg') + '" ' +
                        'alt="' + n.title + '" loading="lazy" ' +
                        'onerror="this.src=' +
                        '\'/images/placeholder.jpg\'" />' +
                        '<span class="mega-news-item-title">' +
                        n.title +
                        '</span>' +
                        '</a>';
                });
                container.html(html);
            })
            .catch(function () {
                container.data('loaded', false);
                container.html(
                    '<p class="text-muted small p-2 mb-0">' +
                    'লোড হয়নি</p>');
            });
    });

    // ── Category search in All dropdown ──────────────────
    window.filterCategories = function (query) {
        const q = query.toLowerCase().trim();
        document.querySelectorAll('.all-cat-item').forEach(function (el) {
            const name = el.dataset.name || '';
            el.classList.toggle('d-none',
                q !== '' && !name.includes(q));
        });
    };

    // ── Dark Mode ────────────────────────────────────────
    const saved = localStorage.getItem('theme') || 'light';
    setTheme(saved);

    $('#theme-toggle').on('click', function () {
        const current = document.documentElement
            .getAttribute('data-theme');
        setTheme(current === 'dark' ? 'light' : 'dark');
    });

    function setTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
        $('#theme-toggle').html(theme === 'dark'
            ? '<i class="bi bi-sun-fill"></i>'
            : '<i class="bi bi-moon-fill"></i>');
    }

    // ── Live Search ──────────────────────────────────────
    let searchTimer;
    $('#live-search').on('input', function () {
        clearTimeout(searchTimer);
        const q = $(this).val().trim();
        if (!q) {
            $('#search-suggestions').empty().hide();
            return;
        }

        searchTimer = setTimeout(async function () {
            try {
                const res = await fetch(
                    '/api/search/suggest?q=' + encodeURIComponent(q));
                const data = await res.json();
                const box = $('#search-suggestions');
                box.empty();
                if (data.length) {
                    data.forEach(function (s) {
                        box.append(
                            '<a href="/Search?q=' +
                            encodeURIComponent(s) + '">' + s + '</a>');
                    });
                    box.show();
                } else {
                    box.hide();
                }
            } catch { }
        }, 300);
    });

    $(document).on('click', function (e) {
        if (!$(e.target).closest('.search-box-nav').length)
            $('#search-suggestions').hide();
    });

    // ── Load Breaking News Ticker ────────────────────────
    loadBreakingNews();

    async function loadBreakingNews() {
        try {
            const res = await fetch('/api/news/breaking?count=8');
            const data = await res.json();
            if (data && data.length) {
                // Duplicate items for seamless infinite scroll
                const links = data.map(function (n) {
                    return '<a href="/news/' + n.slug + '">' +
                        n.title + '</a>';
                }).join('');

                const ticker = document.getElementById('ticker-content');
                if (ticker) {
                    ticker.innerHTML = links + links; // duplicate for loop
                }

                const bar = document.getElementById('breaking-ticker');
                if (bar) bar.classList.remove('d-none');
            }
        } catch { }
    }

    // ── Notifications ────────────────────────────────────
    loadNotifications();
    setInterval(loadNotifications, 60000);

    async function loadNotifications() {
        if (!$('#notif-bell').length) return;
        try {
            const res = await fetch('/api/notifications?count=10');
            if (!res.ok) return;
            const data = await res.json();
            const unread = data.filter(function (n) {
                return !n.isRead;
            }).length;

            if (unread > 0)
                $('#notif-count').text(unread).removeClass('d-none');
            else
                $('#notif-count').addClass('d-none');

            const list = $('#notif-list');
            list.empty();
            if (data.length) {
                data.forEach(function (n) {
                    list.append(
                        '<div class="notif-item ' +
                        (n.isRead ? '' : 'unread') + '" ' +
                        'onclick="readNotification(' + n.id + ',' +
                        '\'' + (n.link || '') + '\')">' +
                        '<div class="fw-medium small">' +
                        n.title + '</div>' +
                        '<div class="text-muted" ' +
                        'style="font-size:12px">' +
                        (n.message || '') +
                        '</div>' +
                        '</div>');
                });
            } else {
                list.html(
                    '<div class="p-3 text-center text-muted small">' +
                    'কোনো বিজ্ঞপ্তি নেই</div>');
            }
        } catch { }
    }

    $('#notif-bell').on('click', function () {
        $('#notif-dropdown').toggleClass('d-none');
    });

    $('#mark-all-read').on('click', async function (e) {
        e.preventDefault();
        await fetch('/api/notifications/mark-all-read',
            { method: 'POST' });
        loadNotifications();
    });

    // ── Ad Tracking (impression) ─────────────────────────
    const adObserver = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                const adId = entry.target.dataset.adId;
                if (adId) {
                    fetch('/api/ads/' + adId + '/impression',
                        { method: 'POST' });
                    adObserver.unobserve(entry.target);
                }
            }
        });
    });
    document.querySelectorAll('[data-ad-id]').forEach(function (el) {
        adObserver.observe(el);
    });

    // ── Toastr Config ────────────────────────────────────
    toastr.options = {
        positionClass: 'toast-bottom-right',
        timeOut: 3500,
        closeButton: true,
        progressBar: true
    };

    // ── Sticky Header Shadow ─────────────────────────────
    $(window).on('scroll', function () {
        if ($(this).scrollTop() > 80)
            $('#main-header').addClass('scrolled');
        else
            $('#main-header').removeClass('scrolled');
    });

    // ── Close notification dropdown on outside click ─────
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.notification-bell-widget').length)
            $('#notif-dropdown').addClass('d-none');
    });

    // ── Smooth scroll to category section on home page ───
    document.querySelectorAll('.nav-link-custom').forEach(function (link) {
        link.addEventListener('click', function (e) {
            const href = this.getAttribute('href') || '';
            if (!href.startsWith('/category/')) return;

            const slug = href.replace('/category/', '')
                .replace(/\/$/, '');
            const section = document.getElementById('cat-' + slug);

            if (section && window.location.pathname === '/') {
                e.preventDefault();
                const offset = 80;
                const top = section.getBoundingClientRect().top
                    + window.scrollY
                    - offset;
                window.scrollTo({ top: top, behavior: 'smooth' });
            }
        });
    });

})(jQuery);

// ── Global Functions ─────────────────────────────────────

function submitSearch() {
    const q = document.getElementById('live-search')?.value?.trim();
    if (q) window.location.href = '/Search?q=' + encodeURIComponent(q);
}

function submitSidebarSearch() {
    const q = document.getElementById('sidebar-search-input')?.value?.trim()
        || document.getElementById('sidebar-search')?.value?.trim();
    if (q) window.location.href = '/Search?q=' + encodeURIComponent(q);
}

function trackAdClick(adId) {
    fetch('/api/ads/' + adId + '/click', { method: 'POST' }).catch(function () { });
}

async function readNotification(id, link) {
    try {
        await fetch('/api/notifications/' + id + '/read',
            { method: 'POST' });
    } catch { }
    if (link) window.location.href = link;
}

document.addEventListener('keydown', function (e) {
    if (e.key === 'Enter' &&
        document.activeElement?.id === 'live-search')
        submitSearch();
});