using System.Buffers.Binary;
using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Features.Recipes.Common;

public static class RecipeMappings
{
    public static RecipeDto ToDto(this Recipe recipe)
    {
        return new RecipeDto(
            recipe.Id,
            recipe.Title,
            recipe.Slug,
            recipe.Description,
            recipe.Instructions,
            recipe.CategoryId,
            recipe.PrepTimeMinutes,
            recipe.CookTimeMinutes,
            recipe.Servings,
            recipe.Difficulty,
            recipe.Status,
            recipe.AuthorId,
            ToBytes(recipe.RowVersion));
    }

    public static byte[] ToBytes(uint rowVersion)
    {
        var bytes = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, rowVersion);
        return bytes;
    }

    public static uint ToRowVersion(byte[] bytes)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }
}
