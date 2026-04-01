using FinanceTracker.Application.Common;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Contracts;
using MediatR;

namespace FinanceTracker.Application.Admin.Queries;

// --- Get All Users ---
public record GetAllUsersQuery : IRequest<Result<List<UserDto>>>;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, Result<List<UserDto>>>
{
    private readonly IAdminService _adminService;

    public GetAllUsersQueryHandler(IAdminService adminService) => _adminService = adminService;

    public async Task<Result<List<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        var users = await _adminService.GetAllUsersAsync();
        return Result<List<UserDto>>.Success(users);
    }
}

// --- Get Admin Dashboard ---
public record GetAdminDashboardQuery(int? Month = null, int? Year = null) : IRequest<Result<AdminDashboardDto>>;

public class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, Result<AdminDashboardDto>>
{
    private readonly IAdminService _adminService;

    public GetAdminDashboardQueryHandler(IAdminService adminService) => _adminService = adminService;

    public async Task<Result<AdminDashboardDto>> Handle(GetAdminDashboardQuery request, CancellationToken ct)
    {
        var dashboard = await _adminService.GetAdminDashboardAsync(request.Month, request.Year);
        return Result<AdminDashboardDto>.Success(dashboard);
    }
}
