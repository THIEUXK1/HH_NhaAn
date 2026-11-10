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

    /// <summary>Bảng tổng hợp: ngày × bộ phận, tách khối cơm Việt Nam và cơm Trung Quốc.</summary>
    public IActionResult Summary(string? month, string? from, string? to, string? src, string? mode, bool detail = false)
    {
        var vm = new SummaryVm { Settings = svc.GetSettings(), Detail = detail, Mode = mode is "vn" or "cn" ? mode : "all" };
        FillPeriod(vm, month, from, to, src);
        var depts = svc.GetDepartments();

        // Cột: từng bộ phận, hoặc gộp theo nhóm
        var labels = new List<string>();
        var colOf = new Dictionary<int, int>();
        foreach (var d in depts)
        {
            var label = detail || d.Group == "" ? d.Name : d.Group;
            var idx = labels.IndexOf(label);
            if (idx < 0) { labels.Add(label); idx = labels.Count - 1; }
            colOf[d.Id] = idx;
        }

        int n = labels.Count;
        var days = Days(vm.From, vm.To).ToList();
        var vn = new SummarySide { Title = "CƠM VIỆT NAM", Price = vm.Settings.PriceVn, Css = "side-vn" };
        var cn = new SummarySide { Title = "CƠM TRUNG QUỐC", Price = vm.Settings.PriceCn, Css = "side-cn" };
        foreach (var side in new[] { vn, cn })
        {
            side.Columns = [.. labels];
            side.Values = days.Select(_ => new int[n]).ToList();
            side.Totals = new int[n];
        }

        var reports = svc.GetRange(vm.From, vm.To).ToDictionary(r => r.Id);
        for (var di = 0; di < days.Count; di++)
        {
            var has = reports.TryGetValue(MealService.Key(days[di]), out var r);
            vm.Days.Add((days[di], has));
            if (!has) continue;
            foreach (var e in r!.Entries)
            {
                if (!colOf.TryGetValue(e.DeptId, out var i)) continue;
                var c = vm.Src == "plan" ? e.Plan : e.Final;
                vn.Values[di][i] += c.Vn; vn.Totals[i] += c.Vn;
                cn.Values[di][i] += c.Cn; cn.Totals[i] += c.Cn;
            }
        }

        // Mỗi khối chỉ giữ cột bộ phận có suất (bảng trống thì giữ hết)
        foreach (var side in new[] { vn, cn })
        {
            if (!side.Totals.Any(x => x > 0)) continue;
            var keep = Enumerable.Range(0, n).Where(i => side.Totals[i] > 0).ToArray();
            side.Columns = keep.Select(i => side.Columns[i]).ToList();
            side.Totals = keep.Select(i => side.Totals[i]).ToArray();
            side.Values = side.Values.Select(v => keep.Select(i => v[i]).ToArray()).ToList();
        }

        if (vm.Mode != "cn") vm.Sides.Add(vn);
        if (vm.Mode != "vn") vm.Sides.Add(cn);
        return View(vm);
    }
}
