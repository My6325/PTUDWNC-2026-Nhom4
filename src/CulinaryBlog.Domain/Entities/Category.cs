using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Exceptions;

namespace CulinaryBlog.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public int OrderIndex { get; set; }

    /// <summary>
    /// Kiểm tra các quy tắc nghiệp vụ của Danh mục và ném Domain Exception nếu vi phạm.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 100)
        {
            throw new InvalidCategoryNameException();
        }

        if (string.IsNullOrWhiteSpace(Slug))
        {
            throw new CategorySlugEmptyException();
        }

        if (OrderIndex < 0)
        {
            throw new InvalidCategoryOrderIndexException();
        }
    }
}
