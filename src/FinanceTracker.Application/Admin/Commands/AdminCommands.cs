using FinanceTracker.Application.Common;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Contracts;
using MediatR;

namespace FinanceTracker.Application.Admin.Commands;

public record SetUserRoleCommand(SetUserRoleDto Dto) : IRequest<Result<bool>>;

public class SetUserRoleCommandHandler : IRequestHandler<SetUserRoleCommand, Result<bool>>
{
    private readonly IAdminService _adminService;

    public SetUserRoleCommandHandler(IAdminService adminService) => _adminService = adminService;

    public async Task<Result<bool>> Handle(SetUserRoleCommand request, CancellationToken ct)
    {
        try
        {
            await _adminService.SetUserRoleAsync(request.Dto.UserId, request.Dto.Role, request.Dto.Assign);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _ = ex;
            return Result<bool>.Failure("Failed to update user role.");
        }
    }
}

public record RecalculateTransactionAmountsCommand : IRequest<Result<int>>;

public class RecalculateTransactionAmountsCommandHandler
    : IRequestHandler<RecalculateTransactionAmountsCommand, Result<int>>
{
    private readonly IAdminService _adminService;

    public RecalculateTransactionAmountsCommandHandler(IAdminService adminService) => _adminService = adminService;

    public async Task<Result<int>> Handle(RecalculateTransactionAmountsCommand request, CancellationToken ct)
    {
        try
        {
            var updated = await _adminService.RecalculateTransactionAmountsAsync();
            return Result<int>.Success(updated);
        }
        catch (Exception ex)
        {
            _ = ex;
            return Result<int>.Failure("Failed to recalculate transaction amounts.");
        }
    }
}
