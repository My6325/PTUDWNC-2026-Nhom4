using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Auth.Refresh;

public record RefreshTokenCommand(RefreshTokenRequest Request) : IRequest<AuthResponseDto>;
