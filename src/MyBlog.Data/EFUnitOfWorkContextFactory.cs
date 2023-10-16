using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyBlog.Data;

/// <summary>
/// IDesignTimeDbContextFactory 开发模式下使用（用於 add-Migration 時使用）！！
/// </summary>
public class EFUnitOfWorkContextFactory : IDesignTimeDbContextFactory<EFUnitOfWork>
{
    public EFUnitOfWork CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<EFUnitOfWork>();

        // builder.UseSqlServer("Server=(local);Database=MVCBlog;Trusted_Connection=True;MultipleActiveResultSets=true");
        //builder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MVCBlog-5B146AA6-13E2-40EC-BA3C-3F7981A9F295;Trusted_Connection=True;MultipleActiveResultSets=true;application name=MVCBlog");
        builder.UseSqlServer("Server=DESKTOP-OO2888A\\SQL2016;Database=MVCBlog;User ID=sa;Password=Tony19811031;TrustServerCertificate=true;Trusted_Connection=False;MultipleActiveResultSets=true;application name=MVCBlog");
        return new EFUnitOfWork(builder.Options);
    }

#pragma warning disable SA1204 // Static elements should appear before instance elements
    public static void Main(string[] args)
#pragma warning restore SA1204 // Static elements should appear before instance elements
    {
    }
}