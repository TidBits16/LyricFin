using System.Net.Mime;
using MediaBrowser.Common.Api;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.LyricFin;

[Authorize(Policy = Policies.RequiresElevation)]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[Route("LyricFin")]
public sealed class LyricFinController : ControllerBase
{
    private readonly LyricEngine _engine;
    private readonly ITaskManager _tasks;

    public LyricFinController(LyricEngine engine, ITaskManager tasks)
    {
        _engine = engine;
        _tasks = tasks;
    }

    /// <summary>
    /// Queue a force-fetch of timed lyrics for every audio track (runs as the scheduled task so the UI request cannot time out).
    /// </summary>
    [HttpPost("FetchAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<FetchAllResponse> FetchAll()
    {
        _engine.RequestForce();
        _tasks.CancelIfRunningAndQueue<LyricLibraryTask>();
        return Ok(new FetchAllResponse { Queued = true });
    }
}

public sealed class FetchAllResponse
{
    public bool Queued { get; set; }
}
