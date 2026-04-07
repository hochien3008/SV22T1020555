/**
 * Ghi chú: Sử dụng thư viện AutoNumeric thay vì xử lý chuỗi thủ công.
 */

// Tiện ích kiểm tra Origin để điều hướng "Quay lại" thông minh
(function () {
  function sameShopOrigin(url) {
    try {
      return new URL(url, window.location.href).origin === window.location.origin;
    } catch (e) {
      return false;
    }
  }

  document.addEventListener("click", function (e) {
      var a = e.target.closest("a.shop-back-link--smart");
      if (!a) return;
      var href = a.getAttribute("href");
      if (!href || href === "#") return;
      e.preventDefault();
      if (window.history.length > 1) {
        window.history.back();
        return;
      }
      var ref = document.referrer;
      if (ref && sameShopOrigin(ref)) {
        window.location.assign(ref);
        return;
      }
      window.location.href = a.href;
    }, false);
})();

/**
 * Khởi tạo bộ định dạng cho các input có class .money-input sử dụng AutoNumeric
 */
window.setupMoneyInput = function(selector) {
    const selectorName = selector || '.money-input';
    const elements = document.querySelectorAll(selectorName);
    
    elements.forEach(el => {
        // Kiểm tra xem đã được khởi tạo AutoNumeric chưa
        if (AutoNumeric.getAutoNumericElement(el)) {
            return;
        }
        
        new AutoNumeric(el, {
            digitGroupSeparator: '.',
            decimalCharacter: ',',
            decimalPlaces: 0,
            minimumValue: '0',
            modifyValueOnWheel: false,
            allowDecimalPadding: false,
            emptyInputBehavior: 'null'
        });
    });
};

/**
 * Hàm lấy giá trị số thuần túy từ AutoNumeric để gửi về Server
 */
function toServerPrice(valOrEl) {
    if (typeof valOrEl === 'string') {
        return valOrEl.replace(/\./g, '');
    }
    const autoNumericEl = AutoNumeric.getAutoNumericElement(valOrEl);
    if (autoNumericEl) {
        return autoNumericEl.getNumber();
    }
    return (valOrEl.value || '').toString().replace(/\./g, '');
}

// Khởi chạy khi trang tải xong
document.addEventListener("DOMContentLoaded", function () {
    if (typeof AutoNumeric !== 'undefined') {
        setupMoneyInput();
    }
});
