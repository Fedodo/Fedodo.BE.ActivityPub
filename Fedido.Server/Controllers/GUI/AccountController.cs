using System.Security.Claims;
using CommonExtensions;
using Fedido.Server.Interfaces;
using Fedido.Server.Model.OAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IAuthenticationHandler = Fedido.Server.Interfaces.IAuthenticationHandler;

namespace Fedido.Server.Controllers.GUI;

public class AccountController : Controller
{
    private readonly IAuthenticationHandler _authenticationHandler;
    private readonly ILogger<AccountController> _logger;
    private readonly IUserHandler _userHandler;

    public AccountController(IAuthenticationHandler authenticationHandler, IUserHandler userHandler,
        ILogger<AccountController> logger)
    {
        _authenticationHandler = authenticationHandler;
        _userHandler = userHandler;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("~/account/login")]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [Route("~/account/login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (ModelState.IsValid)
        {
            var user = await _userHandler.GetUserByName(model.Username);

            if (user.IsNull()) return BadRequest("UserName or Password are not correct!");

            if (!_authenticationHandler.VerifyPasswordHash(model.Password, user.PasswordHash, user.PasswordSalt))
                return BadRequest("UserName or Password are not correct!");

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, model.Username)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity));

            if (Url.IsLocalUrl(model.ReturnUrl)) return Redirect(model.ReturnUrl);

            _logger.LogWarning("Redirecting to ~");
            return Redirect("~");
        }

        return View(model);
    }

    // public async Task<IActionResult> Logout()
    // {
    //     await HttpContext.SignOutAsync();
    //
    //     return RedirectToAction(nameof(HomeController.Index), "Home");
    // }
}