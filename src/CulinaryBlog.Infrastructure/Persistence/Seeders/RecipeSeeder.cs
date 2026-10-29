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

    private const string SupabaseStorageBaseUrl = "https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/";

    private sealed record DishTemplate(string Title, string ImageUrl);

    private static readonly DishTemplate[] DefaultTemplates =
    [
        new("Món ngon gia đình thanh đạm", SupabaseStorageBaseUrl + "com-chien.jpg")
    ];

    /// <summary>
    /// Ma trận phân loại món ăn chuẩn ngữ nghĩa (Semantic Category Mapping).
    /// Ánh xạ chính xác Slug danh mục ẩm thực sang các món ăn thực tế và ảnh tương ứng trên Supabase Storage CDN.
    /// </summary>
    private static readonly Dictionary<string, DishTemplate[]> CategoryDishMap = new()
    {
        ["mon-khai-vi"] =
        [
            new("Gỏi cuốn tôm thịt thanh đạm", SupabaseStorageBaseUrl + "goi-cuon.jpg"),
            new("Nem rán giòn truyền thống", SupabaseStorageBaseUrl + "cha-gio.jpg"),
            new("Salad ức gà xé phay", SupabaseStorageBaseUrl + "goi-ga.jpg")
        ],
        ["mon-canh-sup"] =
        [
            new("Canh chua cá lóc Nam Bộ", SupabaseStorageBaseUrl + "canh-chua-ca-loc.jpg"),
            new("Cháo gà nấm hương nóng hổi", SupabaseStorageBaseUrl + "chao-ga.jpg"),
            new("Canh sườn hầm rau củ", SupabaseStorageBaseUrl + "canh-chua-ca-loc.jpg")
        ],
        ["mon-kho-rim"] =
        [
            new("Cá kho tộ nước dừa", SupabaseStorageBaseUrl + "ca-kho-to.jpg"),
            new("Thịt kho trứng kiểu miền Nam", SupabaseStorageBaseUrl + "thit-kho-trung.jpg"),
            new("Tôm rim mặn ngọt đưa cơm", SupabaseStorageBaseUrl + "tom-rim.jpg")
        ],
        ["mon-xao-ap-chao"] =
        [
            new("Bò lúc lắc sốt tiêu đen", SupabaseStorageBaseUrl + "bo-luc-lac.jpg"),
            new("Mực xào sa tế giòn ngọt", SupabaseStorageBaseUrl + "muc-xao.jpg"),
            new("Cá hồi áp chảo sốt bơ tỏi", SupabaseStorageBaseUrl + "tom-rim.jpg"),
            new("Rau muống xào tỏi xanh mướt", SupabaseStorageBaseUrl + "rau-muong-xao-toi.jpg")
        ],
        ["mon-chien-ran"] =
        [
            new("Nem rán giòn Hà Nội", SupabaseStorageBaseUrl + "cha-gio.jpg"),
            new("Bánh xèo miền Tây giòn rụm", SupabaseStorageBaseUrl + "banh-xeo.jpg"),
            new("Cánh gà chiên nước mắm", SupabaseStorageBaseUrl + "ga-nuong.jpg")
        ],
        ["mon-nuong-bbq"] =
        [
            new("Gà nướng mật ong sả ớt", SupabaseStorageBaseUrl + "ga-nuong.jpg"),
            new("Bún thịt nướng than hoa", SupabaseStorageBaseUrl + "bun-thit-nuong.jpg"),
            new("Sườn heo nướng mật ong", SupabaseStorageBaseUrl + "suon-xao-chua-ngot.jpg")
        ],
        ["mon-hap-luoc"] =
        [
            new("Gà ta hấp lá chanh", SupabaseStorageBaseUrl + "ga-luoc-la-chanh.jpg"),
            new("Tôm sú hấp nước dừa", SupabaseStorageBaseUrl + "tom-rim.jpg"),
            new("Thịt ba chỉ luộc chấm mắm tỏi", SupabaseStorageBaseUrl + "thit-kho-trung.jpg")
        ],
        ["bun-mi-pho"] =
        [
            new("Phở bò tái nạm Hà Nội", SupabaseStorageBaseUrl + "pho-bo.jpg"),
            new("Bún bò Huế sa tế đậm đà", SupabaseStorageBaseUrl + "bun-bo-hue.jpg"),
            new("Mì Quảng gà ta chính gốc", SupabaseStorageBaseUrl + "mi-quang.jpg"),
            new("Bún thịt nướng chả giò", SupabaseStorageBaseUrl + "bun-thit-nuong.jpg")
        ],
        ["com-xoi"] =
        [
            new("Cơm tấm sườn bì chả Sài Gòn", SupabaseStorageBaseUrl + "com-tam.jpg"),
            new("Xôi gấc đậu xanh dẻo thơm", SupabaseStorageBaseUrl + "xoi-gac.jpg"),
            new("Cơm chiên hải sản hoàng kim", SupabaseStorageBaseUrl + "com-chien.jpg")
        ],
        ["mon-cuon-goi"] =
        [
            new("Gỏi cuốn tôm thịt chấm tương đen", SupabaseStorageBaseUrl + "goi-cuon.jpg"),
            new("Gỏi gà bắp cải hành tây", SupabaseStorageBaseUrl + "goi-ga.jpg"),
            new("Gỏi ngó sen tôm thịt chua ngọt", SupabaseStorageBaseUrl + "goi-cuon.jpg")
        ],
        ["mon-lau-nong-hoi"] =
        [
            new("Lẩu thái hải sản chua cay", SupabaseStorageBaseUrl + "lau-hai-san.jpg"),
            new("Lẩu gà nấm thanh ngọt", SupabaseStorageBaseUrl + "chao-ga.jpg"),
            new("Lẩu riêu cua bắp bò", SupabaseStorageBaseUrl + "lau-hai-san.jpg")
        ],
        ["hai-san-bien"] =
        [
            new("Mực xào sa tế cay nồng", SupabaseStorageBaseUrl + "muc-xao.jpg"),
            new("Cá hồi áp chảo sốt chanh leo", SupabaseStorageBaseUrl + "tom-rim.jpg"),
            new("Tôm rim mặn ngọt miền Trung", SupabaseStorageBaseUrl + "tom-rim.jpg"),
            new("Chả cá thì là Hà Thành", SupabaseStorageBaseUrl + "cha-ca-la-vong.jpg")
        ],
        ["mon-bo-dam-vi"] =
        [
            new("Bò lúc lắc sốt tiêu đen mềm ngon", SupabaseStorageBaseUrl + "bo-luc-lac.jpg"),
            new("Bò sốt vang bánh mì", SupabaseStorageBaseUrl + "bo-luc-lac.jpg"),
            new("Phở bò tái lăn áp chảo", SupabaseStorageBaseUrl + "pho-bo.jpg")
        ],
        ["mon-heo-gia-dinh"] =
        [
            new("Thịt kho trứng cút kiểu miền Nam", SupabaseStorageBaseUrl + "thit-kho-trung.jpg"),
            new("Sườn xào chua ngọt chuẩn vị", SupabaseStorageBaseUrl + "suon-xao-chua-ngot.jpg"),
            new("Thịt ba chỉ luộc mắm nêm", SupabaseStorageBaseUrl + "thit-kho-trung.jpg")
        ],
        ["mon-ga-vit"] =
        [
            new("Gà nướng mật ong sả ớt thơm lừng", SupabaseStorageBaseUrl + "ga-nuong.jpg"),
            new("Vịt om sấu chua thanh Hà Nội", SupabaseStorageBaseUrl + "ga-luoc-la-chanh.jpg"),
            new("Gỏi gà bắp cải hoa chuối", SupabaseStorageBaseUrl + "goi-ga.jpg")
        ],
        ["mon-chay-thanh-tinh"] =
        [
            new("Đậu hũ sốt cà chua nấm đông cô", SupabaseStorageBaseUrl + "dau-hu-sot-ca.jpg"),
            new("Rau muống xào tỏi chay thanh nhẹ", SupabaseStorageBaseUrl + "rau-muong-xao-toi.jpg"),
            new("Nấm kho tiêu chay đưa cơm", SupabaseStorageBaseUrl + "dau-hu-sot-ca.jpg")
        ],
        ["an-vat-duong-pho"] =
        [
            new("Bánh xèo giòn rụm đường phố", SupabaseStorageBaseUrl + "banh-xeo.jpg"),
            new("Bánh mì que pate giòn tan", SupabaseStorageBaseUrl + "banh-mi.jpg"),
            new("Nem chua rán giòn rụm", SupabaseStorageBaseUrl + "cha-gio.jpg")
        ],
        ["banh-mi-sandwich"] =
        [
            new("Bánh mì pate trứng ốp la", SupabaseStorageBaseUrl + "banh-mi.jpg"),
            new("Bánh mì xíu mại sốt cà chua", SupabaseStorageBaseUrl + "banh-mi.jpg"),
            new("Bánh mì kẹp thịt nướng giòn bì", SupabaseStorageBaseUrl + "banh-mi.jpg")
        ],
        ["banh-ngot-banh-nuong"] =
        [
            new("Bánh flan cà phê caramel mềm mịn", SupabaseStorageBaseUrl + "banh-flan.jpg"),
            new("Bánh bông lan trứng muối chà bông", SupabaseStorageBaseUrl + "banh-flan.jpg"),
            new("Bánh chuối nướng nước cốt dừa", SupabaseStorageBaseUrl + "banh-flan.jpg")
        ],
        ["che-trang-mieng"] =
        [
            new("Chè đậu xanh nha đam thanh nhiệt", SupabaseStorageBaseUrl + "che-dau-xanh.jpg"),
            new("Bánh flan caramen cà phê", SupabaseStorageBaseUrl + "banh-flan.jpg"),
            new("Chè bưởi nước cốt dừa An Giang", SupabaseStorageBaseUrl + "che-dau-xanh.jpg")
        ],
        ["do-uong-sinh-to"] =
        [
            new("Sinh tố bơ sữa đặc béo ngậy", SupabaseStorageBaseUrl + "sinh-to-bo.jpg"),
            new("Sinh tố xoài sữa chua mát lạnh", SupabaseStorageBaseUrl + "sinh-to-bo.jpg"),
            new("Nước ép dưa hấu bạc hà", SupabaseStorageBaseUrl + "sinh-to-bo.jpg")
        ],
        ["tra-ca-phe"] =
        [
            new("Cà phê sữa đá pha phin đậm đà", SupabaseStorageBaseUrl + "ca-phe-sua-da.jpg"),
            new("Cà phê đen đá nguyên chất", SupabaseStorageBaseUrl + "ca-phe-sua-da.jpg"),
            new("Trà sen vàng hạt sen nhãn nhục", SupabaseStorageBaseUrl + "ca-phe-sua-da.jpg")
        ],
        ["nuoc-cham-gia-vi"] =
        [
            new("Nước mắm tỏi ớt chua ngọt chuẩn vị", SupabaseStorageBaseUrl + "cha-gio.jpg"),
            new("Muối ớt xanh chấm hải sản nha trang", SupabaseStorageBaseUrl + "muc-xao.jpg"),
            new("Sốt me chấm bánh tráng chua cay", SupabaseStorageBaseUrl + "goi-cuon.jpg")
        ],
        ["am-thuc-mien-bac"] =
        [
            new("Phở bò tái chín phố cổ Hà Nội", SupabaseStorageBaseUrl + "pho-bo.jpg"),
            new("Vịt om sấu chua thanh xứ Bắc", SupabaseStorageBaseUrl + "ga-luoc-la-chanh.jpg"),
            new("Chả cá Lã Vọng thì là hành hoa", SupabaseStorageBaseUrl + "cha-ca-la-vong.jpg")
        ],
        ["am-thuc-mien-nam"] =
        [
            new("Cơm tấm sườn bì chả Sài Thành", SupabaseStorageBaseUrl + "com-tam.jpg"),
            new("Canh chua cá lóc bông điên điển", SupabaseStorageBaseUrl + "canh-chua-ca-loc.jpg"),
            new("Bánh xèo tôm nhảy miền Tây", SupabaseStorageBaseUrl + "banh-xeo.jpg")
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
                ImageUrl = null
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
