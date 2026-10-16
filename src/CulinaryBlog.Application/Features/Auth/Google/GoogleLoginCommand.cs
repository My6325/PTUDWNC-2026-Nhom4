using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Auth.Google;

public record GoogleLoginRequest(string IdToken);

public record GoogleLoginCommand(string IdToken, string? ClientIp, string? UserAgent) : IRequest<AuthResponseDto>;

