using HH_NhaAn.Data;
using HH_NhaAn.Models;
using Microsoft.AspNetCore.Mvc;

namespace HH_NhaAn.Controllers;

/// <summary>Các bảng thống kê tự sinh từ dữ liệu báo cơm hằng ngày.</summary>
public class StatsController(MealService svc) : Controller
{
    private void FillPeriod(PeriodVm vm, string? month, string? from, string? to, string? src)
    {
        var (f, t) = svc.PeriodOf(month);
        if (DateOnly.TryParseExact(from, "yyyy-MM-dd", out var f2)) f = f2;
        if (DateOnly.TryParseExact(to, "yyyy-MM-dd", out var t2)) t = t2;
        if (t < f) (f, t) = (t, f);
        if (t.DayNumber - f.DayNumber > 366) t = f.AddDays(366);
        vm.From = f; vm.To = t;
        vm.Month = string.IsNullOrEmpty(month) ? svc.PeriodOf(null).To.ToString("yyyy-MM") : month;
        vm.Src = src == "plan" ? "plan" : "final";
    }

    private static IEnumerable<DateOnly> Days(DateOnly f, DateOnly t)
    {
        for (var d = f; d <= t; d = d.AddDays(1)) yield return d;
    }

    /// <summary>Thống kê theo từng bộ phận / nhóm bộ phận.</summary>
    public IActionResult Department(string? sel, string? month, string? from, string? to, string? src)
    {
        var vm = new DeptStatsVm { Settings = svc.GetSettings() };
        FillPeriod(vm, month, from, to, src);
        var depts = svc.GetDepartments();

        vm.Options.Add(("all", "Tất cả bộ phận", ""));
        foreach (var g in depts.Select(d => d.Group).Where(g => g != "").Distinct())
            vm.Options.Add(("g:" + g, g, "Nhóm"));
        foreach (var d in depts)
            vm.Options.Add(("d:" + d.Id, d.Name + (d.Active ? "" : " (ngừng)"), "Bộ phận"));

        vm.Sel = vm.Options.Any(o => o.Value == sel) ? sel! : (vm.Options.Count > 1 ? vm.Options[1].Value : "all");
        vm.Title = vm.Options.First(o => o.Value == vm.Sel).Label;

        HashSet<int> ids = vm.Sel switch
        {
            "all" => depts.Select(d => d.Id).ToHashSet(),
            _ when vm.Sel.StartsWith("g:") => depts.Where(d => d.Group == vm.Sel[2..]).Select(d => d.Id).ToHashSet(),
            _ => int.TryParse(vm.Sel[2..], out var id) ? [id] : [],
        };

        var reports = svc.GetRange(vm.From, vm.To).ToDictionary(r => r.Id);
        foreach (var day in Days(vm.From, vm.To))
        {
            var c = new MealCounts();
            var has = reports.TryGetValue(MealService.Key(day), out var r);
            if (has)
                foreach (var e in r!.Entries.Where(e => ids.Contains(e.DeptId)))
                    c.Add(vm.Src == "plan" ? e.Plan : e.Final);
            vm.Rows.Add((day, c, has));
            vm.Total.Add(c);
        }


        vm.ShowCol = [true, true, true, true, true, true]; // luôn hiện đủ các cột
        return View(vm);
    }

    /// <summary>Bảng tổng hợp: ngày × bộ phận.</summary>
    public IActionResult Summary(string? month, string? from, string? to, string? src, string? mode, bool detail = false)
    {
        var vm = new SummaryVm { Settings = svc.GetSettings(), Detail = detail, Mode = mode is "vn" or "cn" ? mode : "all" };
        FillPeriod(vm, month, from, to, src);
        var depts = svc.GetDepartments();

        // Cột: từng bộ phận, hoặc gộp theo nhóm
        var colOf = new Dictionary<int, int>();
        foreach (var d in depts)
        {
            var label = detail || d.Group == "" ? d.Name : d.Group;
            var idx = vm.Columns.IndexOf(label);
            if (idx < 0) { vm.Columns.Add(label); idx = vm.Columns.Count - 1; }
            colOf[d.Id] = idx;
        }

        int n = vm.Columns.Count;
        vm.ColTotals = new int[n];
        vm.ColMoney = new decimal[n];
        var reports = svc.GetRange(vm.From, vm.To).ToDictionary(r => r.Id);
        foreach (var day in Days(vm.From, vm.To))
        {
            var vals = new int[n];
            var has = reports.TryGetValue(MealService.Key(day), out var r);
            if (has)
                foreach (var e in r!.Entries)
                {
                    if (!colOf.TryGetValue(e.DeptId, out var i)) continue;
                    var c = vm.Src == "plan" ? e.Plan : e.Final;
                    var vn = vm.Mode == "cn" ? 0 : c.Vn;
                    var cn = vm.Mode == "vn" ? 0 : c.Cn;
                    vals[i] += vn + cn;
                    vm.ColTotals[i] += vn + cn;
                    vm.ColMoney[i] += vn * vm.Settings.PriceVn + cn * vm.Settings.PriceCn;
                }
            vm.Rows.Add((day, vals, has));
        }

        // Ẩn cột không có suất nào (trừ khi cả bảng trống)
        if (vm.ColTotals.Any(x => x > 0))
        {
            var keep = Enumerable.Range(0, n).Where(i => vm.ColTotals[i] > 0).ToArray();
            vm.Columns = keep.Select(i => vm.Columns[i]).ToList();
            vm.ColTotals = keep.Select(i => vm.ColTotals[i]).ToArray();
            vm.ColMoney = keep.Select(i => vm.ColMoney[i]).ToArray();
            vm.Rows = vm.Rows.Select(r => (r.Date, keep.Select(i => r.Values[i]).ToArray(), r.HasData)).ToList();
        }
        return View(vm);
    }
}
