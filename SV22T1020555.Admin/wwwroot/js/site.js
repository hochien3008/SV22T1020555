// Hiển thị ảnh được chọn từ input file lên thẻ img
function previewImage(input) {
    if (!input.files || !input.files[0]) return;
    const previewId = input.dataset.imgPreview;
    if (!previewId) return;
    const img = document.getElementById(previewId);
    if (!img) return;
    const reader = new FileReader();
    reader.onload = function (e) {
        img.src = e.target.result;
    };
    reader.readAsDataURL(input.files[0]);
}

/**
 * Logic định dạng tiền tệ (Thầy dạy): Chấm phần nghìn, Phẩy thập phân
 */
function onlyDigits(s) {
    return (s || '').toString().replace(/\D/g, '');
}

function addThousandDots(s) {
    s = (s || '').toString().replace(/[^\d,]/g, '');
    if (!s) return '';
    var c = s.lastIndexOf(',');
    var intRaw, frac = '';
    if (c >= 0) {
        intRaw = onlyDigits(s.slice(0, c));
        frac = onlyDigits(s.slice(c + 1)).slice(0, 2);
    } else {
        intRaw = onlyDigits(s);
    }
    if (!intRaw && !frac) return s.endsWith(',') ? ',' : '';
    var head = intRaw ? intRaw.replace(/\B(?=(\d{3})+(?!\d))/g, '.') : '';
    if (c >= 0 && !frac && s.endsWith(',')) return head + ',';
    return head + (frac ? ',' + frac : '');
}

/**
 * Chuyển đổi định dạng hiển thị sang số thực để gửi về Server (1.000,50 -> 1000.5)
 */
function toServerPrice(val) {
    val = (val || '').toString().replace(/\s/g, '');
    var c = val.lastIndexOf(',');
    if (c >= 0) {
        var a = onlyDigits(val.slice(0, c));
        var b = onlyDigits(val.slice(c + 1));
        return b ? a + '.' + b : a;
    }
    return onlyDigits(val);
}

/**
 * Khởi tạo bộ định dạng cho các input có class .money-input
 */
window.setupMoneyInput = function(selector) {
    const elements = document.querySelectorAll(selector || '.money-input');
    elements.forEach(el => {
        // Định dạng giá trị hiện có
        el.value = addThousandDots(el.value);

        // Lắng nghe thay đổi khi gõ
        const handler = function () {
            let pos = this.selectionStart;
            let oldLen = this.value.length;
            this.value = addThousandDots(this.value);
            let newLen = this.value.length;
            pos = pos + (newLen - oldLen);
            this.setSelectionRange(pos, pos);
        };
        el.removeEventListener('input', handler);
        el.addEventListener('input', handler);

        // Đảm bảo unformat trước khi submit form (Native POST)
        if (el.form) {
            el.form.removeEventListener('submit', unformatAllMoneyFields);
            el.form.addEventListener('submit', unformatAllMoneyFields);
        }
    });
};

function unformatAllMoneyFields(e) {
    const form = e.currentTarget;
    form.querySelectorAll('.money-input').forEach(el => {
        el.value = toServerPrice(el.value);
    });
}

// Tìm kiếm AJAX (Index và các trang Search)
window.paginationSearch = function(event, form, page) {
    if (event) event.preventDefault();
    if (!form) return;
    const url = form.action;
    const method = (form.method || "GET").toUpperCase();
    const targetId = form.dataset.target;
    const formData = new FormData(form);
    formData.append("page", page || 1);

    // Unformat tiền trước khi tạo URL/Body
    form.querySelectorAll(".money-input").forEach(el => {
        formData.set(el.name, toServerPrice(el.value));
    });

    let fetchUrl = url;
    if (method === "GET") {
        fetchUrl = url + "?" + new URLSearchParams(formData).toString();
    }

    const targetEl = targetId ? document.getElementById(targetId) : null;
    if (targetEl) targetEl.innerHTML = `<div class="text-center py-4"><span>Đang tải dữ liệu...</span></div>`;

    fetch(fetchUrl, { method: method, body: method === "GET" ? null : formData })
        .then(res => res.text())
        .then(html => {
            if (targetEl) {
                targetEl.innerHTML = html;
                // Khởi tạo lại định dạng cho kết quả mới (nếu có input)
                setupMoneyInput();
            }
        })
        .catch(() => { if (targetEl) targetEl.innerHTML = `<div class="text-danger">Lỗi tải dữ liệu</div>`; });
};

// Logic Modal dùng chung
(function () {
    const modalEl = document.getElementById("dialogModal");
    if (!modalEl) return;
    const modalContent = modalEl.querySelector(".modal-content");
    modalEl.addEventListener('hidden.bs.modal', function () { modalContent.innerHTML = ''; });

    window.openModal = function (event, link) {
        if (!link) return;
        if (event) event.preventDefault();
        const url = link.getAttribute("href");
        modalContent.innerHTML = `<div class="modal-body text-center py-5"><span>Đang tải dữ liệu...</span></div>`;

        let modal = bootstrap.Modal.getInstance(modalEl);
        if (!modal) modal = new bootstrap.Modal(modalEl, { backdrop: 'static', keyboard: false });
        modal.show();

        fetch(url)
            .then(res => res.text())
            .then(html => {
                modalContent.innerHTML = html;
                // QUAN TRỌNG: Gán lại định dạng cho nội dung vừa tải vào Modal
                setupMoneyInput();
            })
            .catch(() => { modalContent.innerHTML = `<div class="modal-body text-danger">Lỗi tải dữ liệu</div>`; });
    };
})();

// Khởi chạy khi trang web tải xong
document.addEventListener("DOMContentLoaded", function () {
    setupMoneyInput();
});
