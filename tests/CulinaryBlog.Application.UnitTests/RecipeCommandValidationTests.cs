using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Application.Features.Recipes.CreateDraft;
using CulinaryBlog.Application.Features.Recipes.Update;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Xunit;

namespace CulinaryBlog.Application.UnitTests;

public sealed class RecipeCommandValidationTests
{
    [Theory]
    [InlineData("1234", false)]
    [InlineData("12345", true)]
    public void CreateDraft_TitleBoundary_IsValidated(string title, bool expectedValid)
    {
        var validator = new CreateRecipeDraftValidator();
        var command = new CreateRecipeDraftCommand(new CreateRecipeDraftRequest(
            title,
            null,
            null,
            10,
            20,
            2,
            RecipeDifficulty.Easy,
            null,
            null,
            null,
            null));

        var result = validator.Validate(command);

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(0, 20, 2)]
    [InlineData(10, 0, 2)]
    [InlineData(10, 20, 0)]
    public void CreateDraft_NonPositiveValues_AreRejected(int prepTime, int cookTime, int servings)
    {
        var validator = new CreateRecipeDraftValidator();
        var command = new CreateRecipeDraftCommand(new CreateRecipeDraftRequest(
            "Công thức hợp lệ",
            null,
            null,
            prepTime,
            cookTime,
            servings,
            RecipeDifficulty.Easy,
            null,
            null,
            null,
            null));

        Assert.False(validator.Validate(command).IsValid);
    }

    [Fact]
    public void Update_RowVersionMustContainFourBytes()
    {
        var validator = new UpdateRecipeValidator();
        var command = new UpdateRecipeCommand(Guid.NewGuid(), new UpdateRecipeRequest(
            "Công thức hợp lệ",
            "Mô tả",
            null,
            null,
            10,
            20,
            2,
            RecipeDifficulty.Medium,
            null,
            [1, 2, 3]));

        Assert.False(validator.Validate(command).IsValid);
    }

    [Fact]
    public void RowVersion_ConvertsToBytesAndBackWithoutDataLoss()
    {
        const uint rowVersion = 4_294_000_000;

        var bytes = RecipeMappings.ToBytes(rowVersion);

        Assert.Equal(sizeof(uint), bytes.Length);
        Assert.Equal(rowVersion, RecipeMappings.ToRowVersion(bytes));
    }

    [Fact]
    public void RecipeCreate_CreatesDraftAndAllowsOptionalChildren()
    {
        var recipe = Recipe.Create(
            "Cá kho tộ",
            "ca-kho-to",
            null,
            null,
            null,
            "author-id",
            10,
            30,
            4,
            RecipeDifficulty.Easy);

        recipe.AddStep(RecipeStep.Create(recipe.Id, 1, "Sơ chế", "Rửa sạch", null, null));
        recipe.AddIngredient(RecipeIngredient.Create(recipe.Id, 1, "Cá", 500, "g", null));

        Assert.Equal(RecipeStatus.Draft, recipe.Status);
        Assert.Single(recipe.Steps);
        Assert.Single(recipe.Ingredients);
        Assert.Equal(1, recipe.Steps.Single().StepNumber);
        Assert.Equal(1, recipe.Ingredients.Single().OrderIndex);
    }
}
