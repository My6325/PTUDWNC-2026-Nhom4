using Bogus;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence.Seeders;

/// <summary>
/// Nạp dữ liệu mẫu cho module công thức lõi do TV3 phụ trách.
/// Seeder phụ thuộc vào CategorySeeder của TV1 và UserSeeder của TV2.
/// </summary>
public static class RecipeSeeder
{
    private const int TargetRecipeCount = 120;

    // UserSeeder của TV2 tạo Author mẫu với ID cố định này.
    private const string AuthorUserId = "22222222-2222-2222-2222-222222222201";
    private const string LegacyAuthorUserId = "tv2-author-2312664";

    private static readonly string[] RecipeTitles =
    [
        "Cá kho tộ nước dừa",
        "Thịt kho trứng kiểu miền Nam",
        "Gà nướng mật ong sả ớt",
        "Bò lúc lắc sốt tiêu đen",
        "Canh chua cá lóc",
        "Bún bò Huế sa tế",
        "Phở bò tái nạm",
        "Mì Quảng gà ta",
        "Cơm tấm sườn bì chả",
        "Gỏi cuốn tôm thịt",
        "Nem rán giòn Hà Nội",
        "Lẩu thái hải sản",
        "Tôm rim mặn ngọt",
        "Mực xào sa tế",
        "Đậu hũ sốt cà chua",
        "Rau muống xào tỏi",
        "Sườn xào chua ngọt",
        "Cháo gà nấm hương",
        "Bánh mì pate trứng",
        "Chè đậu xanh nha đam",
        "Bánh flan cà phê",
        "Sinh tố bơ sữa đặc",
        "Cà phê sữa đá",
        "Gỏi gà bắp cải",
        "Vịt om sấu",
        "Bánh xèo miền Tây",
        "Bún thịt nướng",
        "Cá hồi áp chảo",
        "Chả cá thì là",
        "Xôi gấc đậu xanh"
    ];

    private static readonly string[] IngredientNames =
    [
        "thịt ba chỉ", "thịt bò", "ức gà", "cá lóc", "tôm tươi", "mực ống", "trứng gà",
        "đậu hũ", "nấm hương", "cà chua", "hành tím", "tỏi", "sả", "ớt", "gừng",
        "rau thơm", "rau muống", "bắp cải", "nước dừa", "nước mắm", "dầu hào",
        "đường thốt nốt", "muối biển", "tiêu xay", "bột nghệ", "sa tế", "chanh",
        "bún tươi", "gạo thơm", "đậu xanh", "sữa đặc"
    ];

    private static readonly string[] Units =
    [
        "g", "kg", "ml", "muỗng canh", "muỗng cà phê", "củ", "trái", "nhánh", "bó", "chén"
    ];

    private static readonly string[] StepTitles =
    [
        "Sơ chế nguyên liệu",
        "Ướp gia vị",
        "Phi thơm nền gia vị",
        "Xào săn nguyên liệu chính",
        "Nấu và điều chỉnh lửa",
        "Nêm nếm lần cuối",
        "Hoàn thiện nước sốt",
        "Trình bày món ăn"
    ];

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await EnsureRecipeTablesExistAsync(context);
        await EnsureAuthorExistsAsync(context);
        await NormalizeLegacyAuthorAsync(context);
        await RemoveLegacyAuthorIfUnusedAsync(context);

        var existingRecipeCount = await context.Recipes.CountAsync();
        if (existingRecipeCount >= TargetRecipeCount)
        {
            return;
        }

        var categoryIds = await GetCategoryIdsAsync(context);
        var recipes = CreateRecipes(existingRecipeCount, categoryIds);

