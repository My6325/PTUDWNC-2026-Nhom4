using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Application.Features.Recipes.Delete;
using CulinaryBlog.Application.Features.Recipes.GetBySlug;
using CulinaryBlog.Application.Features.Recipes.Images.DeleteImage;
using CulinaryBlog.Application.Features.Recipes.Images.UploadImage;
using CulinaryBlog.Application.Features.Recipes.Ingredients.AddIngredient;
using CulinaryBlog.Application.Features.Recipes.Ingredients.DeleteIngredient;
using CulinaryBlog.Application.Features.Recipes.Ingredients.UpdateIngredient;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Application.Features.Recipes.Steps.AddStep;
using CulinaryBlog.Application.Features.Recipes.Steps.DeleteStep;
using CulinaryBlog.Application.Features.Recipes.Steps.UpdateStep;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class Lab4RecipeEndpointsCommandTests
{
    private static Recipe CreateTestRecipe()
    {
        return Recipe.Create(
            "Phở Bò Hà Nội",
            "pho-bo-ha-noi",
            "Món phở truyền thống",
            "Nấu nước dùng kỹ",
            null,
            "author-123",
            30,
            120,
            4,
            RecipeDifficulty.Medium);
    }

    [Fact]
    public async Task AddStep_ShouldAutoAssignStepNumberAndInvalidateCache()
    {
        var recipe = CreateTestRecipe();
        var repo = new InMemoryRecipeRepo(recipe);
        var uow = new InMemoryUnitOfWork(repo);
        var cache = new InMemoryCacheInvalidator();
        var handler = new AddRecipeStepCommandHandler(repo, uow, new AllowAuthService(), cache);

        var step1 = await handler.Handle(
            new AddRecipeStepCommand(recipe.Id, new AddRecipeStepRequest("Hầm xương", "Hầm trong 2 giờ", 120, null)),
            CancellationToken.None);

        var step2 = await handler.Handle(
            new AddRecipeStepCommand(recipe.Id, new AddRecipeStepRequest("Trụng bánh phở", "Trụng qua nước sôi", 2, null)),
            CancellationToken.None);

        Assert.Equal(1, step1.StepNumber);
        Assert.Equal(2, step2.StepNumber);
        Assert.Equal(2, recipe.Steps.Count);
        Assert.Equal(2, uow.SaveCount);
        Assert.Equal(2, cache.DetailInvalidationCount);
    }

    [Fact]
    public async Task UpdateStep_ShouldModifyFieldsAndSave()
    {
        var recipe = CreateTestRecipe();
        var step = RecipeStep.Create(recipe.Id, 1, "Bước 1", "Mô tả cũ", 10, null);
        recipe.AddStep(step);

        var repo = new InMemoryRecipeRepo(recipe);
        var uow = new InMemoryUnitOfWork(repo);
        var cache = new InMemoryCacheInvalidator();
        var handler = new UpdateRecipeStepCommandHandler(repo, uow, new AllowAuthService(), cache);

        var result = await handler.Handle(
            new UpdateRecipeStepCommand(recipe.Id, step.Id, new UpdateRecipeStepRequest("Bước 1 mới", "Mô tả mới", 15, "https://img.com/1.jpg")),
            CancellationToken.None);

        Assert.Equal("Bước 1 mới", result.Title);
        Assert.Equal("Mô tả mới", result.Description);
        Assert.Equal(15, result.TimerMinutes);
        Assert.Equal("https://img.com/1.jpg", result.ImageUrl);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task DeleteStep_ShouldAutomaticallyRenumberRemainingSteps()
    {
        var recipe = CreateTestRecipe();
        var step1 = RecipeStep.Create(recipe.Id, 1, "Bước 1", "Mô tả 1", null, null);
        var step2 = RecipeStep.Create(recipe.Id, 2, "Bước 2", "Mô tả 2", null, null);
        var step3 = RecipeStep.Create(recipe.Id, 3, "Bước 3", "Mô tả 3", null, null);
        recipe.AddStep(step1);
        recipe.AddStep(step2);
        recipe.AddStep(step3);

        var repo = new InMemoryRecipeRepo(recipe);
        var uow = new InMemoryUnitOfWork(repo);
        var cache = new InMemoryCacheInvalidator();
        var handler = new DeleteRecipeStepCommandHandler(repo, uow, new AllowAuthService(), cache);

        // Xóa bước 2
        await handler.Handle(new DeleteRecipeStepCommand(recipe.Id, step2.Id), CancellationToken.None);

        var activeSteps = recipe.Steps.Where(s => !s.IsDeleted).OrderBy(s => s.StepNumber).ToList();
        Assert.Equal(2, activeSteps.Count);
        Assert.Equal(step1.Id, activeSteps[0].Id);
        Assert.Equal(1, activeSteps[0].StepNumber);
        Assert.Equal(step3.Id, activeSteps[1].Id);
        Assert.Equal(2, activeSteps[1].StepNumber); // Tự động đánh lại thành 2
        Assert.True(step2.IsDeleted);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task AddUpdateDeleteIngredient_ShouldWorkCorrectly()
    {
        var recipe = CreateTestRecipe();
        var repo = new InMemoryRecipeRepo(recipe);
        var uow = new InMemoryUnitOfWork(repo);
        var cache = new InMemoryCacheInvalidator();

        var addHandler = new AddIngredientCommandHandler(repo, uow, new AllowAuthService(), cache);
        var ing1 = await addHandler.Handle(
            new AddIngredientCommand(recipe.Id, new AddIngredientRequest("Bò tái", 200, "g", "Thái lát mỏng")),
            CancellationToken.None);
        var ing2 = await addHandler.Handle(
            new AddIngredientCommand(recipe.Id, new AddIngredientRequest("Bánh phở", 500, "g", null)),
            CancellationToken.None);

        Assert.Equal(1, ing1.OrderIndex);
        Assert.Equal(2, ing2.OrderIndex);

        var updateHandler = new UpdateIngredientCommandHandler(repo, uow, new AllowAuthService(), cache);
        var updated = await updateHandler.Handle(
            new UpdateIngredientCommand(recipe.Id, ing1.Id, new UpdateIngredientRequest("Bò nạm", 250, "g", "Thái mỏng")),
            CancellationToken.None);
        Assert.Equal("Bò nạm", updated.Name);
        Assert.Equal(250, updated.Quantity);

        var deleteHandler = new DeleteIngredientCommandHandler(repo, uow, new AllowAuthService(), cache);
        await deleteHandler.Handle(new DeleteIngredientCommand(recipe.Id, ing1.Id), CancellationToken.None);

        var activeIngs = recipe.Ingredients.Where(i => !i.IsDeleted).ToList();
        Assert.Single(activeIngs);
        Assert.Equal(1, activeIngs[0].OrderIndex);
    }

    [Fact]
    public async Task UploadImage_ShouldVerifyMagicBytesAndAssignPrimary()
    {
        var recipe = CreateTestRecipe();
        var repo = new InMemoryRecipeRepo(recipe);
        var uow = new InMemoryUnitOfWork(repo);
        var storage = new InMemoryStorageService();
        var cache = new InMemoryCacheInvalidator();
        var handler = new UploadRecipeImageCommandHandler(repo, uow, new AllowAuthService(), storage, cache);

        // 1. Tải ảnh JPEG hợp lệ (Magic bytes: FF D8 FF)
        byte[] validJpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
        using var stream1 = new MemoryStream(validJpeg);

        var img1 = await handler.Handle(
            new UploadRecipeImageCommand(recipe.Id, stream1, "avatar.jpg", "image/jpeg"),
            CancellationToken.None);

        Assert.True(img1.IsPrimary); // Ảnh đầu tiên là ảnh chính
        Assert.Contains(".jpg", img1.OriginalUrl);

        // 2. Tải ảnh PNG thứ hai (Magic bytes: 89 50 4E 47)
        byte[] validPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        using var stream2 = new MemoryStream(validPng);

        var img2 = await handler.Handle(
            new UploadRecipeImageCommand(recipe.Id, stream2, "detail.png", "image/png"),
            CancellationToken.None);

        Assert.False(img2.IsPrimary); // Ảnh thứ 2 không phải là primary
        Assert.Equal(2, recipe.Images.Count);
        Assert.Equal(2, cache.ListInvalidationCount);

        // 3. Tải tệp giả mạo (Magic bytes sai) -> Bị từ chối HTTP 422
        byte[] maliciousBytes = [0x4D, 0x5A, 0x90, 0x00]; // EXE MZ header
        using var stream3 = new MemoryStream(maliciousBytes);

        var ex = await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            handler.Handle(new UploadRecipeImageCommand(recipe.Id, stream3, "hacker.jpg", "image/jpeg"), CancellationToken.None));

        Assert.Equal("INVALID_IMAGE_FORMAT", ex.Code);
    }

    [Fact]
    public async Task DeletePrimaryImage_ShouldPromoteNextImageToPrimary()
    {
        var recipe = CreateTestRecipe();
        var repo = new InMemoryRecipeRepo(recipe);
        var uow = new InMemoryUnitOfWork(repo);
        var storage = new InMemoryStorageService();
        var cache = new InMemoryCacheInvalidator();

        var img1 = new RecipeImage { RecipeId = recipe.Id, OriginalUrl = "https://cdn.com/1.jpg", IsPrimary = true, OrderIndex = 1 };
        var img2 = new RecipeImage { RecipeId = recipe.Id, OriginalUrl = "https://cdn.com/2.jpg", IsPrimary = false, OrderIndex = 2 };
        recipe.AddImage(img1);
        recipe.AddImage(img2);

        var handler = new DeleteRecipeImageCommandHandler(repo, uow, new AllowAuthService(), storage, cache);

        // Xóa ảnh 1 (đang là IsPrimary = true)
        await handler.Handle(new DeleteRecipeImageCommand(recipe.Id, img1.Id), CancellationToken.None);

        Assert.True(img1.IsDeleted);
        Assert.False(img1.IsPrimary);
        Assert.True(img2.IsPrimary); // img2 được nâng lên làm primary
        Assert.Equal("https://cdn.com/1.jpg", storage.LastDeletedUrl);
        Assert.Equal(1, uow.SaveCount);
        Assert.Equal(1, cache.ListInvalidationCount);
    }

    [Fact]
    public async Task UploadImage_ShouldRejectTruncatedOrIncorrectPngSignature()
    {
        var recipe = CreateTestRecipe();
        var repo = new InMemoryRecipeRepo(recipe);
        var uow = new InMemoryUnitOfWork(repo);
        var handler = new UploadRecipeImageCommandHandler(repo, uow, new AllowAuthService(),
            new InMemoryStorageService(), new InMemoryCacheInvalidator());

        byte[][] invalidPngs = [[0x89, 0x50, 0x4E, 0x47], [0x89, 0x50, 0x4E, 0x47, 0, 0, 0, 0]];
        foreach (var bytes in invalidPngs)
        {
            using var stream = new MemoryStream(bytes);
            await Assert.ThrowsAsync<UnprocessableEntityException>(() => handler.Handle(
                new UploadRecipeImageCommand(recipe.Id, stream, "fake.png", "image/png"), CancellationToken.None));
        }
        Assert.Empty(recipe.Images);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task UploadImage_StorageFailure_ShouldNotAddImageOrSave()
    {
        var recipe = CreateTestRecipe();
        var repo = new InMemoryRecipeRepo(recipe);
        var uow = new InMemoryUnitOfWork(repo);
        var cache = new InMemoryCacheInvalidator();
        var storage = new InMemoryStorageService { Fail = true };
        var handler = new UploadRecipeImageCommandHandler(repo, uow, new AllowAuthService(), storage, cache);
        using var stream = new MemoryStream([0xFF, 0xD8, 0xFF]);

        await Assert.ThrowsAsync<StorageUnavailableException>(() => handler.Handle(
            new UploadRecipeImageCommand(recipe.Id, stream, "../../fake.jpg", "image/jpeg"), CancellationToken.None));
        Assert.Empty(recipe.Images);
        Assert.Equal(0, uow.SaveCount);
        Assert.Equal(0, cache.ListInvalidationCount);
        Assert.Equal(0, cache.DetailInvalidationCount);
    }

    [Fact]
    public async Task DeleteImage_StorageFailure_ShouldPreserveImageAndPrimary()
    {
        var recipe = CreateTestRecipe();
        var image = new RecipeImage { RecipeId = recipe.Id, OriginalUrl = "https://cdn.com/1.jpg", IsPrimary = true };
        recipe.AddImage(image);
        var repo = new InMemoryRecipeRepo(recipe);
        var uow = new InMemoryUnitOfWork(repo);
        var cache = new InMemoryCacheInvalidator();
        var handler = new DeleteRecipeImageCommandHandler(repo, uow, new AllowAuthService(),
            new InMemoryStorageService { Fail = true }, cache);

        await Assert.ThrowsAsync<StorageUnavailableException>(() => handler.Handle(
            new DeleteRecipeImageCommand(recipe.Id, image.Id), CancellationToken.None));
        Assert.False(image.IsDeleted);
        Assert.True(image.IsPrimary);
        Assert.Equal(0, uow.SaveCount);
        Assert.Equal(0, cache.ListInvalidationCount);
        Assert.Equal(0, cache.DetailInvalidationCount);
    }

    [Fact]
    public async Task DeleteRecipe_ShouldSoftDeleteCascadeAndInvalidateCache()
    {
        var recipe = CreateTestRecipe();
        var step = RecipeStep.Create(recipe.Id, 1, "Bước 1", "Mô tả", null, null);
        var ing = RecipeIngredient.Create(recipe.Id, 1, "Hành tây", 1, "củ", null);
        var img = new RecipeImage { RecipeId = recipe.Id, OriginalUrl = "https://cdn.com/1.jpg", IsPrimary = true, OrderIndex = 1 };

        recipe.AddStep(step);
        recipe.AddIngredient(ing);
        recipe.AddImage(img);

        var repo = new InMemoryRecipeRepo(recipe);
        var uow = new InMemoryUnitOfWork(repo);
        var cache = new InMemoryCacheInvalidator();

        var handler = new DeleteRecipeCommandHandler(repo, uow, new AllowAuthService(), cache);
        await handler.Handle(new DeleteRecipeCommand(recipe.Id), CancellationToken.None);

        Assert.True(recipe.IsDeleted);
        Assert.NotNull(recipe.DeletedAt);
        Assert.True(step.IsDeleted);
        Assert.True(ing.IsDeleted);
        Assert.True(img.IsDeleted);

        Assert.Equal(1, uow.SaveCount);
        Assert.Equal(1, cache.ListInvalidationCount);
        Assert.Equal(1, cache.DetailInvalidationCount);
    }

    private sealed class InMemoryRecipeRepo(Recipe recipe) : IRecipeRepository
    {
        public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == recipe.Id ? recipe : null);

        public Task<Recipe?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == recipe.Id ? recipe : null);

        public Task<Recipe?> GetByIdForPublishingAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == recipe.Id ? recipe : null);

        public Task<RecipeDetailDto?> GetDetailBySlugAsync(string slug, CancellationToken cancellationToken) =>
            Task.FromResult<RecipeDetailDto?>(null);

        public Task AddAsync(Recipe entity, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> SlugExistsAsync(string slug, Guid? excludedRecipeId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(string? searchTerm, int pageIndex, int pageSize, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(string? searchTerm, Guid? categoryId, string? difficulty, string sortBy, int pageIndex, int pageSize, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class InMemoryUnitOfWork(IRecipeRepository recipes) : IUnitOfWork
    {
        public IRecipeRepository Recipes => recipes;
        public ICategoryRepository Categories => throw new NotImplementedException();
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class AllowAuthService : IRecipeAuthorizationService
    {
        public Task EnsureCanUpdateAsync(Recipe recipe, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InMemoryCacheInvalidator : IRecipeCacheInvalidator
    {
        public int ListInvalidationCount { get; private set; }
        public int DetailInvalidationCount { get; private set; }

        public Task InvalidateListAsync(CancellationToken cancellationToken)
        {
            ListInvalidationCount++;
            return Task.CompletedTask;
        }

        public Task InvalidateDetailAsync(string slug, CancellationToken cancellationToken)
        {
            DetailInvalidationCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryStorageService : ISupabaseStorageService
    {
        public bool Fail { get; init; }
        public string? LastDeletedUrl { get; private set; }

        public Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new StorageUnavailableException("Storage unavailable");
            return Task.FromResult($"https://supabase.co/storage/v1/object/public/culinary-blog/{fileName}");
        }

        public Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new StorageUnavailableException("Storage unavailable");
            LastDeletedUrl = fileUrl;
            return Task.CompletedTask;
        }
    }
}
