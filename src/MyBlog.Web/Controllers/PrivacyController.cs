using Microsoft.AspNetCore.Mvc;

namespace MyBlog.Web.Controllers;

public class PrivacyController : Controller
{
    public IActionResult Index()
    {
        return this.View();
    }
}