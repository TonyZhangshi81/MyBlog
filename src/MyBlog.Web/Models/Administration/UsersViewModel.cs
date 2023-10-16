using MyBlog.Data;
using MyBlog.Web.Infrastructure.Paging;

namespace MyBlog.Web.Models.Administration;

public class UsersViewModel
{
    public string? SearchTerm { get; set; }

    public PagedResult<User>? Users { get; set; }
}
