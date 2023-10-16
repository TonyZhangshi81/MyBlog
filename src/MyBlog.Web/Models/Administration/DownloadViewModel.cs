using MyBlog.Data;
using MyBlog.Web.Infrastructure.Paging;

namespace MyBlog.Web.Models.Administration;

public class DownloadViewModel
{
    public string? SearchTerm { get; set; }

    public PagedResult<BlogEntry>? BlogEntries { get; set; }
}
