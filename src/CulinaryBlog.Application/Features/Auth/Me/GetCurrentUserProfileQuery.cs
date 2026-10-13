using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Auth.Me;

public record GetCurrentUserProfileQuery() : IRequest<UserDto>;
