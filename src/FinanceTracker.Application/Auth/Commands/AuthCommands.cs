using FinanceTracker.Application.Common;
using FinanceTracker.Contracts;
using FinanceTracker.Application.Interfaces;
using MediatR;

namespace FinanceTracker.Application.Auth.Commands;

public record RegisterCommand(RegisterDto Dto) : IRequest<Result<AuthResponseDto>>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService) => _authService = authService;

    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.RegisterAsync(request.Dto);
            return Result<AuthResponseDto>.Success(result);
        }
        catch (Exception ex)
        {
            _ = ex;
            return Result<AuthResponseDto>.Failure("Registration failed. Please check your input and try again.");
        }
    }
}

public record LoginCommand(LoginDto Dto) : IRequest<Result<AuthResponseDto>>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService) => _authService = authService;

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.LoginAsync(request.Dto);
            return Result<AuthResponseDto>.Success(result);
        }
        catch (Exception ex)
        {
            _ = ex;
            return Result<AuthResponseDto>.Failure("Invalid email or password.");
        }
    }
}

public record UpdatePreferredCurrencyCommand(UpdatePreferredCurrencyDto Dto) : IRequest<Result<AuthResponseDto>>;

public class UpdatePreferredCurrencyCommandHandler : IRequestHandler<UpdatePreferredCurrencyCommand, Result<AuthResponseDto>>
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public UpdatePreferredCurrencyCommandHandler(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    public async Task<Result<AuthResponseDto>> Handle(UpdatePreferredCurrencyCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<AuthResponseDto>.Failure("User not authenticated.");

        try
        {
            var result = await _authService.UpdatePreferredCurrencyAsync(_currentUser.UserId, request.Dto.PreferredCurrency);
            return Result<AuthResponseDto>.Success(result);
        }
        catch (Exception ex)
        {
            _ = ex;
            return Result<AuthResponseDto>.Failure("Failed to update preferred currency.");
        }
    }
}
