using Microsoft.AspNetCore.Mvc;

namespace ActivityPubServer.Controllers.GUI;

public class HomeController : Controller
{
    [HttpGet]
    [Route("~/index")]
    public IActionResult Index()
    {
        return View();
    }
}