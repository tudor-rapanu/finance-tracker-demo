using FinanceTracker.Application.Admin.Commands;
using FinanceTracker.Application.Admin.Queries;
using FinanceTracker.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get all registered users with their roles.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(List<UserDto>), 200)]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await _mediator.Send(new GetAllUsersQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>Get system-wide stats: user count, transaction totals, recent users.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(AdminDashboardDto), 200)]
    public async Task<IActionResult> GetAdminDashboard([FromQuery] int? month, [FromQuery] int? year)
    {
        var result = await _mediator.Send(new GetAdminDashboardQuery(month, year));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>Promote or demote a user by assigning or removing a role.</summary>
    [HttpPut("users/role")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SetUserRole([FromBody] SetUserRoleDto dto)
    {
        var result = await _mediator.Send(new SetUserRoleCommand(dto));
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    /// <summary>Recalculate stored converted transaction amounts based on each user's preferred currency.</summary>
    [HttpPost("transactions/recalculate-amounts")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RecalculateTransactionAmounts()
    {
        var result = await _mediator.Send(new RecalculateTransactionAmountsCommand());
        return result.IsSuccess
            ? Ok(new { updatedTransactions = result.Value })
            : BadRequest(new { error = result.Error });
    }
}
