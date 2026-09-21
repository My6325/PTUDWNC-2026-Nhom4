using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence.Seeders;

/// <summary>
/// Lớp tạo dữ liệu mẫu cho Danh mục Ẩm thực (Category Seeder)
/// Đảm bảo sinh tối thiểu 20 danh mục ẩm thực thực tế, chuẩn SEO và thân thiện với văn hóa ẩm thực Việt Nam.
/// </summary>
public static class CategorySeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Cơ chế Idempotent: Nếu đã có danh mục trong CSDL thì bỏ qua, không nạp trùng lặp
        if (await context.Categories.AnyAsync())
        {
            return;
        }

        var categories = new List<Category>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                Name = "Món Khai Vị",
                Slug = SlugHelper.GenerateSlug("Món Khai Vị"),
                Description = "Các món khai vị thanh nhẹ kích thích vị giác trước bữa ăn chính.",
                ImageUrl = "https://images.unsplash.com/photo-1541544741938-0af808871cc0",
                OrderIndex = 1
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                Name = "Món Canh & Súp",
                Slug = SlugHelper.GenerateSlug("Món Canh & Súp"),
                Description = "Tổng hợp các món canh đậm đà, súp nóng hổi bồi bổ sức khỏe cho cả gia đình.",
                ImageUrl = "https://images.unsplash.com/photo-1547592166-23ac45744acd",
                OrderIndex = 2
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111103"),
                Name = "Món Kho & Rim",
                Slug = SlugHelper.GenerateSlug("Món Kho & Rim"),
                Description = "Hương vị truyền thống với các món kho tộ, cá kho, thịt kho đậm đà đưa cơm.",
                ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947",
                OrderIndex = 3
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111104"),
                Name = "Món Xào & Áp Chảo",
                Slug = SlugHelper.GenerateSlug("Món Xào & Áp Chảo"),
                Description = "Các món xào giòn ngon, giữ trọn dưỡng chất và màu sắc tươi tắn của nguyên liệu.",
                ImageUrl = "https://images.unsplash.com/photo-1512058564366-18510be2db19",
                OrderIndex = 4
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111105"),
                Name = "Món Chiên & Rán",
                Slug = SlugHelper.GenerateSlug("Món Chiên & Rán"),
                Description = "Những món chiên giòn rụm bên ngoài, mọng nước đậm vị bên trong hấp dẫn mọi lứa tuổi.",
                ImageUrl = "https://images.unsplash.com/photo-1562967914-608f82629710",
                OrderIndex = 5
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111106"),
                Name = "Món Nướng BBQ",
                Slug = SlugHelper.GenerateSlug("Món Nướng BBQ"),
                Description = "Công thức ướp thịt nướng, hải sản nướng thơm lừng chuẩn vị nhà hàng và tiệc ngoài trời.",
                ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1",
                OrderIndex = 6
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111107"),
                Name = "Món Hấp & Luộc",
                Slug = SlugHelper.GenerateSlug("Món Hấp & Luộc"),
                Description = "Phương pháp nấu ăn thanh đạm, ít dầu mỡ, giữ trọn vị ngọt tự nhiên của thực phẩm.",
                ImageUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999",
                OrderIndex = 7
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111108"),
                Name = "Bún, Mì & Phở",
                Slug = SlugHelper.GenerateSlug("Bún, Mì & Phở"),
                Description = "Tinh hoa ẩm thực nước với phở bò Hà Nội, bún bò Huế, mì Quảng và hủ tiếu trứ danh.",
                ImageUrl = "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43",
                OrderIndex = 8
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111109"),
                Name = "Cơm & Xôi",
                Slug = SlugHelper.GenerateSlug("Cơm & Xôi"),
                Description = "Các món cơm chiên, cơm tấm, xôi vò, xôi gấc mang đậm bản sắc văn hóa lúa nước.",
                ImageUrl = "https://images.unsplash.com/photo-1536304993881-ff6e9eefa2a6",
                OrderIndex = 9
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111110"),
                Name = "Món Cuốn & Gỏi",
                Slug = SlugHelper.GenerateSlug("Món Cuốn & Gỏi"),
                Description = "Gỏi ngó sen, gỏi cuốn tôm thịt giòn ngọt chấm cùng nước chấm chua cay đặc trưng.",
                ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb",
                OrderIndex = 10
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Món Lẩu Nóng Hổi",
                Slug = SlugHelper.GenerateSlug("Món Lẩu Nóng Hổi"),
                Description = "Tuyển tập các món lẩu thái, lẩu hải sản, lẩu nấm cho những buổi sum họp ấm áp.",
                ImageUrl = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624",
                OrderIndex = 11
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111112"),
                Name = "Hải Sản Biển",
                Slug = SlugHelper.GenerateSlug("Hải Sản Biển"),
                Description = "Các công thức chế biến tôm, cua, mực, cá biển tươi ngon giữ trọn hương vị của đại dương.",
                ImageUrl = "https://images.unsplash.com/photo-1534939561126-855b8675edd7",
                OrderIndex = 12
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111113"),
                Name = "Món Bò Đậm Vị",
                Slug = SlugHelper.GenerateSlug("Món Bò Đậm Vị"),
                Description = "Bò sốt vang, bò bít tết, bò lúc lắc giàu protein cho bữa ăn giàu năng lượng.",
                ImageUrl = "https://images.unsplash.com/photo-1558030006-450675393462",
                OrderIndex = 13
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111114"),
                Name = "Món Heo Gia Đình",
                Slug = SlugHelper.GenerateSlug("Món Heo Gia Đình"),
                Description = "Các món sườn xào chua ngọt, thịt kho tàu, thịt ba chỉ nướng giòn bì quen thuộc.",
                ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947",
                OrderIndex = 14
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111115"),
                Name = "Món Gà & Vịt",
                Slug = SlugHelper.GenerateSlug("Món Gà & Vịt"),
                Description = "Gà luộc lá chanh, gà rán giòn, vịt om sấu thơm lừng hấp dẫn mọi thực khách.",
                ImageUrl = "https://images.unsplash.com/photo-1587593810167-a84920ea0781",
                OrderIndex = 15
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111116"),
                Name = "Món Chay Thanh Tịnh",
                Slug = SlugHelper.GenerateSlug("Món Chay Thanh Tịnh"),
                Description = "Thực đơn ăn chay lành mạnh từ rau củ, đậu hũ và nấm thơm ngon bổ dưỡng.",
                ImageUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999",
                OrderIndex = 16
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111117"),
                Name = "Ăn Vặt & Đường Phố",
                Slug = SlugHelper.GenerateSlug("Ăn Vặt & Đường Phố"),
                Description = "Bánh tráng nướng, nem chua rán, ốc luộc và những món ăn vặt được giới trẻ yêu thích.",
                ImageUrl = "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8",
                OrderIndex = 17
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111118"),
                Name = "Bánh Mì & Sandwich",
                Slug = SlugHelper.GenerateSlug("Bánh Mì & Sandwich"),
                Description = "Bánh mì kẹp pate Việt Nam và các loại bánh sandwich tiện lợi cho bữa sáng nhanh gọn.",
                ImageUrl = "https://images.unsplash.com/photo-1509722747041-616f39b57569",
                OrderIndex = 18
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111119"),
                Name = "Bánh Ngọt & Bánh Nướng",
                Slug = SlugHelper.GenerateSlug("Bánh Ngọt & Bánh Nướng"),
                Description = "Bánh bông lan, tiramisu, cookie ngọt ngào dành cho những tín đồ mê làm bánh.",
                ImageUrl = "https://images.unsplash.com/photo-1578985545062-69928b1d9587",
                OrderIndex = 19
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111120"),
                Name = "Chè & Tráng Miệng",
                Slug = SlugHelper.GenerateSlug("Chè & Tráng Miệng"),
                Description = "Chè bưởi, chè khúc bạch, flan caramen ngọt mát giải nhiệt mùa hè.",
                ImageUrl = "https://images.unsplash.com/photo-1551024709-8f23befc6f87",
                OrderIndex = 20
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111121"),
                Name = "Đồ Uống & Sinh Tố",
                Slug = SlugHelper.GenerateSlug("Đồ Uống & Sinh Tố"),
                Description = "Nước ép hoa quả, sinh tố bơ mọng nước bổ sung vitamin và khoáng chất tự nhiên.",
                ImageUrl = "https://images.unsplash.com/photo-1553530666-ba11a7da3888",
                OrderIndex = 21
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111122"),
                Name = "Trà & Cà Phê",
                Slug = SlugHelper.GenerateSlug("Trà & Cà Phê"),
                Description = "Cà phê sữa đá, trà sen vàng, matcha đá xay giúp tinh thần sảng khoái mỗi ngày.",
                ImageUrl = "https://images.unsplash.com/photo-1509042239860-f550ce710b93",
                OrderIndex = 22
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111123"),
                Name = "Nước Chấm & Gia Vị",
                Slug = SlugHelper.GenerateSlug("Nước Chấm & Gia Vị"),
                Description = "Bí quyết pha nước mắm chua ngọt, sốt me, sốt chấm hải sản linh hồn của món ăn.",
                ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947",
                OrderIndex = 23
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111124"),
                Name = "Ẩm Thực Miền Bắc",
                Slug = SlugHelper.GenerateSlug("Ẩm Thực Miền Bắc"),
                Description = "Hương vị thanh tao, tinh tế lưu giữ nét đẹp văn hóa ngàn năm Thăng Long - Hà Nội.",
                ImageUrl = "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43",
                OrderIndex = 24
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111125"),
                Name = "Ẩm Thực Miền Nam",
                Slug = SlugHelper.GenerateSlug("Ẩm Thực Miền Nam"),
                Description = "Món ăn phóng khoáng, đậm đà vị ngọt béo của nước cốt dừa và sản vật sông nước miệt vườn.",
                ImageUrl = "https://images.unsplash.com/photo-1536304993881-ff6e9eefa2a6",
                OrderIndex = 25
            }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }
}
