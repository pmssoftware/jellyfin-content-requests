using System;
using System.IO;
using System.Net.Mime;
using Jellyfin.Plugin.ContentRequests.Models;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.ContentRequests.Api;

/// <summary>
/// Request form and request-management API.
/// </summary>
[ApiController]
[Route("ContentRequests")]
public sealed class ContentRequestsController : ControllerBase
{
    [HttpGet("Form")]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Text.Html)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetForm()
    {
        const string resourceName = "Jellyfin.Plugin.ContentRequests.Web.request-form.html";
        using var stream = typeof(Plugin).Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return NotFound();
        }

        using var reader = new StreamReader(stream);
        Response.Headers.CacheControl = "no-store";
        return Content(reader.ReadToEnd(), "text/html; charset=utf-8");
    }

    [HttpGet("DisplaySettings")]
    [Authorize]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(DisplaySettings), StatusCodes.Status200OK)]
    public ActionResult GetDisplaySettings()
    {
        var plugin = Plugin.Instance;
        return plugin is null
            ? Problem("The Content Requests plugin is not ready.", statusCode: StatusCodes.Status503ServiceUnavailable)
            : Ok(plugin.Store.GetDisplaySettings());
    }

    [HttpPut("DisplaySettings")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(DisplaySettings), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult SetDisplaySettings([FromBody] DisplaySettings input)
    {
        var tabName = input.TabName?.Trim() ?? string.Empty;
        if (tabName.Length == 0)
        {
            return BadRequest(new { Message = "A tab name is required." });
        }

        input.TabName = tabName;
        var plugin = Plugin.Instance;
        return plugin is null
            ? Problem("The Content Requests plugin is not ready.", statusCode: StatusCodes.Status503ServiceUnavailable)
            : Ok(plugin.Store.SetDisplaySettings(input));
    }

    [HttpPost]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ContentRequest), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<ContentRequest> Create([FromBody] CreateContentRequest input)
    {
        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            return Problem("The Content Requests plugin is not ready.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var title = input.Title?.Trim() ?? string.Empty;
        if (title.Length == 0)
        {
            return BadRequest(new { Message = "A content name is required." });
        }

        var contentType = RequestValues.CanonicalContentType(input.ContentType?.Trim() ?? string.Empty);
        if (contentType is null)
        {
            return BadRequest(new { Message = "Choose a valid content type." });
        }

        input.Title = title;
        input.ContentType = contentType;
        var requestedBy = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            requestedBy = "Jellyfin user";
        }

        var created = plugin.Store.Add(input, requestedBy);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequiresElevation)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetAll()
    {
        var plugin = Plugin.Instance;
        return plugin is null
            ? Problem("The Content Requests plugin is not ready.", statusCode: StatusCodes.Status503ServiceUnavailable)
            : Ok(plugin.Store.GetAll());
    }

    [HttpPut("{id:guid}/Status")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult SetStatus([FromRoute] Guid id, [FromBody] UpdateRequestStatus input)
    {
        var status = RequestValues.CanonicalStatus(input.Status?.Trim() ?? string.Empty);
        if (status is null)
        {
            return BadRequest(new { Message = "Choose a valid request status." });
        }

        var updated = Plugin.Instance?.Store.SetStatus(id, status);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Delete([FromRoute] Guid id)
    {
        return Plugin.Instance?.Store.Delete(id) == true ? NoContent() : NotFound();
    }
}
