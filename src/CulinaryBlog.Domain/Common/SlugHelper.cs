using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CulinaryBlog.Domain.Common;

/// <summary>
/// Tiện ích chuẩn hóa chuỗi tiếng Việt thành đường dẫn URL (Slug) thân thiện với SEO.
/// Ví dụ: "Món Bò Kho Tiêu Chuẩn 100%!" -> "mon-bo-kho-tieu-chuan-100"
/// </summary>
public static partial class SlugHelper
{
    /// <summary>
    /// Chuyển đổi một chuỗi văn bản bất kỳ (tiếng Việt có dấu) thành slug URL chuẩn.
    /// </summary>
    /// <param name="text">Văn bản đầu vào (ví dụ: Tiêu đề bài viết, tên danh mục)</param>
    /// <returns>Chuỗi slug không dấu, ngăn cách bằng dấu gạch ngang</returns>
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // 1. Chuyển thành chữ thường và loại bỏ khoảng trắng thừa đầu cuối
        text = text.ToLowerInvariant().Trim();

        // 2. Chuyển đổi chữ đ/Đ tiếng Việt
        text = text.Replace("đ", "d").Replace("Đ", "d");

        // 3. Chuẩn hóa phân rã ký tự Unicode (FormD) để tách dấu khỏi chữ cái gốc
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            // Bỏ qua các dấu thanh và dấu mũ (NonSpacingMark)
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Tái tạo lại chuỗi chuẩn dạng FormC sau khi bỏ dấu
        text = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

        // 4. Thay thế tất cả các ký tự không phải chữ cái (a-z) hoặc số (0-9) thành dấu gạch ngang
        text = Regex.Replace(text, @"[^a-z0-9\s-]", "");

        // 5. Gom nhiều khoảng trắng hoặc nhiều dấu gạch ngang liên tiếp thành một dấu gạch ngang duy nhất
        text = Regex.Replace(text, @"[\s-]+", "-").Trim('-');

        return text;
    }
}
