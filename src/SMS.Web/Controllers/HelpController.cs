using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SMS.Web.Controllers;

[Authorize]
public class HelpController : Controller
{
    public IActionResult Index() => View();
}
