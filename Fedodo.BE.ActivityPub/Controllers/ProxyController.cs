using System.Net.Mime;
using CommonExtensions;
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
    /// Gets an item in behalf of an client.
    /// </summary>
    /// <param name="url">The request URL.</param>
    /// <returns>The content as stream.</returns>
    [HttpGet]
    public async Task<ActionResult> GetItem(Uri url)
    {
        HttpClient http = new();

        foreach (var item in HttpContext.Request.Headers)
        {
            http.DefaultRequestHeaders.Add(item.Key, item.Value.ToString());
        }

        var result = await http.GetAsync(url);

        if (result.IsSuccessStatusCode)
        {
            return Ok(await result.Content.ReadAsStreamAsync());
        }
        else
        {
            return BadRequest(
                $"Non successful status code.\nStatus-Code: {result.StatusCode}\nMessage: {await result.Content.ReadAsStringAsync()}");
        }
    }
}