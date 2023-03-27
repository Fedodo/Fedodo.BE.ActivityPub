using Microsoft.AspNetCore.Mvc;

namespace Fedodo.BE.ActivityPub.Controllers;

public class ErrorController : Controller
{
    [HttpGet]
    [Route("/NotFound")]
    public ActionResult NotFoundPage()
    {
        return View();
    }
}