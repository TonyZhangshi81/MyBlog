using System.ComponentModel.DataAnnotations;
using MyBlog.Localization;

namespace MyBlog.Web.Models.Administration;

public class AddBlogEntryFileViewModel
{
    [Required(ErrorMessageResourceName = nameof(Resources.Validation_Required), ErrorMessageResourceType = typeof(Resources))]
    public Guid? BlogEntryId { get; set; }

    [Required(ErrorMessageResourceName = nameof(Resources.Validation_Required), ErrorMessageResourceType = typeof(Resources))]
    public IFormFile? File { get; set; }
}
