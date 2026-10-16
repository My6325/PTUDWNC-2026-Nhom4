using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CulinaryBlog.Application.Features.Auth.Profile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateProfileCommandHandler(
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager)
    {
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<UserDto> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
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

        var request = command.Request;

        if (request.DisplayName != null)
        {
            user.DisplayName = request.DisplayName.Trim();
        }

        if (request.AvatarUrl != null)
        {
            user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        }

        if (request.Bio != null)
        {
            user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        }

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new UnprocessableEntityException($"Cập nhật hồ sơ thất bại: {errors}", "PROFILE_UPDATE_FAILED");
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
