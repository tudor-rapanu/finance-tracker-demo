using FinanceTracker.Application.Budgets.Commands;
using FinanceTracker.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BudgetsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Create a new monthly budget for a category.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BudgetDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateBudgetDto dto)
    {
        var result = await _mediator.Send(new CreateBudgetCommand(dto));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(Create), new { }, result.Value);
    }

    /// <summary>Update an existing budget by ID.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BudgetDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBudgetDto dto)
    {
        var result = await _mediator.Send(new UpdateBudgetCommand(id, dto));
        if (!result.IsSuccess)
            return result.Error == "Budget not found."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Delete a budget by ID.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteBudgetCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }
}
