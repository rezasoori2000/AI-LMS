using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers;

/// <summary>
/// Base class for all API controllers.
/// Applies [ApiController] and the standard /api/[controller] route prefix.
/// All domain controllers should inherit from this.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
}
