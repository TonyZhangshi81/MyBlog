using System.ComponentModel.DataAnnotations;
using MyBlog.Data;
using MyBlog.Localization;
using MyBlog.Web.Infrastructure.Paging;

namespace MyBlog.Web.Models.Administration;

public class ImagesViewModel
{
    public string? SearchTerm { get; set; }

    public PagedResult<Image>? Images { get; set; }

    [Required(ErrorMessageResourceName = nameof(Resources.Validation_Required), ErrorMessageResourceType = typeof(Resources))]
    public IFormFile? Image { get; set; }
}
