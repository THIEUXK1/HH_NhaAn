using LiteDB;

namespace HH_NhaAn.Models;

/// <summary>Bộ phận báo cơm (cấu hình ở trang Cài đặt).</summary>
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Xưởng mới / Xưởng cũ — dùng để cộng "Tổng X. ..." trên bảng báo cơm.</summary>
    public string Workshop { get; set; } = "";
    /// <summary>Nhóm thống kê (vd: "VP khác" gom IT, Kế toán, Mua bán...).</summary>
    public string Group { get; set; } = "";
    public int Order { get; set; }
    public bool Active { get; set; } = true;
}

/// <summary>Số suất theo cột của bảng báo cơm gốc.</summary>
public class MealCounts
{
    public int VnOfficial { get; set; }   // Cơm VN – bữa trưa – chính thức
    public int VnSeasonal { get; set; }   // Cơm VN – bữa trưa – thời vụ
    public int CnLunch { get; set; }      // Cơm TQ – bữa trưa
    public int CnDinner { get; set; }     // Cơm TQ – bữa tối
    public int CnNight { get; set; }      // Cơm TQ – bữa đêm
    public int CnBreakfast { get; set; }  // Cơm TQ – bữa sáng

    [BsonIgnore] public int Vn => VnOfficial + VnSeasonal;
    [BsonIgnore] public int Cn => CnLunch + CnDinner + CnNight + CnBreakfast;
    [BsonIgnore] public int Total => Vn + Cn;

    public int[] ToArray() => [VnOfficial, VnSeasonal, CnLunch, CnDinner, CnNight, CnBreakfast];

    public void Add(MealCounts o)
    {
        VnOfficial += o.VnOfficial; VnSeasonal += o.VnSeasonal;
        CnLunch += o.CnLunch; CnDinner += o.CnDinner; CnNight += o.CnNight; CnBreakfast += o.CnBreakfast;
    }

    public MealCounts Clone() => (MealCounts)MemberwiseClone();
}

public class DailyEntry
{
    public int DeptId { get; set; }
    public int StaffOfficial { get; set; }
    public int StaffSeasonal { get; set; }
    public MealCounts Plan { get; set; } = new();   // Dự kiến
    public MealCounts Final { get; set; } = new();  // Chốt
}

public class DailyReport
{
    /// <summary>Khoá = ngày dạng yyyy-MM-dd.</summary>
    public string Id { get; set; } = "";
    public List<DailyEntry> Entries { get; set; } = [];
    public DateTime UpdatedAt { get; set; }
}

public class AppSettings
{
    public int Id { get; set; } = 1;
    public decimal PriceVn { get; set; } = 25000;
    public decimal PriceCn { get; set; } = 25000;
    /// <summary>Ngày bắt đầu kỳ thống kê (1 = theo tháng dương lịch; 25 = 25 tháng trước → 24 tháng này).</summary>
    public int PeriodStartDay { get; set; } = 1;
    /// <summary>Phiên bản dữ liệu (dùng cho cập nhật một lần).</summary>
    public int DataVersion { get; set; }
}

// ===== View models =====

public class DailyReportVm
{
    public DateOnly Date { get; set; }
    public bool Saved { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CopiedFrom { get; set; }
    public List<(Department Dept, DailyEntry Entry)> Rows { get; set; } = [];
}

public class DailyReportForm
{
    public string Date { get; set; } = "";
    public List<DailyEntry> Entries { get; set; } = [];
}

public class PeriodVm
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string Month { get; set; } = "";
    public string Src { get; set; } = "final";
    public string SrcLabel => Src == "plan" ? "Dự kiến" : "Chốt";
}

public class DeptStatsVm : PeriodVm
{
    public string Sel { get; set; } = "";
    public string Title { get; set; } = "";
    public List<(string Value, string Label, string OptGroup)> Options { get; set; } = [];
    public List<(DateOnly Date, MealCounts C, bool HasData)> Rows { get; set; } = [];
    public MealCounts Total { get; set; } = new();
    public bool[] ShowCol { get; set; } = new bool[6];
    public AppSettings Settings { get; set; } = new();
}

public class SummaryVm : PeriodVm
{
    public string Mode { get; set; } = "all";   // all | vn | cn
    public bool Detail { get; set; }
    public List<string> Columns { get; set; } = [];
    public List<(DateOnly Date, int[] Values, bool HasData)> Rows { get; set; } = [];
    public int[] ColTotals { get; set; } = [];
    public decimal[] ColMoney { get; set; } = [];
    public AppSettings Settings { get; set; } = new();
}

public class SettingsVm
{
    public AppSettings Settings { get; set; } = new();
    public List<Department> Departments { get; set; } = [];
    public List<int> Delete { get; set; } = [];
}
