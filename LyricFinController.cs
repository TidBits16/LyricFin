using System.Net.Mime;
using MediaBrowser.Common.Api;
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

    public LyricFinController(LyricEngine engine)
    {
        _engine = engine;
    }

    /// <summary>Force-fetch timed lyrics for every audio track (overwrites existing).</summary>
    [HttpPost("FetchAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<FetchAllResponse>> FetchAll(CancellationToken cancellationToken)
    {
        var result = await _engine.RunAsync(force: true, new Progress<double>(), cancellationToken)
            .ConfigureAwait(false);
        return Ok(new FetchAllResponse
        {
            Saved = result.Saved,
            Missed = result.Missed,
            Skipped = result.Skipped
        });
    }
}

public sealed class FetchAllResponse
{
    public int Saved { get; set; }

    public int Missed { get; set; }

    public int Skipped { get; set; }
}
