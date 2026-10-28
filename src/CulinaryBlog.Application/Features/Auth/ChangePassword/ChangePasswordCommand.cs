using MediatR;

namespace CulinaryBlog.Application.Features.Auth.ChangePassword;

public record ChangePasswordCommand(ChangePasswordRequest Request) : IRequest;
