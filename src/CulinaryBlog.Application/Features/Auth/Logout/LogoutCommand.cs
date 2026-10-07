using MediatR;
using CulinaryBlog.Application.Features.Auth.Refresh;

namespace CulinaryBlog.Application.Features.Auth.Logout;

public record LogoutCommand(RefreshTokenRequest Request) : IRequest;
