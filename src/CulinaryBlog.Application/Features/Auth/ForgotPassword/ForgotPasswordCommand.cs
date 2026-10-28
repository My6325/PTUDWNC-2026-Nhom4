using MediatR;

namespace CulinaryBlog.Application.Features.Auth.ForgotPassword;

public record ForgotPasswordCommand(ForgotPasswordRequest Request) : IRequest<ForgotPasswordResponseDto>;
