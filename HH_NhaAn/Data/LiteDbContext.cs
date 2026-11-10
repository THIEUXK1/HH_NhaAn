using LiteDB;

namespace HH_NhaAn.Data;

/// <summary>Giữ một instance LiteDatabase duy nhất cho toàn ứng dụng (đăng ký Singleton).</summary>
public sealed class LiteDbContext : IDisposable
{
    public LiteDatabase Database { get; }

    public LiteDbContext(IConfiguration config, IWebHostEnvironment env)
    {
        var cs = new ConnectionString(config.GetConnectionString("LiteDb") ?? "Filename=App_Data/HH_NhaAn.db");

        // Đường dẫn tương đối → tính từ thư mục gốc dự án
        if (!Path.IsPathRooted(cs.Filename))
            cs.Filename = Path.Combine(env.ContentRootPath, cs.Filename);

        Directory.CreateDirectory(Path.GetDirectoryName(cs.Filename)!);
        Database = new LiteDatabase(cs);
    }

    public ILiteCollection<T> Collection<T>(string? name = null) =>
        name is null ? Database.GetCollection<T>() : Database.GetCollection<T>(name);

    public void Dispose() => Database.Dispose();
}
