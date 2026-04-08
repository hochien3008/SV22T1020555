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
 * Khởi tạo bộ định dạng cho các input có class .money-input sử dụng AutoNumeric
 */
window.setupMoneyInput = function(selector) {
    const elements = document.querySelectorAll(selector || '.money-input');
    elements.forEach(el => {
        // Kiểm tra xem đã khởi tạo AutoNumeric chưa
        if (AutoNumeric.getAutoNumericElement(el)) return;

        // Xóa các mốc số 0 vô nghĩa hiển thị ban đầu (do server nhét vào) để dễ nhập
        const v = el.value.trim();
        if (v === '0' || v === '0.00' || v === '0,00') {
            el.value = '';
        }

        new AutoNumeric(el, {
            digitGroupSeparator: '.',
            decimalCharacter: ',',
            allowDecimalPadding: false,
            decimalPlaces: 2,
            minimumValue: '0',
            unformatOnSubmit: false, // Tắt tự động unformat để tự custom dấu phẩy
            emptyInputBehavior: 'null',
            selectOnFocus: true, // Tự bôi đen giá trị khi rà chuột để gõ đè lên
            watchExternalChanges: true
        });

        if (el.form && !el.form.dataset.moneySubmitAttached) {
            el.form.dataset.moneySubmitAttached = "true";
            el.form.addEventListener('submit', unformatAllMoneyFields);
        }
    });
};

function unformatAllMoneyFields(e) {
    const form = e.currentTarget;
    // Ngăn AutoNumeric tự format lại ngay lập tức
    form.querySelectorAll('.money-input').forEach(el => {
        const an = AutoNumeric.getAutoNumericElement(el);
        if (an) {
            let val = an.getNumericString(); // 1000.50
            if (val) {
                // Đổi dấu chấm phân cách thập phân thành phẩy cho vi-VN ModelBinder 
                val = val.replace('.', ',');
            }
            // Hủy instace hoặc gỡ events để tránh auto-format lại sau khi gán raw value
            an.remove();
            el.value = val;
        }
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
        const an = AutoNumeric.getAutoNumericElement(el);
        const serverVal = an ? an.getNumericString() : el.value;
        formData.set(el.name, serverVal);
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
