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
    // Tổng số công thức mẫu cần có trong cơ sở dữ liệu.
    private const int TargetRecipeCount = 120;

    // NormalizedName của vai trò Author trong ASP.NET Core Identity.
    private const string AuthorRoleName = "AUTHOR";

    private sealed record DishTemplate(string Title, string ImageUrl);

    private static readonly DishTemplate[] DefaultTemplates =
    [
        new("Món ngon gia đình thanh đạm", "https://images.unsplash.com/photo-1546069901-ba9599a7e63c")
    ];

    /// <summary>
    /// Ma trận phân loại món ăn chuẩn ngữ nghĩa (Semantic Category Mapping).
    /// Ánh xạ chính xác Slug danh mục ẩm thực sang các món ăn thực tế và ảnh tương ứng.
    /// </summary>
    private static readonly Dictionary<string, DishTemplate[]> CategoryDishMap = new()
    {
        ["mon-khai-vi"] =
        [
            new("Gỏi cuốn tôm thịt thanh đạm", "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb"),
            new("Nem rán giòn truyền thống", "https://images.unsplash.com/photo-1541544741938-0af808871cc0"),
            new("Salad ức gà xé phay", "https://images.unsplash.com/photo-1540420773420-3366772f4999")
        ],
        ["mon-canh-sup"] =
        [
            new("Canh chua cá lóc Nam Bộ", "https://images.unsplash.com/photo-1547592166-23ac45744acd"),
            new("Cháo gà nấm hương nóng hổi", "https://images.unsplash.com/photo-1547592166-23ac45744acd"),
            new("Canh sườn hầm rau củ", "https://images.unsplash.com/photo-1547592166-23ac45744acd")
        ],
        ["mon-kho-rim"] =
        [
            new("Cá kho tộ nước dừa", "https://images.unsplash.com/photo-1544025162-d76694265947"),
            new("Thịt kho trứng kiểu miền Nam", "https://images.unsplash.com/photo-1544025162-d76694265947"),
            new("Tôm rim mặn ngọt đưa cơm", "https://images.unsplash.com/photo-1534939561126-855b8675edd7")
        ],
        ["mon-xao-ap-chao"] =
        [
            new("Bò lúc lắc sốt tiêu đen", "https://images.unsplash.com/photo-1558030006-450675393462"),
            new("Mực xào sa tế giòn ngọt", "https://images.unsplash.com/photo-1534939561126-855b8675edd7"),
            new("Cá hồi áp chảo sốt bơ tỏi", "https://images.unsplash.com/photo-1467003909585-2f8a72700288"),
            new("Rau muống xào tỏi xanh mướt", "https://images.unsplash.com/photo-1540420773420-3366772f4999")
        ],
        ["mon-chien-ran"] =
        [
            new("Nem rán giòn Hà Nội", "https://images.unsplash.com/photo-1541544741938-0af808871cc0"),
            new("Bánh xèo miền Tây giòn rụm", "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8"),
            new("Cánh gà chiên nước mắm", "https://images.unsplash.com/photo-1562967914-608f82629710")
        ],
        ["mon-nuong-bbq"] =
        [
            new("Gà nướng mật ong sả ớt", "https://images.unsplash.com/photo-1598515214211-89d3c73ae83b"),
            new("Bún thịt nướng than hoa", "https://images.unsplash.com/photo-1559847844-5315695dadae"),
            new("Sườn heo nướng mật ong", "https://images.unsplash.com/photo-1544025162-d76694265947")
        ],
        ["mon-hap-luoc"] =
        [
            new("Gà ta hấp lá chanh", "https://images.unsplash.com/photo-1587593810167-a84920ea0781"),
            new("Tôm sú hấp nước dừa", "https://images.unsplash.com/photo-1534939561126-855b8675edd7"),
            new("Thịt ba chỉ luộc chấm mắm tỏi", "https://images.unsplash.com/photo-1544025162-d76694265947")
        ],
        ["bun-mi-pho"] =
        [
            new("Phở bò tái nạm Hà Nội", "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43"),
            new("Bún bò Huế sa tế đậm đà", "https://images.unsplash.com/photo-1569058242253-92a9c755a0ec"),
            new("Mì Quảng gà ta chính gốc", "https://images.unsplash.com/photo-1569718212165-3a8278d5f624"),
            new("Bún thịt nướng chả giò", "https://images.unsplash.com/photo-1559847844-5315695dadae")
        ],
        ["com-xoi"] =
        [
            new("Cơm tấm sườn bì chả Sài Gòn", "https://images.unsplash.com/photo-1504674900247-0877df9cc836"),
            new("Xôi gấc đậu xanh dẻo thơm", "https://images.unsplash.com/photo-1509722747041-616f39b57569"),
            new("Cơm chiên hải sản hoàng kim", "https://images.unsplash.com/photo-1536304993881-ff6e9eefa2a6")
        ],
        ["mon-cuon-goi"] =
        [
            new("Gỏi cuốn tôm thịt chấm tương đen", "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb"),
            new("Gỏi gà bắp cải hành tây", "https://images.unsplash.com/photo-1540420773420-3366772f4999"),
            new("Gỏi ngó sen tôm thịt chua ngọt", "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb")
        ],
        ["mon-lau-nong-hoi"] =
        [
            new("Lẩu thái hải sản chua cay", "https://images.unsplash.com/photo-1547496502-affa22d38842"),
            new("Lẩu gà nấm thanh ngọt", "https://images.unsplash.com/photo-1547592166-23ac45744acd"),
            new("Lẩu riêu cua bắp bò", "https://images.unsplash.com/photo-1569718212165-3a8278d5f624")
        ],
        ["hai-san-bien"] =
        [
            new("Mực xào sa tế cay nồng", "https://images.unsplash.com/photo-1534939561126-855b8675edd7"),
            new("Cá hồi áp chảo sốt chanh leo", "https://images.unsplash.com/photo-1467003909585-2f8a72700288"),
            new("Tôm rim mặn ngọt miền Trung", "https://images.unsplash.com/photo-1534939561126-855b8675edd7"),
            new("Chả cá thì là Hà Thành", "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb")
        ],
        ["mon-bo-dam-vi"] =
        [
            new("Bò lúc lắc sốt tiêu đen mềm ngon", "https://images.unsplash.com/photo-1558030006-450675393462"),
            new("Bò sốt vang bánh mì", "https://images.unsplash.com/photo-1558030006-450675393462"),
            new("Phở bò tái lăn áp chảo", "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43")
        ],
        ["mon-heo-gia-dinh"] =
        [
            new("Thịt kho trứng cút kiểu miền Nam", "https://images.unsplash.com/photo-1544025162-d76694265947"),
            new("Sườn xào chua ngọt chuẩn vị", "https://images.unsplash.com/photo-1544025162-d76694265947"),
            new("Thịt ba chỉ luộc mắm nêm", "https://images.unsplash.com/photo-1544025162-d76694265947")
        ],
        ["mon-ga-vit"] =
        [
            new("Gà nướng mật ong sả ớt thơm lừng", "https://images.unsplash.com/photo-1598515214211-89d3c73ae83b"),
            new("Vịt om sấu chua thanh Hà Nội", "https://images.unsplash.com/photo-1587593810167-a84920ea0781"),
            new("Gỏi gà bắp cải hoa chuối", "https://images.unsplash.com/photo-1540420773420-3366772f4999")
        ],
        ["mon-chay-thanh-tinh"] =
        [
            new("Đậu hũ sốt cà chua nấm đông cô", "https://images.unsplash.com/photo-1546069901-ba9599a7e63c"),
            new("Rau muống xào tỏi chay thanh nhẹ", "https://images.unsplash.com/photo-1540420773420-3366772f4999"),
            new("Nấm kho tiêu chay đưa cơm", "https://images.unsplash.com/photo-1546069901-ba9599a7e63c")
        ],
        ["an-vat-duong-pho"] =
        [
            new("Bánh xèo giòn rụm đường phố", "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8"),
            new("Bánh mì que pate giòn tan", "https://images.unsplash.com/photo-1626804475297-41608ea09aeb"),
            new("Nem chua rán giòn rụm", "https://images.unsplash.com/photo-1541544741938-0af808871cc0")
        ],
        ["banh-mi-sandwich"] =
        [
            new("Bánh mì pate trứng ốp la", "https://images.unsplash.com/photo-1626804475297-41608ea09aeb"),
            new("Bánh mì xíu mại sốt cà chua", "https://images.unsplash.com/photo-1626804475297-41608ea09aeb"),
            new("Bánh mì kẹp thịt nướng giòn bì", "https://images.unsplash.com/photo-1626804475297-41608ea09aeb")
        ],
        ["banh-ngot-banh-nuong"] =
        [
            new("Bánh flan cà phê caramel mềm mịn", "https://images.unsplash.com/photo-1578985545062-69928b1d9587"),
            new("Bánh bông lan trứng muối chà bông", "https://images.unsplash.com/photo-1578985545062-69928b1d9587"),
            new("Bánh chuối nướng nước cốt dừa", "https://images.unsplash.com/photo-1578985545062-69928b1d9587")
        ],
        ["che-trang-mieng"] =
        [
            new("Chè đậu xanh nha đam thanh nhiệt", "https://images.unsplash.com/photo-1551024709-8f23befc6f87"),
            new("Bánh flan caramen cà phê", "https://images.unsplash.com/photo-1578985545062-69928b1d9587"),
            new("Chè bưởi nước cốt dừa An Giang", "https://images.unsplash.com/photo-1551024709-8f23befc6f87")
        ],
        ["do-uong-sinh-to"] =
        [
            new("Sinh tố bơ sữa đặc béo ngậy", "https://images.unsplash.com/photo-1553530666-ba11a7da3888"),
            new("Sinh tố xoài sữa chua mát lạnh", "https://images.unsplash.com/photo-1553530666-ba11a7da3888"),
            new("Nước ép dưa hấu bạc hà", "https://images.unsplash.com/photo-1553530666-ba11a7da3888")
        ],
        ["tra-ca-phe"] =
        [
            new("Cà phê sữa đá pha phin đậm đà", "https://images.unsplash.com/photo-1509042239860-f550ce710b93"),
            new("Cà phê đen đá nguyên chất", "https://images.unsplash.com/photo-1509042239860-f550ce710b93"),
            new("Trà sen vàng hạt sen nhãn nhục", "https://images.unsplash.com/photo-1509042239860-f550ce710b93")
        ],
        ["nuoc-cham-gia-vi"] =
        [
            new("Nước mắm tỏi ớt chua ngọt chuẩn vị", "https://images.unsplash.com/photo-1544025162-d76694265947"),
            new("Muối ớt xanh chấm hải sản nha trang", "https://images.unsplash.com/photo-1544025162-d76694265947"),
            new("Sốt me chấm bánh tráng chua cay", "https://images.unsplash.com/photo-1544025162-d76694265947")
        ],
        ["am-thuc-mien-bac"] =
        [
            new("Phở bò tái chín phố cổ Hà Nội", "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43"),
            new("Vịt om sấu chua thanh xứ Bắc", "https://images.unsplash.com/photo-1587593810167-a84920ea0781"),
            new("Chả cá Lã Vọng thì là hành hoa", "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb")
        ],
        ["am-thuc-mien-nam"] =
        [
            new("Cơm tấm sườn bì chả Sài Thành", "https://images.unsplash.com/photo-1504674900247-0877df9cc836"),
            new("Canh chua cá lóc bông điên điển", "https://images.unsplash.com/photo-1547592166-23ac45744acd"),
            new("Bánh xèo tôm nhảy miền Tây", "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8")
        ]
    };

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

    private static readonly string[] StepImageUrls =
    [
        "https://images.unsplash.com/photo-1556910103-1c02745aae4d",
        "https://images.unsplash.com/photo-1506368249639-73a05d6f6488",
        "https://images.unsplash.com/photo-1556911220-e15b29be8c8f",
        "https://images.unsplash.com/photo-1547592180-85f173990554",
        "https://images.unsplash.com/photo-1546069901-ba9599a7e63c"
    ];

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Lấy các tài khoản Author do UserSeeder tạo để gán làm tác giả công thức.
        var authorIds = await GetAuthorIdsAsync(context);

        // Đếm số công thức hiện có để khi chạy lại chương trình không tạo trùng thêm 120 bản ghi.
        var existingRecipeCount = await context.Recipes.CountAsync();
        if (existingRecipeCount >= TargetRecipeCount)
        {
            return;
        }

        // Lấy danh mục do CategorySeeder tạo và sinh công thức theo ánh xạ ngữ nghĩa chính xác.
        var categories = await GetCategoriesAsync(context);
        var recipes = CreateRecipes(existingRecipeCount, categories, authorIds);

        await context.Recipes.AddRangeAsync(recipes);
        await context.SaveChangesAsync();
    }

    private static async Task<List<Category>> GetCategoriesAsync(ApplicationDbContext context)
    {
        var categories = await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.OrderIndex)
            .ToListAsync();

        if (categories.Count > 0)
        {
            return categories;
        }

        // Nếu chưa có danh mục, chạy CategorySeeder trước rồi truy vấn lại.
        await CategorySeeder.SeedAsync(context);

        return await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.OrderIndex)
            .ToListAsync();
    }

    private static List<Recipe> CreateRecipes(
        int existingRecipeCount,
        IReadOnlyList<Category> categories,
        IReadOnlyList<string> authorIds)
    {
        // Seed cố định theo MSSV để các lần tạo mới cho kết quả ngẫu nhiên có thể tái lập.
        Randomizer.Seed = new Random(2312802);
        var faker = new Faker("vi");

        // Ví dụ đã có 20 công thức thì chỉ tạo thêm 100 để đạt tổng cộng 120.
        var recipesToCreate = TargetRecipeCount - existingRecipeCount;
        var recipes = new List<Recipe>(recipesToCreate);

        for (var offset = 1; offset <= recipesToCreate; offset++)
        {
            var recipeNumber = existingRecipeCount + offset;
            var recipeId = CreateRecipeId(recipeNumber);

            // Phân bổ đều và tuần tự công thức cho 25 danh mục ẩm thực
            var category = categories[(recipeNumber - 1) % categories.Count];

            // Tra cứu danh sách món ăn ngữ nghĩa chuẩn theo Slug của danh mục
            var templates = CategoryDishMap.TryGetValue(category.Slug, out var mappedTemplates) && mappedTemplates.Length > 0
                ? mappedTemplates
                : DefaultTemplates;

            // Chọn món ăn tương ứng trong danh mục để đảm bảo không bị trùng lặp liên tiếp
            var templateIndex = ((recipeNumber - 1) / categories.Count) % templates.Length;
            var selectedDish = templates[templateIndex];

            var baseTitle = selectedDish.Title;
            var title = $"{baseTitle} #{recipeNumber:D3}";
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
                // Gán CategoryId theo đúng danh mục ngữ nghĩa của món ăn
                CategoryId = category.Id,
                AuthorId = authorIds[(recipeNumber - 1) % authorIds.Count],
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
            AddImage(recipe, recipeId, recipeNumber, selectedDish.ImageUrl);

            recipes.Add(recipe);
        }

        return recipes;
    }

    private static async Task<List<string>> GetAuthorIdsAsync(ApplicationDbContext context)
    {
        // Tìm ID của vai trò Author từ bảng AspNetRoles.
        var authorRoleId = await context.Roles
            .Where(role => role.NormalizedName == AuthorRoleName)
            .Select(role => role.Id)
            .SingleOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(authorRoleId))
        {
            throw new InvalidOperationException(
                "Author role from UserSeeder is missing. Run UserSeeder before RecipeSeeder.");
        }

        // Join AspNetUserRoles với AspNetUsers để lấy mọi Author đang hoạt động.
        // Nhờ vậy khi có thêm Author mới, RecipeSeeder vẫn tự động sử dụng được.
        var authorIds = await context.UserRoles
            .Where(userRole => userRole.RoleId == authorRoleId)
            .Join(
                context.Users.Where(user => user.IsActive),
                userRole => userRole.UserId,
                user => user.Id,
                (userRole, user) => user.Id)
            .OrderBy(userId => userId)
            .ToListAsync();

        if (authorIds.Count > 0)
        {
            return authorIds;
        }

        throw new InvalidOperationException(
            "No active Author users found. Run UserSeeder before RecipeSeeder.");
    }

    private static void AddIngredients(
        Recipe recipe,
        Guid recipeId,
        int recipeNumber,
        int ingredientCount,
        Faker faker)
    {
        // Mỗi công thức có từ 10 đến 14 nguyên liệu, đúng yêu cầu bài tập.
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
        // Mỗi công thức có từ 5 đến 8 bước và StepNumber tăng liên tục từ 1.
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
                    ? $"{StepImageUrls[(stepNumber - 1) % StepImageUrls.Length]}?w=900&auto=format&fit=crop&q=80"
                    : null
            });
        }
    }

    private static void AddImage(Recipe recipe, Guid recipeId, int recipeNumber, string baseImageUrl)
    {
        // Tạo một ảnh chính từ kho ảnh Unsplash chuẩn xác theo từng món ăn
        recipe.Images.Add(new RecipeImage
        {
            Id = CreateImageId(recipeNumber),
            RecipeId = recipeId,
            OriginalUrl = $"{baseImageUrl}?w=1200&auto=format&fit=crop&q=80",
            MediumUrl = $"{baseImageUrl}?w=800&auto=format&fit=crop&q=80",
            ThumbnailUrl = $"{baseImageUrl}?w=300&auto=format&fit=crop&q=80",
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
        // Dùng GUID xác định để dễ nhận biết và tránh thay đổi ID giữa các lần tạo lại dữ liệu.
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
