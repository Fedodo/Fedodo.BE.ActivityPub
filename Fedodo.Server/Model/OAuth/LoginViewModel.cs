using System.ComponentModel.DataAnnotations;

namespace Fedodo.Server.Model.OAuth;

public class LoginViewModel
{
    [Required] public string Username { get; set; }

    [Required] public string Password { get; set; }

    public string ReturnUrl { get; set; }
}