        await context.Recipes.AddRangeAsync(recipes);
        await context.SaveChangesAsync();
    }

    private static async Task EnsureRecipeTablesExistAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Recipes" (
                "Id" uuid NOT NULL,
                "Title" character varying(200) NOT NULL,
                "Slug" character varying(220) NOT NULL,
                "Description" text NULL,
                "Instructions" text NULL,
                "PrepTimeMinutes" integer NOT NULL,
                "CookTimeMinutes" integer NOT NULL,
                "Servings" integer NOT NULL,
                "Difficulty" integer NOT NULL,
                "Status" integer NOT NULL,
                "CategoryId" uuid NULL,
                "AuthorId" character varying(450) NOT NULL,
                "Nutrition_Calories" integer NULL,
                "Nutrition_Protein" numeric(10,2) NULL,
                "Nutrition_Carbohydrates" numeric(10,2) NULL,
                "Nutrition_Fat" numeric(10,2) NULL,
                "Nutrition_Fiber" numeric(10,2) NULL,
                "Nutrition_Sodium" numeric(10,2) NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "IsDeleted" boolean NOT NULL,
                "DeletedAt" timestamp with time zone NULL,
                CONSTRAINT "PK_Recipes" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_Recipes_Categories_CategoryId" FOREIGN KEY ("CategoryId")
                    REFERENCES "Categories" ("Id") ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS "RecipeImages" (
                "Id" uuid NOT NULL,
                "RecipeId" uuid NOT NULL,
                "OriginalUrl" text NOT NULL,
                "MediumUrl" text NULL,
                "ThumbnailUrl" text NULL,
                "IsPrimary" boolean NOT NULL,
                "OrderIndex" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "IsDeleted" boolean NOT NULL,
                "DeletedAt" timestamp with time zone NULL,
                "RowVersion" bigint NOT NULL,
                CONSTRAINT "PK_RecipeImages" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_RecipeImages_Recipes_RecipeId" FOREIGN KEY ("RecipeId")
                    REFERENCES "Recipes" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "RecipeIngredients" (
                "Id" uuid NOT NULL,
                "RecipeId" uuid NOT NULL,
                "Name" text NOT NULL,
                "Quantity" numeric NULL,
                "Unit" text NULL,
                "Notes" text NULL,
                "OrderIndex" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "IsDeleted" boolean NOT NULL,
                "DeletedAt" timestamp with time zone NULL,
                "RowVersion" bigint NOT NULL,
                CONSTRAINT "PK_RecipeIngredients" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_RecipeIngredients_Recipes_RecipeId" FOREIGN KEY ("RecipeId")
                    REFERENCES "Recipes" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "RecipeSteps" (
                "Id" uuid NOT NULL,
                "RecipeId" uuid NOT NULL,
                "StepNumber" integer NOT NULL,
                "Title" text NOT NULL,
                "Description" text NOT NULL,
                "TimerMinutes" integer NULL,
                "ImageUrl" text NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "IsDeleted" boolean NOT NULL,
                "DeletedAt" timestamp with time zone NULL,
                "RowVersion" bigint NOT NULL,
                CONSTRAINT "PK_RecipeSteps" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_RecipeSteps_Recipes_RecipeId" FOREIGN KEY ("RecipeId")
                    REFERENCES "Recipes" ("Id") ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Recipes_Slug" ON "Recipes" ("Slug");
            CREATE INDEX IF NOT EXISTS "IX_Recipes_CategoryId" ON "Recipes" ("CategoryId");
            CREATE INDEX IF NOT EXISTS "IX_RecipeImages_RecipeId" ON "RecipeImages" ("RecipeId");
            CREATE INDEX IF NOT EXISTS "IX_RecipeIngredients_RecipeId" ON "RecipeIngredients" ("RecipeId");
            CREATE INDEX IF NOT EXISTS "IX_RecipeSteps_RecipeId" ON "RecipeSteps" ("RecipeId");
            """);
    }

    private static async Task<List<Guid>> GetCategoryIdsAsync(ApplicationDbContext context)
    {
        var categoryIds = await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.OrderIndex)
            .Select(category => category.Id)
            .ToListAsync();

        if (categoryIds.Count > 0)
        {
            return categoryIds;
        }

        await CategorySeeder.SeedAsync(context);

        return await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.OrderIndex)
            .Select(category => category.Id)
            .ToListAsync();
    }

    private static List<Recipe> CreateRecipes(int existingRecipeCount, IReadOnlyList<Guid> categoryIds)
    {
        Randomizer.Seed = new Random(2312802);
        var faker = new Faker("vi");
        var recipesToCreate = TargetRecipeCount - existingRecipeCount;
        var recipes = new List<Recipe>(recipesToCreate);

        for (var offset = 1; offset <= recipesToCreate; offset++)
        {
            var recipeNumber = existingRecipeCount + offset;
            var recipeId = CreateRecipeId(recipeNumber);
            var title = $"{faker.PickRandom(RecipeTitles)} #{recipeNumber:D3}";
            var slug = $"{SlugHelper.GenerateSlug(title)}-{recipeNumber:D3}";
            var ingredientCount = faker.Random.Int(10, 14);
            var stepCount = faker.Random.Int(5, 8);

            var recipe = new Recipe
            {
                Id = recipeId,
                Title = title,
                Slug = slug,
                Description = faker.Lorem.Sentence(16),
                Instructions = faker.Lorem.Paragraphs(2),
                PrepTimeMinutes = faker.Random.Int(10, 45),
                CookTimeMinutes = faker.Random.Int(20, 120),
                Servings = faker.Random.Int(2, 8),
                Difficulty = faker.PickRandom<RecipeDifficulty>(),
                Status = RecipeStatus.Published,
                CategoryId = categoryIds[(recipeNumber - 1) % categoryIds.Count],
                AuthorId = AuthorUserId,
                Nutrition = new RecipeNutrition
                {
                    Calories = faker.Random.Int(180, 850),
                    Protein = Round(faker.Random.Decimal(8, 65)),
                    Carbohydrates = Round(faker.Random.Decimal(15, 120)),
                    Fat = Round(faker.Random.Decimal(4, 55)),
                    Fiber = Round(faker.Random.Decimal(1, 18)),
                    Sodium = Round(faker.Random.Decimal(120, 1800))
                }
            };

            AddIngredients(recipe, recipeId, recipeNumber, ingredientCount, faker);
            AddSteps(recipe, recipeId, recipeNumber, stepCount, faker);
            AddImage(recipe, recipeId, recipeNumber);

            recipes.Add(recipe);
        }

        return recipes;
    }

    private static async Task EnsureAuthorExistsAsync(ApplicationDbContext context)
    {
        var authorExists = await context.Users.AnyAsync(user => user.Id == AuthorUserId);
        if (authorExists)
        {
            return;
        }

        throw new InvalidOperationException(
            "Author user from UserSeeder is missing. Run UserSeeder before RecipeSeeder. Expected user ID: 22222222-2222-2222-2222-222222222201.");
    }

    private static async Task NormalizeLegacyAuthorAsync(ApplicationDbContext context)
    {
        var legacyRecipes = await context.Recipes
            .Where(recipe => recipe.AuthorId == LegacyAuthorUserId)
            .ToListAsync();

        if (legacyRecipes.Count == 0)
        {
            return;
        }

        foreach (var recipe in legacyRecipes)
        {
            recipe.AuthorId = AuthorUserId;
        }

        await context.SaveChangesAsync();
    }

    private static async Task RemoveLegacyAuthorIfUnusedAsync(ApplicationDbContext context)
    {
        var legacyAuthorIsUsed = await context.Recipes
            .AnyAsync(recipe => recipe.AuthorId == LegacyAuthorUserId);

        if (legacyAuthorIsUsed)
        {
            return;
        }

        var legacyAuthor = await context.Users
            .FirstOrDefaultAsync(user => user.Id == LegacyAuthorUserId);

        if (legacyAuthor is null)
        {
            return;
        }

        context.Users.Remove(legacyAuthor);
        await context.SaveChangesAsync();
    }

    private static void AddIngredients(
        Recipe recipe,
        Guid recipeId,
        int recipeNumber,
        int ingredientCount,
        Faker faker)
    {
        var ingredientNames = faker.Random.ListItems(IngredientNames, ingredientCount);

        for (var orderIndex = 1; orderIndex <= ingredientCount; orderIndex++)
        {
            recipe.Ingredients.Add(new RecipeIngredient
            {
                Id = CreateIngredientId(recipeNumber, orderIndex),
                RecipeId = recipeId,
                Name = ingredientNames[orderIndex - 1],
                Quantity = Round(faker.Random.Decimal(0.5m, 900m)),
                Unit = faker.PickRandom(Units),
                Notes = faker.Random.Bool(0.35f) ? faker.Lorem.Sentence(6) : null,
                OrderIndex = orderIndex
            });
        }
    }

    private static void AddSteps(Recipe recipe, Guid recipeId, int recipeNumber, int stepCount, Faker faker)
    {
        for (var stepNumber = 1; stepNumber <= stepCount; stepNumber++)
        {
            recipe.Steps.Add(new RecipeStep
            {
                Id = CreateStepId(recipeNumber, stepNumber),
                RecipeId = recipeId,
                StepNumber = stepNumber,
                Title = StepTitles[Math.Min(stepNumber - 1, StepTitles.Length - 1)],
                Description = faker.Lorem.Paragraph(),
                TimerMinutes = faker.Random.Bool(0.7f) ? faker.Random.Int(3, 25) : null,
                ImageUrl = faker.Random.Bool(0.4f)
                    ? $"https://picsum.photos/seed/recipe-step-{recipeNumber:D3}-{stepNumber}/900/600"
                    : null
            });
        }
    }

    private static void AddImage(Recipe recipe, Guid recipeId, int recipeNumber)
    {
        recipe.Images.Add(new RecipeImage
        {
            Id = CreateImageId(recipeNumber),
            RecipeId = recipeId,
            OriginalUrl = $"https://picsum.photos/seed/recipe-{recipeNumber:D3}/1200/900",
            MediumUrl = $"https://picsum.photos/seed/recipe-{recipeNumber:D3}/800/600",
            ThumbnailUrl = $"https://picsum.photos/seed/recipe-{recipeNumber:D3}/300/300",
            IsPrimary = true,
            OrderIndex = 1
        });
    }

    private static decimal Round(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static Guid CreateRecipeId(int recipeNumber)
    {
        return Guid.Parse($"33333333-3333-3333-3333-{recipeNumber:D12}");
    }

    private static Guid CreateIngredientId(int recipeNumber, int orderIndex)
    {
        return Guid.Parse($"44444444-4444-4444-4444-{recipeNumber * 100 + orderIndex:D12}");
    }

    private static Guid CreateStepId(int recipeNumber, int stepNumber)
    {
        return Guid.Parse($"55555555-5555-5555-5555-{recipeNumber * 100 + stepNumber:D12}");
    }

    private static Guid CreateImageId(int recipeNumber)
    {
        return Guid.Parse($"66666666-6666-6666-6666-{recipeNumber:D12}");
    }
}
