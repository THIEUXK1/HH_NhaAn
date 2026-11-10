using HH_NhaAn.Models;
using LiteDB;

namespace HH_NhaAn.Data;

public sealed class MealService
{
    private readonly LiteDbContext _db;
    private ILiteCollection<Department> Depts => _db.Collection<Department>("departments");
    private ILiteCollection<DailyReport> Reports => _db.Collection<DailyReport>("reports");
    private ILiteCollection<AppSettings> SettingsCol => _db.Collection<AppSettings>("settings");

    public MealService(LiteDbContext db)
    {
        _db = db;
        Seed();
    }

    public static string Key(DateOnly d) => d.ToString("yyyy-MM-dd");

    private void Seed()
    {
        if (Depts.Count() > 0) return;
        (string Name, string Ws, string Group)[] seed =
        [
            ("In Xoa", "Xưởng mới", "BP In xoa"),
            ("QC", "Xưởng mới", "BP QC"),
            ("Khay", "Xưởng cũ", "BP Khay giấy"),
            ("Cắt", "Xưởng cũ", "BP Cắt"),
            ("IT", "Xưởng cũ", "VP khác"),
            ("Kế toán", "Xưởng cũ", "VP khác"),
            ("Mua bán", "Xưởng cũ", "VP khác"),
            ("Nghiệp vụ", "Xưởng cũ", "VP khác"),
            ("Lái xe", "Xưởng cũ", "VP khác"),
            ("Tạp vụ", "Xưởng cũ", "VP khác"),
            ("HCNS", "Xưởng cũ", "HCNS"),
            ("Bảo vệ", "Xưởng cũ", "VP khác"),
            ("Văn phòng (TQ)", "Xưởng cũ", "VP người nước ngoài"),
            ("Hỗ trợ (TQ)", "Xưởng cũ", "VP người nước ngoài"),
        ];
        var i = 0;
        Depts.InsertBulk(seed.Select(s => new Department { Name = s.Name, Workshop = s.Ws, Group = s.Group, Order = ++i }));
    }

    // ===== Bộ phận =====
    public List<Department> GetDepartments(bool activeOnly = false) =>
        Depts.FindAll().Where(d => !activeOnly || d.Active).OrderBy(d => d.Order).ThenBy(d => d.Id).ToList();

    public void SaveDepartment(Department d)
    {
        if (d.Id == 0) Depts.Insert(d); else Depts.Update(d);
    }

    /// <summary>Xoá nếu chưa có dữ liệu báo cơm; nếu đã có → chỉ ngừng sử dụng. Trả về true nếu xoá hẳn.</summary>
    public bool DeleteDepartment(int id)
    {
        var used = Reports.FindAll().Any(r => r.Entries.Any(e => e.DeptId == id && (e.Plan.Total > 0 || e.Final.Total > 0)));
        if (!used) return Depts.Delete(id);
        var d = Depts.FindById(id);
        if (d != null) { d.Active = false; Depts.Update(d); }
        return false;
    }

    // ===== Báo cơm =====
    public DailyReport? GetReport(DateOnly d) => Reports.FindById(Key(d));

    public DailyReport? GetPreviousReport(DateOnly d) =>
        Reports.Find(Query.LT("_id", Key(d))).OrderByDescending(r => r.Id).FirstOrDefault();

    public List<DailyReport> GetRange(DateOnly from, DateOnly to) =>
        Reports.Find(Query.Between("_id", Key(from), Key(to))).OrderBy(r => r.Id).ToList();

    public void SaveReport(DailyReport r)
    {
        r.UpdatedAt = DateTime.Now;
        Reports.Upsert(r);
    }

    // ===== Cài đặt =====
    public AppSettings GetSettings() => SettingsCol.FindById(1) ?? new AppSettings();
    public void SaveSettings(AppSettings s) { s.Id = 1; SettingsCol.Upsert(s); }

    /// <summary>Tính kỳ thống kê theo tháng (yyyy-MM) và ngày bắt đầu kỳ.</summary>
    public (DateOnly From, DateOnly To) PeriodOf(string? month)
    {
        var start = Math.Clamp(GetSettings().PeriodStartDay, 1, 28);
        DateOnly m;
        if (string.IsNullOrEmpty(month) || !DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out m))
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            m = new DateOnly(today.Year, today.Month, 1);
            if (start > 1 && today.Day >= start) m = m.AddMonths(1);
        }
        if (start == 1) return (m, m.AddMonths(1).AddDays(-1));
        var prev = m.AddMonths(-1);
        return (new DateOnly(prev.Year, prev.Month, start), new DateOnly(m.Year, m.Month, start - 1));
    }
}
