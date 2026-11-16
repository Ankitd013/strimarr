using Gelato.Services;
using Gelato.Decorators.MediaSources;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Gelato.Controllers;
[ApiController]
[Route("clawiz/versions")]
[Authorize]
public class HiddenVersionsController(ILibraryManager library, IUserManager users, GelatoManager manager) : ControllerBase
{
    [HttpPost("{id}")]
    public ActionResult Set(Guid id, [FromQuery] bool includeHidden)
    {
        if (!HttpContext.TryGetUserId(out var user) || user == Guid.Empty) return Unauthorized();
        var item = library.GetItemById(id);
        var requestingUser = users.GetUserById(user);
        if (requestingUser is null || item is null || !item.IsGelatoPlaybackItem() || !item.IsVisible(requestingUser)) return NotFound();
        HiddenVersions.Set(user, id, includeHidden);
        StreamSyncOnOpen.Forget(StreamSyncOnOpen.SyncCacheKey(item, user));
        manager.ResetStreamSync(id);
        return Ok(new { IncludeHidden = includeHidden });
    }
}
