using MediatR;

namespace CulinaryBlog.Application.Features.Auth.ResetPassword;

public record ResetPasswordCommand(ResetPasswordRequest Request) : IRequest;
