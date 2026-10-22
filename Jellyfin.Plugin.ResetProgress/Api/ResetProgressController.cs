using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ResetProgress.Api;

/// <summary>
/// Serves the web client script and resets watch progress.
/// </summary>
[ApiController]
[Route("ResetProgress")]
public class ResetProgressController : ControllerBase
{
    // Same claim Jellyfin's own controllers read the user from (Jellyfin.Api.Constants.InternalClaimTypes.UserId).
    private const string UserIdClaim = "Jellyfin-UserId";

    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;
    private readonly ILogger<ResetProgressController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResetProgressController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{T}"/> interface.</param>
    public ResetProgressController(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager userDataManager,
        ILogger<ResetProgressController> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _userDataManager = userDataManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets the script that adds the reset button to the web client.
    /// </summary>
    /// <returns>The JavaScript file.</returns>
    [HttpGet("ClientScript")]
    [AllowAnonymous]
    [Produces("application/javascript")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetClientScript()
    {
        var stream = GetType().Assembly.GetManifestResourceStream(typeof(Plugin).Namespace + ".Web.resetProgress.js");
        if (stream is null)
        {
            return NotFound();
        }

        return File(stream, "application/javascript");
    }

    /// <summary>
    /// Resets the current user's playback position of an item to 0:00, which removes it from Continue Watching.
    /// Played state, play count and last played date are kept.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <returns>No content.</returns>
    [HttpPost("Items/{itemId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult ResetItem([FromRoute] Guid itemId)
    {
        if (!Guid.TryParse(User.FindFirst(UserIdClaim)?.Value, out var userId))
        {
            return Unauthorized();
        }

        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var item = _libraryManager.GetItemById(itemId);
        if (item is null || !item.IsVisible(user))
        {
            return NotFound();
        }

        foreach (var version in GetVersions(item))
        {
            _userDataManager.SaveUserData(
                user,
                version,
                new UpdateUserItemDataDto { PlaybackPositionTicks = 0 },
                UserDataSaveReason.UpdateUserData);
        }

        _logger.LogDebug("Reset playback position of {ItemId} for user {UserId}", itemId, userId);
        return NoContent();
    }

    // A movie with several versions resumes from whichever version was played last, so reset all of them.
    private IEnumerable<BaseItem> GetVersions(BaseItem item)
    {
        if (item is not Video video)
        {
            return [item];
        }

#if NET10_0_OR_GREATER
        return video.GetAllVersions();
#else
        var versions = new List<BaseItem> { video };
        versions.AddRange(video.GetLinkedAlternateVersions());
        versions.AddRange(video.GetLocalAlternateVersionIds().Select(id => _libraryManager.GetItemById(id)).OfType<BaseItem>());
        return versions.DistinctBy(v => v.Id);
#endif
    }
}
