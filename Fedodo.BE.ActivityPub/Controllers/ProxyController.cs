using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace Fedodo.BE.ActivityPub.Controllers;

/// <summary>
/// Controller for making an network call in behalf of an client.
/// </summary>
[Route("Proxy")]
public class ProxyController : ControllerBase
{
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(ILogger<ProxyController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets an image in behalf of an client.
    /// </summary>
    /// <param name="imageUrl">The image URL.</param>
    /// <returns>The image as stream.</returns>
    [HttpGet("Image")]
    public async Task<ActionResult> GetImage(Uri imageUrl)
    {
        HttpClient http = new();
        var result = await http.GetAsync(imageUrl);

        if (result.IsSuccessStatusCode)
        {
            return Ok(await result.Content.ReadAsStreamAsync());
        }
        else
        {
            return BadRequest($"Non successful status code.\nStatus-Code: {result.StatusCode}\nMessage: {await result.Content.ReadAsStringAsync()}");
        }
    }
}