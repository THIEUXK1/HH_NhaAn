using HH_NhaAn.Data;
using HH_NhaAn.Models;
using Microsoft.AspNetCore.Mvc;

namespace HH_NhaAn.Controllers;

public class SettingsController(MealService svc) : Controller
{
    public IActionResult Index() =>
        View(new SettingsVm { Settings = svc.GetSettings(), Departments = svc.GetDepartments() });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Index(SettingsVm form)
    {
        form.Settings.PeriodStartDay = Math.Clamp(form.Settings.PeriodStartDay, 1, 28);
        svc.SaveSettings(form.Settings);

        var notes = new List<string>();
        foreach (var d in form.Departments)
        {
            d.Name = d.Name?.Trim() ?? "";
            d.Workshop = d.Workshop?.Trim() ?? "";
            d.Group = d.Group?.Trim() ?? "";
            if (d.Id != 0 && form.Delete.Contains(d.Id))
            {
                if (!svc.DeleteDepartment(d.Id)) notes.Add($"\"{d.Name}\" đã có dữ liệu nên chỉ chuyển sang ngừng sử dụng.");
                continue;
            }
            if (d.Name == "") continue; // dòng thêm mới để trống
            svc.SaveDepartment(d);
        }
        TempData["msg"] = "Đã lưu cài đặt. " + string.Join(" ", notes);
        return RedirectToAction(nameof(Index));
    }
}
