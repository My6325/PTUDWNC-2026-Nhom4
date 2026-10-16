using CulinaryBlog.Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Auth.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IClientContext _clientContext;

    public LogoutCommandHandler(
        IApplicationDbContext context,
        IJwtService jwtService,
        ICurrentUserService currentUserService,
        IClientContext clientContext)
    {
        _context = context;
        _jwtService = jwtService;
        _currentUserService = currentUserService;
        _clientContext = clientContext;
    }

    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var requestToken = command.Request.RefreshToken!;
        var hashedToken = _jwtService.HashToken(requestToken);

        var tokenEntity = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hashedToken, cancellationToken);

        // Idempotent: token không tồn tại / đã thu hồi → vẫn thành công, không lộ thông tin.
        // Chỉ thu hồi token thuộc về user hiện tại
        if (tokenEntity != null && tokenEntity.UserId == _currentUserService.UserId && tokenEntity.RevokedAt == null)
        {
            tokenEntity.RevokedAt = DateTime.UtcNow;
            tokenEntity.RevokedByIp = _clientContext.IpAddress;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
