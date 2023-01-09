using System.Security.Claims;
using ActivityPubServer.Interfaces;
using ActivityPubServer.Model.OAuth;
using CommonExtensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IAuthenticationHandler = ActivityPubServer.Interfaces.IAuthenticationHandler;

namespace ActivityPubServer.Controllers.GUI;

public class AccountController : Controller
{
    private readonly IAuthenticationHandler _authenticationHandler;
    private readonly IUserHandler _userHandler;

    public AccountController(IAuthenticationHandler authenticationHandler, IUserHandler userHandler)
    {
        _authenticationHandler = authenticationHandler;
        _userHandler = userHandler;
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
            var user = await _userHandler.GetUser(model.Username);

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

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        return View(model);
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }
}