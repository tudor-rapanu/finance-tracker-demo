using FinanceTracker.Contracts;
using FinanceTracker.Application.Transactions.Commands;
using FinanceTracker.Application.Transactions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get all transactions, optionally filtered by month/year.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int? month, [FromQuery] int? year)
    {
        var result = await _mediator.Send(new GetTransactionsQuery(month, year));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>Get dashboard summary for a given month and year.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardDto), 200)]
    public async Task<IActionResult> GetDashboard([FromQuery] int? month, [FromQuery] int? year)
    {
        var now = DateTime.UtcNow;
        var result = await _mediator.Send(new GetDashboardQuery(month ?? now.Month, year ?? now.Year));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>Create a new transaction.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateTransactionDto dto)
    {
        var result = await _mediator.Send(new CreateTransactionCommand(dto));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetAll), new { }, result.Value);
    }

    /// <summary>Update an existing transaction.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTransactionDto dto)
    {
        var result = await _mediator.Send(new UpdateTransactionCommand(id, dto));
        if (!result.IsSuccess)
            return result.Error == "Transaction not found."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Delete a transaction by ID.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteTransactionCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(new { error = result.Error });
    }
}
