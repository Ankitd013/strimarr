using Gelato.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gelato.Controllers;

[ApiController]
[AllowAnonymous]
[Route("clawiz/relay/{id}/video")]
public class ParallelRelayController : ControllerBase
{
    [HttpGet]
    [HttpHead]
    public Task Stream(string id) => ParallelStreamRelay.Serve(HttpContext, id,
        GelatoPlugin.Instance!.Configuration.ParallelRelayConnections);
}
