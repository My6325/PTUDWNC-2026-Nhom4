using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CulinaryBlog.Application.Features.Auth.Me;

public class GetCurrentUserProfileQueryHandler : IRequestHandler<GetCurrentUserProfileQuery, UserDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetCurrentUserProfileQueryHandler(
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager)
    {
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<UserDto> Handle(GetCurrentUserProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Không xác định được danh tính người dùng.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedException("Người dùng không tồn tại hoặc đã bị khoá.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new UserDto(
            Id: user.Id,
            UserName: user.UserName!,
            Email: user.Email!,
            DisplayName: user.DisplayName,
            AvatarUrl: user.AvatarUrl,
            Bio: user.Bio,
            Roles: roles,
            CreatedAt: user.CreatedAt
        );
    }
}
