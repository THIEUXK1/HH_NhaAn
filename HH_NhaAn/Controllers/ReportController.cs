using HH_NhaAn.Data;
using HH_NhaAn.Models;
using Microsoft.AspNetCore.Mvc;

namespace HH_NhaAn.Controllers;

/// <summary>Bảng báo cơm hằng ngày (file gốc).</summary>
public class ReportController(MealService svc) : Controller
{
    private static DateOnly ParseDate(string? s) =>
        DateOnly.TryParseExact(s, "yyyy-MM-dd", out var d) ? d : DateOnly.FromDateTime(DateTime.Today);

    public IActionResult Index(string? date, bool copyPrev = false)
    {
        var d = ParseDate(date);
        var depts = svc.GetDepartments();
        var report = svc.GetReport(d);
        var vm = new DailyReportVm { Date = d, Saved = report != null, UpdatedAt = report?.UpdatedAt };

        DailyReport? source = report;
        if (copyPrev)
        {
            source = svc.GetPreviousReport(d);
            vm.CopiedFrom = source?.Id;
        }
        else if (report == null)
        {
            // Ngày mới: lấy sẵn số CNV hiện tại từ ngày gần nhất
            var prev = svc.GetPreviousReport(d);
            if (prev != null)
                source = new DailyReport
                {
                    Entries = prev.Entries.Select(e => new DailyEntry
                        { DeptId = e.DeptId, StaffOfficial = e.StaffOfficial, StaffSeasonal = e.StaffSeasonal }).ToList()
                };
        }

        var byDept = source?.Entries.ToDictionary(e => e.DeptId) ?? [];
        foreach (var dept in depts)
        {
            byDept.TryGetValue(dept.Id, out var e);
            var hasData = e != null && (e.Plan.Total > 0 || e.Final.Total > 0);
            if (!dept.Active && !hasData) continue;
            vm.Rows.Add((dept, e ?? new DailyEntry { DeptId = dept.Id }));
        }

        // Tháng (kỳ) chứa ngày đang xem → dùng cho các nút xem bảng thống kê
        var start = Math.Clamp(svc.GetSettings().PeriodStartDay, 1, 28);
        var m = new DateOnly(d.Year, d.Month, 1);
        if (start > 1 && d.Day >= start) m = m.AddMonths(1);
        ViewBag.Month = m.ToString("yyyy-MM");
        ViewBag.Groups = depts.Where(x => x.Active).Select(x => x.Group).Where(g => g != "").Distinct().ToList();
        ViewBag.Depts = depts.Where(x => x.Active).ToList();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Save(DailyReportForm form)
    {
        var d = ParseDate(form.Date);
        ModelState.Clear(); // ô trống = 0
        svc.SaveReport(new DailyReport { Id = MealService.Key(d), Entries = form.Entries });
        TempData["msg"] = $"Đã lưu báo cơm ngày {d:dd/MM/yyyy}.";
        return RedirectToAction(nameof(Index), new { date = MealService.Key(d) });
    }
}
