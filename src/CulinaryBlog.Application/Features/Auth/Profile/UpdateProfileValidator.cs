using FluentValidation;

namespace CulinaryBlog.Application.Features.Auth.Profile;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.Request)
            .Must(x => x.DisplayName != null || x.AvatarUrl != null || x.Bio != null)
            .WithMessage("Ít nhất một trường dữ liệu (DisplayName, AvatarUrl, Bio) phải được cung cấp để cập nhật.");

        RuleFor(x => x.Request.DisplayName)
            .NotEmpty().WithMessage("DisplayName không được để trống nếu đã cung cấp.")
            .MaximumLength(100).WithMessage("DisplayName không được vượt quá 100 ký tự.")
            .When(x => x.Request.DisplayName != null);

        RuleFor(x => x.Request.AvatarUrl)
            .MaximumLength(500).WithMessage("AvatarUrl không được vượt quá 500 ký tự.")
            .Must(BeAValidUrl).WithMessage("AvatarUrl phải là định dạng URL hợp lệ (http/https).")
            .When(x => !string.IsNullOrEmpty(x.Request.AvatarUrl)); // Allow empty string to clear the avatar

        RuleFor(x => x.Request.Bio)
            .MaximumLength(500).WithMessage("Bio không được vượt quá 500 ký tự.")
            .When(x => x.Request.Bio != null);
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var outUri) 
               && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps);
    }
}
