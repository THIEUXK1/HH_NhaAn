// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Xuất bảng HTML ra file Excel (.xls) để mở bằng Excel
function exportExcel(tableId, name) {
    const t = document.getElementById(tableId).cloneNode(true);
    t.querySelectorAll('a').forEach(a => a.replaceWith(a.textContent));
    t.setAttribute('border', '1');
    const html = '<html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel">' +
        '<head><meta charset="utf-8"><style>td,th{mso-number-format:"\@";text-align:center}td{mso-number-format:General}</style></head><body>' +
        t.outerHTML + '</body></html>';
    const blob = new Blob(['\ufeff' + html], { type: 'application/vnd.ms-excel' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = name.replace(/[\/:*?"<>|]/g, '_') + '.xls';
    a.click();
    setTimeout(() => URL.revokeObjectURL(a.href), 1000);
}
