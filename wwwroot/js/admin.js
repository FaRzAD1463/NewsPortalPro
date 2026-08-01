$(document).ready(function () {

    // ── Sidebar Toggle ───────────────────────────────────
    $('#sidebar-collapse-btn').on('click', function () {
        $('#admin-sidebar').toggleClass('collapsed');
        $('#admin-main').toggleClass('expanded');
        localStorage.setItem('sidebarCollapsed',
            $('#admin-sidebar').hasClass('collapsed') ? '1' : '0');
    });

    if (localStorage.getItem('sidebarCollapsed') === '1') {
        $('#admin-sidebar').addClass('collapsed');
        $('#admin-main').addClass('expanded');
    }

    // ── AJAX Setup (CSRF) ────────────────────────────────
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajaxSetup({
        headers: { 'RequestVerificationToken': token }
    });

    // ── DataTables ───────────────────────────────────────
    if ($.fn.DataTable && $('.data-table').length) {
        $('.data-table').DataTable({
            language: {
                search: 'খুঁজুন:',
                lengthMenu: 'প্রতি পাতায় _MENU_ এন্ট্রি',
                info: '_START_ থেকে _END_ (মোট _TOTAL_)',
                paginate: { previous: 'পূর্ববর্তী', next: 'পরবর্তী' }
            },
            pageLength: 25
        });
    }

    // ── News Quick Toggles (guarded against rapid double-clicks) ──
    $(document).on('change', '.toggle-breaking', async function () {
        const $el = $(this);
        if ($el.data('busy')) return;
        $el.data('busy', true).prop('disabled', true);

        const id = $el.data('id');
        const val = $el.is(':checked');
        try {
            const res = await fetch(`/Admin/News/ToggleBreaking?id=${id}&value=${val}`, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token }
            });
            if (res.ok) {
                toastr.success(val ? 'ব্রেকিং চালু হয়েছে' : 'ব্রেকিং বন্ধ হয়েছে');
            } else {
                $el.prop('checked', !val);
                toastr.error('আপডেট ব্যর্থ হয়েছে');
            }
        } catch {
            $el.prop('checked', !val);
            toastr.error('নেটওয়ার্ক ত্রুটি');
        } finally {
            $el.data('busy', false).prop('disabled', false);
        }
    });

    $(document).on('change', '.toggle-featured', async function () {
        const $el = $(this);
        if ($el.data('busy')) return;
        $el.data('busy', true).prop('disabled', true);

        const id = $el.data('id');
        const val = $el.is(':checked');
        try {
            const res = await fetch(`/Admin/News/ToggleFeatured?id=${id}&value=${val}`, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token }
            });
            if (res.ok) {
                toastr.success(val ? 'ফিচার্ড চালু হয়েছে' : 'ফিচার্ড বন্ধ হয়েছে');
            } else {
                $el.prop('checked', !val);
                toastr.error('আপডেট ব্যর্থ হয়েছে');
            }
        } catch {
            $el.prop('checked', !val);
            toastr.error('নেটওয়ার্ক ত্রুটি');
        } finally {
            $el.data('busy', false).prop('disabled', false);
        }
    });

    // ── Comment Actions (guarded against double-click double-submit) ──
    $(document).on('click', '.btn-approve-comment', async function () {
        const $btn = $(this);
        if ($btn.data('busy')) return;
        $btn.data('busy', true).prop('disabled', true);

        const id = $btn.data('id');
        try {
            const res = await fetch(`/Admin/Comment/Approve/${id}`, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token }
            });
            if (res.ok) {
                $btn.closest('.comment-row')
                    .fadeOut(400, function () { $(this).remove(); });
                toastr.success('মন্তব্য অনুমোদিত হয়েছে');
            } else {
                toastr.error('ব্যর্থ হয়েছে');
                $btn.data('busy', false).prop('disabled', false);
            }
        } catch {
            toastr.error('নেটওয়ার্ক ত্রুটি');
            $btn.data('busy', false).prop('disabled', false);
        }
    });

    $(document).on('click', '.btn-reject-comment', async function () {
        const $btn = $(this);
        if ($btn.data('busy')) return;
        $btn.data('busy', true).prop('disabled', true);

        const id = $btn.data('id');
        try {
            const res = await fetch(`/Admin/Comment/Reject/${id}`, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token }
            });
            if (res.ok) {
                $btn.closest('.comment-row')
                    .fadeOut(400, function () { $(this).remove(); });
                toastr.warning('মন্তব্য প্রত্যাখ্যান করা হয়েছে');
            } else {
                toastr.error('ব্যর্থ হয়েছে');
                $btn.data('busy', false).prop('disabled', false);
            }
        } catch {
            toastr.error('নেটওয়ার্ক ত্রুটি');
            $btn.data('busy', false).prop('disabled', false);
        }
    });

    // ── Image Upload Preview ─────────────────────────────
    $(document).on('change',
        'input[type="file"][accept="image/*"]', function () {
            const file = this.files[0];
            if (!file) return;
            const reader = new FileReader();
            const previewId = $(this).data('preview');
            reader.onload = e => {
                if (previewId)
                    $(`#${previewId}`).attr('src', e.target.result).show();
            };
            reader.readAsDataURL(file);
        });

    // ── Toastr Config ────────────────────────────────────
    toastr.options = {
        positionClass: 'toast-top-right',
        timeOut: 4000,
        closeButton: true,
        progressBar: true
    };

    // ── Auto-generate slug from title ────────────────────
    let slugManuallyEdited = false;

    $('#Title').on('input', function () {
        if (slugManuallyEdited) return;
        const slug = $(this).val()
            .toLowerCase()
            .replace(/[^\w\s-]/g, '')
            .replace(/[\s_]+/g, '-')
            .trim();
        $('#Slug').val(slug);
    });

    $('#Slug').on('input', function () { slugManuallyEdited = true; });
});

// ── Delete confirmation with SweetAlert2 ─────────────────
// Called from form onsubmit — returns false to prevent
// immediate submit, SweetAlert handles actual submission
function confirmDelete(form) {
    Swal.fire({
        title: 'সংবাদ মুছে ফেলবেন?',
        text: 'এই কাজটি পূর্বাবস্থায় ফেরানো যাবে না।',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'মুছুন',
        cancelButtonText: 'বাতিল'
    }).then(result => {
        if (result.isConfirmed) form.submit();
    });

    return false;
}