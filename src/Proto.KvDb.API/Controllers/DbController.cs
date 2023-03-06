using Microsoft.AspNetCore.Mvc;

namespace Proto.KvDb.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DbController : ControllerBase
{
    private readonly ActorSystem _actorSystem;
    private readonly ILogger<DbController> _logger;

    public DbController(
        ActorSystem actorSystem,
        ILogger<DbController> logger)
    {
        _actorSystem = actorSystem;
        _logger = logger;
    }
}