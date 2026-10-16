using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Auth.Profile;

public record UpdateProfileCommand(UpdateProfileRequest Request) : IRequest<UserDto>;
