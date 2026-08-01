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

    // ── Load mega menu news on hover (debounced) ─────────
    let megaMenuHoverTimer;
    $(document).on('mouseenter', '.dropdown-hover', function () {
        const el = this;
        clearTimeout(megaMenuHoverTimer);
        megaMenuHoverTimer = setTimeout(function () {
            const link = $(el).find('.nav-link-custom').first().attr('href') || '';
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
        }, 150);
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
    let searchAbortController;
    $('#live-search').on('input', function () {
        clearTimeout(searchTimer);
        const q = $(this).val().trim();
        if (!q) {
            $('#search-suggestions').empty().hide();
            return;
        }

        searchTimer = setTimeout(async function () {
            // Cancel any in-flight suggestion request before starting a new one
            if (searchAbortController) searchAbortController.abort();
            searchAbortController = new AbortController();

            try {
                const res = await fetch(
                    '/api/search/suggest?q=' + encodeURIComponent(q),
                    { signal: searchAbortController.signal });
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
            } catch (err) {
                // Ignore aborted requests — a newer keystroke superseded this one
                if (err.name !== 'AbortError') { /* swallow — non-critical UI feature */ }
            }
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

    // ── Notifications ─────────────────────────────────────
    // Initial load only — SignalR's 'ReceiveNotification' push (see
    // signalr-client.js) keeps the badge/list in sync in real time from
    // here on. The old 60s poll duplicated that traffic on every open
    // tab for every logged-in user; a 5-minute fallback poll is kept
    // only as a safety net in case a SignalR connection is silently
    // stuck (rare, since onreconnecting/onreconnected already handle
    // normal drops).
    loadNotifications();
    setInterval(loadNotifications, 300000);

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
    window.loadNotifications = loadNotifications;

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
    // Batched: instead of firing one POST per ad the instant it scrolls
    // into view, queue visible ad IDs and flush them as a single request
    // on a short delay. Cuts a homepage with 8 ad slots from 8 separate
    // POSTs down to 1 in the common case, and uses sendBeacon so it
    // never blocks or competes with in-flight page requests.
    let pendingImpressions = [];
    let impressionFlushTimer = null;

    function flushImpressions() {
        if (!pendingImpressions.length) return;
        const ids = pendingImpressions.splice(0, pendingImpressions.length);
        const payload = JSON.stringify({ adIds: ids });

        if (navigator.sendBeacon) {
            const blob = new Blob([payload], { type: 'application/json' });
            navigator.sendBeacon('/api/ads/impressions', blob);
        } else {
            fetch('/api/ads/impressions', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: payload,
                keepalive: true
            }).catch(function () { });
        }
    }

    const adObserver = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                const adId = entry.target.dataset.adId;
                if (adId) {
                    pendingImpressions.push(adId);
                    adObserver.unobserve(entry.target);
                }
            }
        });
    });
    document.querySelectorAll('[data-ad-id]').forEach(function (el) {
        adObserver.observe(el);
    });

    // Flush the impression queue on a short interval, and also on
    // page unload so nothing gets lost from the very last batch.
    impressionFlushTimer = setInterval(flushImpressions, 2000);
    window.addEventListener('pagehide', flushImpressions);
    window.addEventListener('beforeunload', flushImpressions);

    // ── Toastr Config ────────────────────────────────────
    toastr.options = {
        positionClass: 'toast-bottom-right',
        timeOut: 3500,
        closeButton: true,
        progressBar: true
    };

    // ── Sticky Header Shadow (throttled via rAF) ─────────
    let scrollTicking = false;
    $(window).on('scroll', function () {
        if (scrollTicking) return;
        scrollTicking = true;
        requestAnimationFrame(function () {
            $('#main-header').toggleClass('scrolled', $(window).scrollTop() > 80);
            scrollTicking = false;
        });
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

    // ── Video scroll carousel buttons ─────────────────────
    // FIX: this used to be re-bound inside the global keydown handler
    // below, adding a fresh duplicate click listener to every scroll
    // button on every keypress anywhere on the page (search box typing
    // included) — after typing a short query, "next"/"prev" would fire
    // many times per click and leak memory. Bound once, here, on load.
    document.querySelectorAll('.pb-video-scroll-wrap').forEach(function (wrap) {
        var track = wrap.querySelector('.pb-video-scroll-track');
        var prev = wrap.querySelector('.pb-video-scroll-prev');
        var next = wrap.querySelector('.pb-video-scroll-next');
        if (!track) return;

        prev?.addEventListener('click', function () {
            track.scrollBy({ left: -600, behavior: 'smooth' });
        });
        next?.addEventListener('click', function () {
            track.scrollBy({ left: 600, behavior: 'smooth' });
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

// Fire-and-forget — don't make the user wait on an analytics ping
// before the browser follows the ad's target link.
function trackAdClick(adId) {
    if (navigator.sendBeacon) {
        navigator.sendBeacon('/api/ads/' + adId + '/click');
    } else {
        fetch('/api/ads/' + adId + '/click',
            { method: 'POST', keepalive: true }).catch(function () { });
    }
}

// Fire-and-forget — navigate immediately, log the read in the background.
function readNotification(id, link) {
    if (navigator.sendBeacon) {
        navigator.sendBeacon('/api/notifications/' + id + '/read');
    } else {
        fetch('/api/notifications/' + id + '/read',
            { method: 'POST', keepalive: true }).catch(function () { });
    }
    if (link) window.location.href = link;
}

// FIX: the video-scroll-button binding that used to live inside this
// handler has been moved to a one-time setup block above (inside the
// jQuery IIFE). This handler now does only what it's meant to do —
// submit the search box on Enter.
document.addEventListener('keydown', function (e) {
    if (e.key === 'Enter' &&
        document.activeElement?.id === 'live-search')
        submitSearch();
});