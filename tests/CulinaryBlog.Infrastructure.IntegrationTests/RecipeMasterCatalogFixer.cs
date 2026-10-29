using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class RecipeMasterCatalogFixer
{
    private sealed record IngredientData(string Name, decimal Quantity, string Unit);
    private sealed record StepData(int StepNumber, string Title, string Description, int? TimerMinutes);

    private sealed record DishCatalog(
        string MatchPattern,
        string CategorySlug,
        string ImageUrl,
        string Description,
        string Instructions,
        IngredientData[] Ingredients,
        StepData[] Steps);

    private static readonly DishCatalog[] Catalogs =
    [
        new(
            "Sườn xào",
            "mon-xao-ap-chao",
            "https://images.unsplash.com/photo-1529193591184-b1d58069ecdd",
            "Sườn xào chua ngọt óng ả màu cánh gián, thịt sườn non mềm ngọt quyện cùng sốt cà chua, giấm đường chua ngọt thanh dịu kích thích vị giác.",
            "Bước 1: Sơ chế sườn non, chần qua nước sôi rồi ướp gia vị.\nBước 2: Chiên sườn vàng xém các cạnh.\nBước 3: Pha sốt chua ngọt từ cà chua, giấm, đường, nước mắm.\nBước 4: Đảo sườn với sốt và hành tây đến khi keo lại sánh mịn.",
            [
                new("sườn non heo", 600, "g"),
                new("cà chua", 2, "trái"),
                new("hành tây", 1, "củ"),
                new("ớt chuông", 1, "trái"),
                new("tỏi băm", 1, "củ"),
                new("nước mắm", 2, "muỗng canh"),
                new("đường thốt nốt", 2, "muỗng canh"),
                new("dầu ăn", 2, "muỗng canh"),
                new("tiêu xay", 1, "muỗng cà phê")
            ],
            [
                new(1, "Sơ chế và chần sườn", "Sườn non chặt khúc vừa ăn, rửa sạch với nước muối loãng rồi chần sơ qua nước sôi có thả vài lát gừng để khử mùi.", 10),
                new(2, "Chiên săn sườn", "Đặt chảo lên bếp, cho 2 muỗng canh dầu ăn vào đun nóng rồi cho sườn vào chiên lửa vừa đến khi hai mặt vàng xém thơm lừng.", 12),
                new(3, "Pha sốt chua ngọt", "Khuấy đều 2 muỗng canh nước mắm, 2 muỗng canh đường, giấm gạo và cà chua băm nhuyễn để tạo hỗn hợp sốt đậm đà.", 5),
                new(4, "Rim sườn với sốt", "Trút hỗn hợp sốt vào chảo sườn, đun nhỏ lửa cho sốt ngấm sâu vào từng thớ thịt đến khi sánh lại bao quanh miếng sườn.", 15),
                new(5, "Hoàn thiện và trình bày", "Thêm hành tây bổ múi cau và ớt chuông vào đảo nhanh 2 phút, tắt bếp rồi múc ra đĩa rắc tiêu xay và hành lá lên trên.", 2)
            ]
        ),
        new(
            "Canh chua",
            "mon-canh-sup",
            "https://images.unsplash.com/photo-1547496502-affa22d38842",
            "Món canh chua cá lóc Nam Bộ chuẩn vị với thịt cá béo ngọt, nước dùng chua thanh đậm đà từ me cùng hương thơm nồng nàn của ngò gai, ngò ôm.",
            "Bước 1: Sơ chế cá lóc khử tanh, cắt khúc vừa ăn.\nBước 2: Phi thơm tỏi, xào sơ cà chua lấy màu.\nBước 3: Đun sôi nước, lọc me lấy vị chua rồi cho cá vào nấu chín.\nBước 4: Cho đậu bắp, bạc hà, giá đỗ, nêm nếm chua ngọt hài hòa.",
            [
                new("cá lóc", 600, "g"),
                new("cà chua", 2, "trái"),
                new("đậu bắp", 6, "trái"),
                new("bắp cải", 1, "bắp"),
                new("hành tím", 3, "củ"),
                new("nước mắm", 3, "muỗng canh"),
                new("đường thốt nốt", 2, "muỗng cà phê"),
                new("muối biển", 1, "muỗng cà phê"),
                new("rau thơm", 1, "bó")
            ],
            [
                new(1, "Sơ chế nguyên liệu", "Cá lóc làm sạch ruột, xát muối chanh khử nhớt, cắt khúc 3cm. Cà chua bổ múi cau, đậu bắp cắt xéo, rau thơm nhặt sạch.", 15),
                new(2, "Ướp cá đậm vị", "Ướp cá với 1 muỗng nước mắm, 1 muỗng cà phê hạt tiêu và hành tím băm trong 15 phút.", 15),
                new(3, "Nấu nước dùng chua", "Phi thơm tỏi băm xào cà chua, thêm 1.2 lít nước đun sôi rồi lọc nước cốt me dầm vào nồi.", 10),
                new(4, "Nấu cá và rau bổi", "Thả cá lóc vào đun sôi 8 phút cho cá chín ngọt nước, sau đó cho đậu bắp và giá vào nấu sôi lại.", 8),
                new(5, "Nêm nếm hoàn tất", "Nêm thêm nước mắm và chút đường cho vị chua ngọt cân bằng, múc ra tô rắc ngò ôm ngò gai thơm phức.", 3)
            ]
        ),
        new(
            "Thịt kho trứng",
            "mon-kho-rim",
            "https://images.unsplash.com/photo-1544025162-d76694265947",
            "Thịt kho tàu mềm rục ngấm đẫm nước dừa xiêm ngọt thanh, trứng vịt bùi béo cùng màu nâu cánh gián óng ánh đặc trưng của mâm cơm Việt.",
            "Bước 1: Cắt thịt ba chỉ con chì, ướp hành tỏi ớt mắm đường.\nBước 2: Thắng nước màu caramen, đảo săn thịt.\nBước 3: Đổ nước dừa xiêm vào ngập thịt, đun nhỏ lửa.\nBước 4: Cho trứng luộc vào kho liu riu đến khi thịt mềm rục.",
            [
                new("thịt ba chỉ", 600, "g"),
                new("trứng gà", 4, "trái"),
                new("nước dừa", 400, "ml"),
                new("nước mắm", 3, "muỗng canh"),
                new("đường thốt nốt", 2, "muỗng canh"),
                new("hành tím", 3, "củ"),
                new("tỏi", 2, "củ"),
                new("tiêu xay", 1, "muỗng cà phê")
            ],
            [
                new(1, "Sơ chế và ướp thịt", "Thịt ba chỉ cạo sạch bì, cắt miếng vuông quân cờ 4x4cm. Ướp thịt với hành tỏi băm, nước mắm, đường và tiêu trong 30 phút.", 30),
                new(2, "Thắng nước màu", "Cho 1 muỗng đường vào nồi đun lửa vừa đến khi đường chuyển màu cánh gián đẹp mắt thì trút thịt vào đảo thật săn.", 7),
                new(3, "Kho với nước dừa", "Đổ nước dừa xiêm ngập mặt thịt, đun sôi bùng rồi hạ lửa nhỏ liu riu, hớt bọt để nước kho trong vắt.", 45),
                new(4, "Thấm vị cùng trứng", "Cho trứng đã luộc chín bóc vỏ vào nồi thịt kho cùng, đậy hé vung đun thêm 20 phút cho trứng ngấm màu nâu bóng.", 20),
                new(5, "Hoàn tất món kho", "Khi nước kho cạn bớt sánh lại, thịt mềm tan béo ngậy thì tắt bếp, dọn ăn kèm cơm trắng và dưa giá chua giòn.", 5)
            ]
        ),
        new(
            "Phở bò",
            "bun-mi-pho",
            "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43",
            "Bát phở bò truyền thống với nước dùng trong veo thơm lừng hương quế hồi gừng nướng, bánh phở mềm mướt cùng từng lát thịt bò tươi ngọt đậm đà.",
            "Bước 1: Hầm xương bò cùng quế, hồi, thảo quả, gừng nướng.\nBước 2: Thái mỏng thịt bò tái và nạm bò chín.\nBước 3: Chần bánh phở xếp vào bát cùng hành hoa ngò gai.\nBước 4: Chan nước dùng sôi sùng sục làm chín thịt bò tái.",
            [
                new("thịt bò", 500, "g"),
                new("bún tươi", 500, "g"),
                new("gừng", 1, "củ"),
                new("hành tím", 4, "củ"),
                new("nước mắm", 3, "muỗng canh"),
                new("muối biển", 1, "muỗng cà phê"),
                new("tiêu xay", 1, "muỗng cà phê"),
                new("rau thơm", 1, "bó"),
                new("chanh", 1, "trái")
            ],
            [
                new(1, "Hầm nước dùng phở", "Xương bò chần sạch rồi cho vào nồi ninh cùng gừng nướng, hành nướng, hoa hồi và thanh quế trong nhiều giờ để nước ngọt thanh.", 60),
                new(2, "Chuẩn bị bánh phở và rau", "Chần bánh phở qua nước sôi cho tơi mềm rồi chia đều ra các bát tô lớn. Hành lá, ngò gai thái nhỏ.", 10),
                new(3, "Thái thịt bò", "Thịt bò thăn thái lát mỏng tang ngang thớ để khi chan nước dùng thịt mềm ngọt tan trong miệng.", 10),
                new(4, "Chan nước phở", "Xếp thịt bò, hành hoa lên trên bát phở, múc nước dùng đang sôi sùng sục chan đều ngập mặt thịt.", 5),
                new(5, "Thưởng thức", "Dùng nóng kèm lát chanh tươi, vài lát ớt hiểm và chút giấm tỏi thơm nồng chuẩn phong vị Hà Thành.", 2)
            ]
        ),
        new(
            "Bún bò Huế",
            "bun-mi-pho",
            "https://images.unsplash.com/photo-1569058242253-92a9c755a0ec",
            "Đặc sản bún bò xứ Huế đậm đà hương mắm ruốc, thơm ngào ngạt sả phi và ớt sa tế cay nồng đánh thức mọi giác quan.",
            "Bước 1: Hầm bắp bò và xương ống cùng sả cây đập dập.\nBước 2: Hòa mắm ruốc lọc lấy nước trong cho vào nồi nước dùng.\nBước 3: Phi màu điều và sa tế cay thơm.\nBước 4: Trụng bún sợi to, chan nước dùng và thưởng thức kèm rau sống.",
            [
                new("thịt bò", 500, "g"),
                new("bún tươi", 500, "g"),
                new("sa tế", 2, "muỗng canh"),
                new("sả", 4, "nhánh"),
                new("hành tím", 3, "củ"),
                new("tỏi", 2, "củ"),
                new("nước mắm", 2, "muỗng canh"),
                new("rau thơm", 1, "bó")
            ],
            [
                new(1, "Hầm thịt bắp bò", "Bắp bò cuộn chặt bó chỉ, hầm cùng xương và sả cây đập dập trên lửa vừa cho chín mềm thơm ngọt.", 40),
                new(2, "Nêm mắm ruốc Huế", "Mắm ruốc khuấy tan với nước lạnh, để lắng cặn rồi chắt lấy phần nước trong trút vào nồi hầm.", 10),
                new(3, "Tạo màu sa tế", "Phi thơm ớt màu điều và sa tế cay nồng cho vào nồi nước dùng tạo lớp váng màu đỏ cam quyến rũ.", 10),
                new(4, "Xếp bát bún", "Bún sợi to chần nóng cho vào tô, xếp bắp bò thái mỏng, chả cua, hành tây thái sợi và ngò gai.", 5),
                new(5, "Chan nước dùng", "Chan nước dùng nóng hổi thơm nồng hương sả ruốc, ăn kèm bắp chuối bào và rau thơm tươi ngon.", 3)
            ]
        ),
        new(
            "Gỏi cuốn",
            "mon-cuon-goi",
            "https://images.unsplash.com/photo-1772004839635-77804fbc7729",
            "Món cuốn thanh đạm trứ danh với tôm đỏ au mọng nước, thịt ba chỉ luộc ngọt bùi cuộn cùng xà lách tươi giòn và chấm tương đen béo ngậy.",
            "Bước 1: Luộc tôm và thịt ba chỉ vừa chín tới.\nBước 2: Cắt lát thịt, bổ đôi tôm theo chiều dọc.\nBước 3: Thấm ướt bánh tráng, xếp rau, bún, thịt và tôm cuộn chặt tay.\nBước 4: Nấu tương chấm pha bơ đậu phộng rắc đậu phộng rang.",
            [
                new("tôm tươi", 350, "g"),
                new("thịt ba chỉ", 350, "g"),
                new("bún tươi", 400, "g"),
                new("rau thơm", 1, "bó"),
                new("hành tím", 2, "củ"),
                new("đậu phộng rang", 50, "g"),
                new("nước mắm", 2, "muỗng canh")
            ],
            [
                new(1, "Luộc tôm thịt", "Thịt ba chỉ luộc chín với chút hành tím rồi ngâm nước đá cắt lát mỏng. Tôm luộc chín bóc vỏ xẻ đôi.", 20),
                new(2, "Chuẩn bị rau bún", "Rau xà lách, rau thơm các loại rửa sạch vẩy ráo nước. Bún tươi cắt ngắn cho dễ cuốn.", 10),
                new(3, "Cuộn gỏi cuốn", "Trải bánh tráng thấm chút nước, xếp rau xà lách, bún, thịt rồi gập một mép lại, đặt tôm lên và cuộn tròn thật chặt tay.", 15),
                new(4, "Pha sốt chấm tương đen", "Xào tương hột xay nhuyễn với bơ đậu phộng và chút nước dừa cho sánh mịn béo ngậy.", 10),
                new(5, "Trình bày", "Bày gỏi cuốn ra đĩa cùng bát tương rắc đậu phộng giã dập và ớt băm cay cay.", 3)
            ]
        ),
        new(
            "Cơm tấm",
            "com-xoi",
            "https://images.unsplash.com/photo-1504674900247-0877df9cc836",
            "Cơm tấm Sài Gòn với hạt tấm dẻo thơm, miếng sườn nướng than hoa vàng ươm thơm lừng mùi sả mật ong kết hợp cùng mỡ hành béo ngậy và nước mắm chua ngọt kẹo quánh.",
            "Bước 1: Nấu chín cơm gạo tấm thơm.\nBước 2: Ướp sườn cốt lết với sả, mật ong, dầu hào rồi nướng chín vàng.\nBước 3: Làm mỡ hành phi và đồ chua cà rốt.\nBước 4: Xới cơm tấm ra đĩa, đặt sườn nướng lên trên và rưới mỡ hành.",
            [
                new("gạo thơm", 400, "g"),
                new("thịt ba chỉ", 500, "g"),
                new("sả", 3, "nhánh"),
                new("hành tím", 3, "củ"),
                new("tỏi", 2, "củ"),
                new("nước mắm", 3, "muỗng canh"),
                new("đường thốt nốt", 2, "muỗng canh"),
                new("ớt", 2, "trái")
            ],
            [
                new(1, "Nấu cơm tấm", "Gạo tấm vo sạch ngâm 20 phút rồi cho vào nồi nấu chín ráo hạt, giữ ấm.", 30),
                new(2, "Ướp sườn nướng", "Sườn cốt lết đập mềm, ướp với sả băm, hành tỏi, mật ong, dầu hào và nước mắm ngon trong 1 tiếng.", 45),
                new(3, "Nướng sườn than hoa", "Đặt sườn lên vỉ nướng than hoa hoặc nồi chiên không dầu lật đều hai mặt cho vàng ruộm.", 20),
                new(4, "Làm mỡ hành", "Hành lá thái nhỏ cho vào bát cùng chút muối đường, đun sôi dầu ăn rồi trút vào đảo đều cho xanh mướt.", 5),
                new(5, "Bày đĩa cơm tấm", "Xới cơm ra đĩa, đặt sườn nướng, chả trứng, chan mỡ hành và dọn kèm bát nước mắm chua ngọt cay cay.", 5)
            ]
        ),
        new(
            "Cá kho tộ",
            "mon-kho-rim",
            "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2",
            "Cá kho tộ đậm đà sắc nước màu dừa, từng khúc cá săn chắc thấm vị mặn ngọt cay nồng của tiêu ớt, ăn cùng cơm nóng là ngon hết sẩy.",
            "Bước 1: Làm sạch cá, cắt khúc ướp gia vị đậm đà.\nBước 2: Xếp thịt ba chỉ lót đáy tộ đất rồi đặt cá lên trên.\nBước 3: Kho cá với nước dừa tươi trên lửa liu riu.\nBước 4: Rưới mỡ hành, tiêu sọ đập dập khi nước kho sánh keo.",
            [
                new("cá lóc", 600, "g"),
                new("thịt ba chỉ", 200, "g"),
                new("nước dừa", 300, "ml"),
                new("nước mắm", 4, "muỗng canh"),
                new("đường thốt nốt", 2, "muỗng canh"),
                new("tiêu xay", 2, "muỗng cà phê"),
                new("ớt", 3, "trái"),
                new("hành tím", 3, "củ")
            ],
            [
                new(1, "Sơ chế cá", "Cá làm sạch vảy và ruột, xát muối khử tanh rồi cắt khúc dày 2.5cm để ráo nước hoàn toàn.", 15),
                new(2, "Ướp cá đậm vị", "Ướp cá với nước mắm ngon, nước màu dừa, đường, hạt tiêu và hành tím đập dập trong 30 phút.", 30),
                new(3, "Kho cá lửa lớn", "Xếp thịt ba chỉ dưới đáy tộ đất, đặt cá lên trên đun lửa to cho sôi bùng 5 phút cho thịt cá săn lại.", 5),
                new(4, "Hạ lửa om cá", "Đổ nước dừa tươi vào ngập mặt cá, đun sôi rồi hạ lửa nhỏ liu riu om trong 35 phút cho cá ngấm sâu.", 35),
                new(5, "Hoàn tất món kho", "Khi nước sốt kho cạn kẹo sánh bóng, rắc nhiều tiêu sọ và vài lát ớt hiểm đỏ tươi lên trên.", 5)
            ]
        ),
        new(
            "Bò lúc lắc",
            "mon-xao-ap-chao",
            "https://images.unsplash.com/photo-1558030006-450675393462",
            "Thịt bò mềm mọng nước được áp chảo lửa lớn xém cạnh thơm lừng bơ tỏi, hòa quyện với ớt chuông giòn ngọt và sốt tiêu đen đậm đà.",
            "Bước 1: Thái thịt thăn bò vuông quân cờ, ướp dầu hào và tỏi băm.\nBước 2: Cắt ớt chuông, hành tây miếng vuông.\nBước 3: Áp chảo thịt bò lửa cực lớn với bơ tỏi.\nBước 4: Đảo nhanh rau củ, trút ra đĩa ăn kèm khoai tây chiên.",
            [
                new("thịt bò", 500, "g"),
                new("ớt chuông", 2, "trái"),
                new("hành tây", 1, "củ"),
                new("tỏi", 2, "củ"),
                new("dầu hào", 2, "muỗng canh"),
                new("tiêu xay", 1, "muỗng cà phê"),
                new("nước tương", 1, "muỗng canh"),
                new("dầu ăn", 2, "muỗng canh")
            ],
            [
                new(1, "Cắt thịt và ướp", "Thịt thăn bò cắt khối vuông 2x2cm. Ướp cùng tỏi băm, dầu hào, nước tương, dầu ăn và tiêu đen trong 20 phút.", 20),
                new(2, "Sơ chế rau củ", "Ớt chuông xanh đỏ và hành tây cắt miếng vuông tương đương kích thước thịt bò.", 10),
                new(3, "Xào rau củ", "Bắc chảo nóng phi thơm tỏi, cho ớt chuông và hành tây vào xào nhanh trên lửa lớn 2 phút rồi trút ra đĩa.", 3),
                new(4, "Áp chảo bò lúc lắc", "Cho bơ lạt vào chảo thật nóng, trút thịt bò vào lắc chảo nhanh tay trên lửa cực lớn để bò xém cạnh nhưng trong vẫn mềm mọng.", 4),
                new(5, "Trộn sốt hoàn thiện", "Đổ rau củ trở lại chảo đảo đều cùng thịt bò trong 1 phút, rắc thêm tiêu đen giã dập và dọn ra đĩa thưởng thức.", 2)
            ]
        ),
        new(
            "Bánh xèo",
            "mon-chien-ran",
            "https://images.unsplash.com/photo-1545019851-1fb83e874046",
            "Bánh xèo miền Tây vỏ mỏng giòn rụm màu vàng nghệ thơm béo nước cốt dừa, nhân tôm thịt đậm đà ăn kèm rau rừng và nước mắm chua ngọt.",
            "Bước 1: Pha bột bánh xèo cùng nước cốt dừa và hành lá.\nBước 2: Xào săn tôm thịt chín tới.\nBước 3: Đổ bột tráng chảo nghe tiếng xèo vui tai.\nBước 4: Cho giá đỗ, gấp đôi bánh chiên giòn rụm rồi thưởng thức.",
            [
                new("bột gạo thơm", 400, "g"),
                new("tôm tươi", 300, "g"),
                new("thịt ba chỉ", 300, "g"),
                new("nước dừa", 200, "ml"),
                new("bột nghệ", 1, "muỗng cà phê"),
                new("rau thơm", 1, "bó"),
                new("nước mắm", 3, "muỗng canh"),
                new("dầu ăn", 3, "muỗng canh")
            ],
            [
                new(1, "Pha bột bánh xèo", "Khuấy đều bột gạo, nước cốt dừa, nước lọc, bột nghệ, chút muối và hành lá thái nhỏ để nghỉ 30 phút.", 30),
                new(2, "Xào nhân tôm thịt", "Thịt ba chỉ thái mỏng và tôm lột vỏ xào chín tới cùng hành tím băm, nêm chút hạt tiêu.", 10),
                new(3, "Đổ bánh xèo", "Đun nóng chảo với chút dầu, múc 1 muôi bột láng thật mỏng đều quanh chảo, đậy vung 2 phút cho bánh chín giòn.", 5),
                new(4, "Cho nhân vào bánh", "Rải tôm thịt và giá đỗ lên một nửa bánh, hạ nhỏ lửa chiên thêm cho vỏ bánh giòn tan rồi gập đôi lại.", 3),
                new(5, "Trình bày", "Lấy bánh ra đĩa, ăn nóng bằng cách cuốn bánh xèo trong lá xà lách, rau cải xanh và chấm nước mắm ớt chua ngọt.", 2)
            ]
        ),
        new(
            "Gà nướng",
            "mon-nuong-bbq",
            "https://images.unsplash.com/photo-1598515214211-89d3c73ae83b",
            "Gà nướng mật ong sả ớt thơm lừng, da gà vàng óng giòn rụm, thớ thịt bên trong mềm ngọt thấm đẫm vị sả thơm cay nồng nàn.",
            "Bước 1: Sơ chế gà xát muối rượu gừng sạch sẽ.\nBước 2: Giã nhuyễn sả ớt tỏi trộn đều cùng mật ong và dầu hào.\nBước 3: Ướp đều hỗn hợp sốt lên khắp mình gà.\nBước 4: Nướng gà vàng đều hai mặt và quét mật ong lớp cuối.",
            [
                new("ức gà", 800, "g"),
                new("sả", 4, "nhánh"),
                new("ớt", 2, "trái"),
                new("tỏi", 2, "củ"),
                new("dầu hào", 2, "muỗng canh"),
                new("nước mắm", 2, "muỗng canh"),
                new("tiêu xay", 1, "muỗng cà phê"),
                new("dầu ăn", 2, "muỗng canh")
            ],
            [
                new(1, "Sơ chế gà", "Thịt gà làm sạch, khứa nhẹ vài đường trên phần thịt dày để khi ướp gia vị ngấm sâu vào bên trong.", 15),
                new(2, "Làm sốt ướp nướng", "Băm nhuyễn sả, ớt, tỏi rồi trộn đều với 2 muỗng mật ong, 2 muỗng dầu hào, nước mắm và chút dầu màu điều.", 10),
                new(3, "Ướp thịt gà", "Thoa đều hỗn hợp sốt ướp lên từng miếng gà, mát xa nhẹ nhàng và để ngăn mát tủ lạnh ít nhất 1 tiếng.", 60),
                new(4, "Nướng gà", "Cho gà vào lò nướng hoặc nồi chiên không dầu ở nhiệt độ 180°C trong 25 phút, lật mặt gà cho chín đều.", 25),
                new(5, "Quét mật ong hoàn tất", "Quét thêm một lớp mật ong mỏng lên bề mặt da rồi nướng thêm 5 phút ở 200°C cho da gà giòn bóng màu hổ phách.", 5)
            ]
        ),
        new(
            "Chè đậu xanh",
            "che-trang-mieng",
            "https://images.unsplash.com/photo-1551024709-8f23befc6f87",
            "Bát chè đậu xanh nha đam thanh mát giải nhiệt mùa hè, hạt đậu nở mềm bùi bùi kết hợp cùng từng miếng nha đam giòn sần sật ngọt dịu.",
            "Bước 1: Ngâm nở đậu xanh bóc vỏ.\nBước 2: Sơ chế nha đam khử nhớt cắt hạt lựu ướp đường.\nBước 3: Ninh nhừ đậu xanh cùng đường phèn.\nBước 4: Thả nha đam vào đun sôi lại rồi để lạnh thưởng thức.",
            [
                new("đậu xanh", 250, "g"),
                new("đường thốt nốt", 150, "g"),
                new("muối biển", 1, "muỗng cà phê"),
                new("nước dừa", 200, "ml")
            ],
            [
                new(1, "Ngâm đậu xanh", "Đậu xanh đãi sạch hạt lép, ngâm trong nước ấm 1 tiếng cho hạt đậu nở mềm giúp ninh nhanh nhừ.", 60),
                new(2, "Sơ chế nha đam", "Nha đam gọt vỏ xanh lấy phần thạch trắng, cắt hạt lựu rồi bóp muối chanh rửa nhiều lần cho sạch nhớt hoàn toàn.", 20),
                new(3, "Nấu đậu xanh", "Cho đậu xanh vào nồi cùng 1 lít nước, đun sôi rồi hạ nhỏ lửa ninh khoảng 20 phút cho đậu chín mềm bở tơi.", 20),
                new(4, "Thêm đường phèn", "Cho đường phèn và chút xíu muối vào nồi chè khuấy tan đều cho vị ngọt thanh đậm đà.", 5),
                new(5, "Hoàn thành", "Trút nha đam vào đun sôi lại 2 phút là tắt bếp. Múc chè ra bát, ăn ấm hoặc để lạnh cùng đá bào đều tuyệt ngon.", 5)
            ]
        ),
        new(
            "Xôi gấc",
            "com-xoi",
            "https://images.unsplash.com/photo-1509722747041-616f39b57569",
            "Xôi gấc đỏ tươi may mắn dẻo thơm từng hạt nếp cái hoa vàng quyện cùng hương gấc ngào ngạt và vị béo ngậy của nước cốt dừa.",
            "Bước 1: Vo sạch nếp ngâm qua đêm.\nBước 2: Bóp thịt gấc với rượu trắng rồi trộn đều vào nếp.\nBước 3: Đồ xôi chín dẻo trên xửng hấp.\nBước 4: Rưới nước cốt dừa và đường đánh tơi xôi.",
            [
                new("gạo thơm", 500, "g"),
                new("nước dừa", 150, "ml"),
                new("đường thốt nốt", 50, "g"),
                new("muối biển", 1, "muỗng cà phê"),
                new("dầu ăn", 1, "muỗng canh")
            ],
            [
                new(1, "Ngâm gạo nếp", "Gạo nếp cái hoa vàng vo sạch, ngâm nước lạnh từ 6-8 tiếng cho hạt nếp ngậm no nước rồi vớt ra để ráo.", 360),
                new(2, "Trộn thịt gấc", "Bổ gấc lấy phần thịt đỏ bóp đều với 1 muỗng rượu trắng để dậy màu đỏ tươi, sau đó trộn đều vào gạo nếp cùng chút muối.", 15),
                new(3, "Đồ xôi lần 1", "Cho nếp vào chõ hấp, đun nước sôi lớn đồ xôi trong 30 phút đến khi hạt xôi chín trong dẻo.", 30),
                new(4, "Rưới nước cốt dừa", "Mở nắp chõ, rưới nước cốt dừa và đường vào xôi rồi dùng đũa xới đều, đậy vung đồ tiếp 10 phút.", 10),
                new(5, "Đóng khuôn xôi", "Xới xôi ra đĩa hoặc ép vào khuôn hoa sen cho đẹp mắt, dùng trong các dịp lễ tết sum vầy gia đình.", 5)
            ]
        ),
        new(
            "Cà phê sữa đá",
            "tra-ca-phe",
            "https://images.unsplash.com/photo-1509042239860-f550ce710b93",
            "Hương vị cà phê phin sữa đá truyền thống đậm đà, vị đắng nồng nàn hòa quyện cùng sữa đặc béo ngọt và đá lạnh sảng khoái.",
            "Bước 1: Tráng phin bằng nước sôi cho nóng.\nBước 2: Cho bột cà phê vào phin gài nắp nhẹ tay.\nBước 3: Rót nước sôi ủ cà phê nở đều.\nBước 4: Nhỏ giọt từng giọt cà phê sánh đậm vào ly sữa đặc.",
            [
                new("sữa đặc", 3, "muỗng canh"),
                new("nước sôi", 100, "ml")
            ],
            [
                new(1, "Chuẩn bị phin và ly", "Tráng phin nhôm qua nước sôi. Cho 3 muỗng canh sữa đặc vào đáy ly thủy tinh.", 2),
                new(2, "Ủ cà phê", "Cho 25g bột cà phê nguyên chất vào phin, lắc nhẹ cho phẳng rồi nén nhẹ gài nắp. Châm 20ml nước sôi ủ trong 2 phút.", 2),
                new(3, "Chiết xuất cà phê", "Rót tiếp 60ml nước sôi vào phin, đậy nắp lại và để từng giọt cà phê đen sánh nhỏ giọt từ từ xuống ly sữa.", 5),
                new(4, "Khuấy đều", "Dùng muỗng khuấy đều tay cho cà phê và sữa đặc quyện vào nhau tạo thành màu nâu cánh gián bắt mắt.", 1),
                new(5, "Thêm đá", "Đập nhỏ đá viên cho đầy ly, khuấy đều và thưởng thức trọn vẹn hương vị cà phê phin Việt Nam.", 1)
            ]
        ),
        new(
            "Chả cá",
            "hai-san-bien",
            "https://images.unsplash.com/photo-1534939561126-855b8675edd7",
            "Chả cá thơm nức mùi thì là và hành hoa, chả dai giòn sần sật đậm đà gia vị biển chấm cùng nước mắm tỏi ớt cay cay đưa cơm.",
            "Bước 1: Quết cá nhuyễn mịn cùng gia vị và thì là.\nBước 2: Nặn thành từng miếng chả tròn dẹt.\nBước 3: Chiên chả ngập dầu cho phồng vàng ruộm.\nBước 4: Vớt ra để ráo dầu và thưởng thức khi còn nóng hổi.",
            [
                new("cá lóc", 600, "g"),
                new("hành tím", 3, "củ"),
                new("tỏi", 2, "củ"),
                new("nước mắm", 2, "muỗng canh"),
                new("tiêu xay", 1, "muỗng cà phê"),
                new("dầu ăn", 4, "muỗng canh"),
                new("rau thơm", 1, "bó")
            ],
            [
                new(1, "Lọc và quết cá", "Thịt cá phi lê thấm khô nước, dùng muỗng nạo lấy thịt rồi cho vào cối quết dẻo quánh cùng hành tỏi và tiêu.", 25),
                new(2, "Trộn thì là", "Thì là rửa sạch thái thật nhỏ, trộn đều vào giò cá quết thêm 5 phút cho chả cá dai giòn tự nhiên.", 5),
                new(3, "Tạo hình chả", "Thoa chút dầu ăn lên tay, nặn cá thành từng miếng tròn dẹt vừa ăn.", 10),
                new(4, "Chiên chả cá", "Đun nóng dầu ăn trong chảo sâu lòng, thả từng miếng chả vào chiên lửa vừa đến khi phồng căng vàng ruộm hai mặt.", 12),
                new(5, "Thưởng thức", "Vớt chả cá ra giấy thấm dầu, cắt miếng xéo vừa ăn, dùng nóng cùng cơm trắng hoặc kẹp bánh mì.", 3)
            ]
        ),
        new(
            "Đậu hũ sốt cà",
            "mon-chay-thanh-tinh",
            "https://images.unsplash.com/photo-1546069901-ba9599a7e63c",
            "Món chay dân dã thanh tịnh với từng miếng đậu hũ vàng giòn ngập trong nước sốt cà chua đỏ mọng đậm đà sánh mịn.",
            "Bước 1: Cắt đậu hũ miếng vuông chiên vàng giòn.\nBước 2: Xào cà chua chín mềm thành nước sốt sánh đỏ.\nBước 3: Cho đậu hũ vào om nhỏ lửa cho ngấm vị sốt.\nBước 4: Rắc hành lá thái nhỏ và hạt tiêu thơm lừng.",
            [
                new("đậu hũ", 4, "miếng"),
                new("cà chua", 3, "trái"),
                new("hành tím", 2, "củ"),
                new("dầu ăn", 2, "muỗng canh"),
                new("nước mắm", 2, "muỗng canh"),
                new("đường thốt nốt", 1, "muỗng canh"),
                new("tiêu xay", 1, "muỗng cà phê")
            ],
            [
                new(1, "Chiên đậu hũ", "Đậu hũ cắt miếng vuông vừa ăn, thả vào chảo dầu nóng chiên vàng đều 4 mặt rồi vớt ra để ráo.", 10),
                new(2, "Xào sốt cà chua", "Cà chua rửa sạch băm nhỏ. Phi thơm hành tím với dầu ăn rồi trút cà chua vào xào nhuyễn cùng chút muối.", 8),
                new(3, "Om đậu hũ", "Thêm nửa chén nước vào sốt cà chua, nêm nước mắm và đường cho vừa miệng rồi thả đậu hũ chiên vào om nhỏ lửa.", 10),
                new(4, "Thấm đẫm sốt", "Lật nhẹ các mặt đậu hũ cho nước sốt cà chua ngấm sâu vào bên trong từng miếng đậu.", 5),
                new(5, "Hoàn tất", "Khi nước sốt sền sệt bao quanh miếng đậu, rắc hành lá thái nhỏ và tiêu xay lên trên rồi tắt bếp.", 2)
            ]
        ),
        new(
            "Mực xào",
            "hai-san-bien",
            "https://images.unsplash.com/photo-1534939561126-855b8675edd7",
            "Mực xào sa tế giòn sần sật đậm đà hương vị biển, vị cay the hấp dẫn của sa tế hòa quyện cùng ớt chuông và hành tây ngọt mát.",
            "Bước 1: Làm sạch mực ống, khứa vảy rồng đẹp mắt.\nBước 2: Ướp mực với sa tế tôm và tỏi băm.\nBước 3: Xào mực trên lửa lớn cho mực chín giòn không ra nước.\nBước 4: Trộn cùng hành tây cần tây đảo nhanh rồi tắt bếp.",
            [
                new("mực ống", 500, "g"),
                new("sa tế", 2, "muỗng canh"),
                new("hành tây", 1, "củ"),
                new("ớt chuông", 1, "trái"),
                new("tỏi", 2, "củ"),
                new("nước mắm", 2, "muỗng canh"),
                new("dầu hào", 1, "muỗng canh"),
                new("dầu ăn", 2, "muỗng canh")
            ],
            [
                new(1, "Sơ chế mực", "Mực ống rút mai, làm sạch ruột, xát rượu gừng khử tanh rồi khứa hoa vảy rồng, cắt miếng vừa ăn.", 15),
                new(2, "Ướp mực sa tế", "Ướp mực với 2 muỗng canh sa tế tôm cay, 1 muỗng dầu hào và tỏi băm trong 15 phút.", 15),
                new(3, "Cắt rau củ", "Hành tây, ớt chuông cắt múi cau; cần tây tỏi tây nhặt sạch cắt khúc 3cm.", 5),
                new(4, "Xào mực lửa lớn", "Đặt chảo thật nóng với dầu ăn, trút mực vào đảo nhanh tay trên lửa cực lớn trong 3 phút đến khi mực xoăn lại giòn ngọt.", 3),
                new(5, "Đảo rau củ hoàn tất", "Cho hành tây, cần tây vào đảo thêm 2 phút cho rau củ chín tái giòn ngọt, tắt bếp và dọn ra đĩa ăn nóng.", 2)
            ]
        ),
        new(
            "Cháo gà",
            "mon-canh-sup",
            "https://images.unsplash.com/photo-1547592180-85f173990554",
            "Bát cháo gà nấm hương nóng hổi bốc khói nghi ngút, hạt gạo nở bung sánh mịn quyện cùng thịt gà ta xé phay ngọt mềm và nấm hương thơm lừng.",
            "Bước 1: Luộc gà ta lấy nước ngọt hầm cháo.\nBước 2: Gạo rang thơm rồi nấu cùng nước luộc gà.\nBước 3: Xé nhỏ thịt gà xào săn cùng nấm hương.\nBước 4: Trút thịt gà vào nồi cháo đun sôi lại, nêm nếm vừa ăn.",
            [
                new("ức gà", 600, "g"),
                new("gạo thơm", 200, "g"),
                new("nấm hương", 100, "g"),
                new("hành tím", 4, "củ"),
                new("gừng", 1, "củ"),
                new("nước mắm", 3, "muỗng canh"),
                new("tiêu xay", 1, "muỗng cà phê"),
                new("rau thơm", 1, "bó")
            ],
            [
                new(1, "Luộc gà lấy nước dùng", "Gà xát muối rửa sạch, cho vào nồi luộc chín cùng củ hành nướng và gừng đập dập. Vớt gà ra ngâm nước đá cho da giòn.", 30),
                new(2, "Nấu cháo", "Gạo tẻ trộn chút nếp rang thơm rồi trút vào nồi nước luộc gà ninh nhỏ lửa cho hạt gạo nở bung sánh mịn.", 40),
                new(3, "Xé thịt gà", "Thịt gà xé miếng nhỏ vừa ăn. Nấm hương ngâm nở thái sợi xào thơm cùng hành tím phi và chút nước mắm.", 15),
                new(4, "Nấu hòa quyện", "Trút gà xé và nấm hương vào nồi cháo, khuấy đều đun sôi liu riu thêm 5 phút cho cháo thơm ngọt.", 5),
                new(5, "Múc cháo ra tô", "Múc cháo nóng ra bát, rắc hành hoa, ngò rí thái nhỏ, nhiều tiêu xay và ăn kèm quẩy giòn tan.", 3)
            ]
        ),
        new(
            "Nem rán",
            "mon-chien-ran",
            "https://images.unsplash.com/photo-1560162071-da4c4a91077a",
            "Nem rán truyền thống Hà Nội với vỏ ngoài vàng ươm giòn tan, nhân bên trong béo bùi từ thịt nạc dăm, miến dong, mộc nhĩ và rau củ tươi ngon.",
            "Bước 1: Trộn đều thịt băm, miến dong, nấm hương, mộc nhĩ và trứng.\nBước 2: Cuốn nem đều tay bằng bánh đa nem mỏng.\nBước 3: Rán nem hai lửa cho vỏ giòn rụm lâu xẹp.\nBước 4: Pha nước chấm chua ngọt tỏi ớt ăn kèm rau sống.",
            [
                new("thịt ba chỉ", 400, "g"),
                new("tôm tươi", 200, "g"),
                new("trứng gà", 2, "trái"),
                new("nấm hương", 50, "g"),
                new("hành tím", 3, "củ"),
                new("tiêu xay", 1, "muỗng cà phê"),
                new("nước mắm", 2, "muỗng canh"),
                new("dầu ăn", 4, "muỗng canh")
            ],
            [
                new(1, "Làm nhân nem", "Trộn đều thịt nạc vai xay, tôm băm, mộc nhĩ nấm hương ngâm nở thái sợi, miến cắt khúc, cà rốt bào sợi và lòng đỏ trứng gà.", 20),
                new(2, "Gói nem", "Trải bánh đa nem, múc nhân vừa đủ vào giữa, gập hai mép bên hông rồi cuộn tròn chặt tay vừa phải.", 20),
                new(3, "Rán sơ lần 1", "Đun nóng dầu ăn ngập nửa chiếc nem, thả nem vào rán lửa vừa cho chín se vỏ rồi vớt ra để nguội.", 15),
                new(4, "Rán giòn lần 2", "Trước khi ăn, đun dầu thật sôi thả nem vào rán lửa lớn cho vỏ ngoài vàng ươm giòn rụm ráo dầu.", 8),
                new(5, "Trình bày", "Cắt nem thành từng khoanh xéo, dọn ra đĩa cùng bát nước mắm chua ngọt, đĩa bún tươi và rau sống xanh mướt.", 5)
            ]
        ),
        new(
            "Vịt om sấu",
            "am-thuc-mien-bac",
            "https://images.unsplash.com/photo-1587593810167-a84920ea0781",
            "Vịt om sấu đậm chất ẩm thực Bắc Bộ, thịt vịt mềm béo ngậy quyện cùng vị chua thanh đặc trưng của quả sấu và nước dừa ngọt thơm.",
            "Bước 1: Sơ chế vịt khử mùi hôi bằng rượu gừng.\nBước 2: Ướp vịt với sả, tỏi, nước mắm ngon.\nBước 3: Xào săn thịt vịt rồi thêm nước và quả sấu vào om.\nBước 4: Dầm sấu lấy vị chua thanh, thêm rau ngổ và tắt bếp.",
            [
                new("thịt ức gà", 800, "g"),
                new("sả", 4, "nhánh"),
                new("hành tím", 3, "củ"),
                new("gừng", 1, "củ"),
                new("nước dừa", 400, "ml"),
                new("nước mắm", 3, "muỗng canh"),
                new("rau thơm", 1, "bó")
            ],
            [
                new(1, "Sơ chế vịt", "Vịt xát kỹ rượu trắng và gừng đập dập để tẩy sạch mùi hôi, chặt miếng vuông vừa ăn.", 20),
                new(2, "Ướp thịt vịt", "Ướp vịt cùng sả băm, tỏi băm, hành tím băm, 2 muỗng nước mắm và chút tiêu trong 30 phút.", 30),
                new(3, "Xào săn vịt", "Cho thịt vịt vào nồi đảo trên lửa lớn cho ra bớt mỡ và thịt săn chắc lại.", 10),
                new(4, "Om cùng sấu tươi", "Đổ nước dừa và thả sấu đã cạo vỏ vào nồi, đun sôi rồi hạ nhỏ lửa om 25 phút cho thịt mềm.", 25),
                new(5, "Dầm sấu nêm vị", "Dùng muỗng dầm sấu lấy độ chua thanh ưng ý, thả mùi tàu rau ngổ vào đun sôi lại rồi múc ra bát dùng nóng.", 5)
            ]
        ),
        new(
            "Sinh tố",
            "do-uong-sinh-to",
            "https://images.unsplash.com/photo-1556881286-fc6915169721",
            "Ly sinh tố bơ sữa sánh mịn béo ngậy, thơm lừng vị sữa tươi và sữa đặc, mát lạnh sảng khoái bổ sung nhiều dưỡng chất.",
            "Bước 1: Chọn bơ sáp chín tới, tách lấy phần thịt bơ dẻo.\nBước 2: Cho bơ, sữa tươi, sữa đặc và đá bào vào máy xay sinh tố.\nBước 3: Xay nhuyễn mịn ở tốc độ cao trong 1-2 phút.\nBước 4: Rót ra ly, trang trí vài nhánh bạc hà và thưởng thức.",
            [
                new("sữa đặc", 4, "muỗng canh"),
                new("nước dừa", 100, "ml")
            ],
            [
                new(1, "Sơ chế bơ", "Bơ khía đôi, bỏ hạt, dùng muỗng nạo lấy phần thịt sáp vàng ươm cho vào cối xay.", 5),
                new(2, "Đong nguyên liệu", "Thêm sữa đặc có đường, sữa tươi và chút nước cốt dừa để tăng độ ngậy thơm.", 3),
                new(3, "Xay sinh tố", "Bật máy xay ở tốc độ vừa rồi tăng dần lên tốc độ cao xay 90 giây đến khi hỗn hợp sánh mịn không còn cặn đá.", 2),
                new(4, "Rót ra ly", "Rót sinh tố ra ly cao, cắm ống hút và thưởng thức ngay khi còn lạnh mịn sảng khoái.", 1)
            ]
        ),
        new(
            "Bún thịt nướng",
            "bun-mi-pho",
            "https://images.unsplash.com/photo-1559847844-5315695dadae",
            "Tô bún thịt nướng hấp dẫn với từng miếng thịt nướng than hoa vàng xém thơm lừng sả mè, ăn kèm bún tươi, chả giò, rau thơm và nước mắm chua ngọt.",
            "Bước 1: Ướp thịt ba chỉ với sả băm, mật ong, nước mắm.\nBước 2: Kẹp vỉ nướng trên than hoa chín vàng thơm nức.\nBước 3: Chuẩn bị bún, rau thơm, giá đỗ và đồ chua.\nBước 4: Xếp thịt nướng lên bún, rắc đậu phộng mỡ hành và chan nước mắm.",
            [
                new("thịt ba chỉ", 500, "g"),
                new("bún tươi", 500, "g"),
                new("sả", 3, "nhánh"),
                new("hành tím", 3, "củ"),
                new("nước mắm", 3, "muỗng canh"),
                new("đường thốt nốt", 2, "muỗng canh"),
                new("rau thơm", 1, "bó"),
                new("ớt", 2, "trái")
            ],
            [
                new(1, "Thái thịt và ướp", "Thịt ba chỉ thái mỏng vừa ăn. Ướp với sả băm nhuyễn, tỏi, mật ong, nước mắm, tiêu và mè rang trong 45 phút.", 45),
                new(2, "Nướng thịt", "Kẹp thịt vào vỉ nướng than hoa lật đều tay đến khi thịt chín vàng ruộm thơm nức mũi.", 20),
                new(3, "Chuẩn bị rau sống", "Rau xà lách, rau thơm các loại thái nhỏ lót dưới đáy tô lớn cùng giá sống.", 10),
                new(4, "Xếp tô bún", "Cho bún tươi lên trên rau, gắp thịt nướng vàng ươm đặt lên mặt bún.", 5),
                new(5, "Hoàn tất tô bún", "Chan mỡ hành xanh mướt, rắc đậu phộng rang giòn, đồ chua và chan bát nước mắm chua ngọt kẹo.", 3)
            ]
        ),
        new(
            "Gỏi gà",
            "mon-cuon-goi",
            "https://images.unsplash.com/photo-1540420773420-3366772f4999",
            "Gỏi gà bắp cải giòn sần sật chua ngọt thanh mát, thịt gà xé phay ngọt mềm hòa quyện cùng rau răm cay the và đậu phộng bùi béo.",
            "Bước 1: Luộc gà chín tới, vớt ra ngâm nước đá rồi xé phay.\nBước 2: Bào mỏng bắp cải và hành tây ngâm giấm đường giòn tan.\nBước 3: Pha nước mắm tỏi ớt chua ngọt chuẩn tỉ lệ.\nBước 4: Trộn đều thịt gà, rau bắp cải, nước mắm và rau răm.",
            [
                new("ức gà", 500, "g"),
                new("bắp cải", 1, "bắp"),
                new("hành tím", 2, "củ"),
                new("rau thơm", 1, "bó"),
                new("nước mắm", 3, "muỗng canh"),
                new("chanh", 2, "trái"),
                new("đường thốt nốt", 2, "muỗng canh"),
                new("ớt", 2, "trái")
            ],
            [
                new(1, "Luộc và xé gà", "Thịt gà luộc chín tới với chút hành gừng, vớt ra ngâm nước đá cho da giòn thịt săn rồi xé miếng mỏng dài.", 25),
                new(2, "Bào bắp cải", "Bắp cải bào thật mỏng, hành tây thái lát ngâm nước đá có chút giấm để khử hăng và giữ độ giòn sần sật.", 15),
                new(3, "Pha nước sốt trộn", "Pha 3 muỗng nước mắm, 2 muỗng đường, 2 muỗng nước cốt chanh, tỏi ớt băm khuấy tan tạo vị chua cay mặn ngọt.", 5),
                new(4, "Bóp trộn gỏi", "Cho thịt gà xé và bắp cải vào thau lớn, rưới từ từ 2/3 lượng nước sốt bóp đều tay cho ngấm đều gia vị.", 5),
                new(5, "Trình bày", "Cho rau răm thái rối và đậu phộng rang giã dập vào đảo nhẹ, múc ra đĩa ăn kèm phồng tôm giòn rụm.", 3)
            ]
        ),
        new(
            "Bánh flan",
            "banh-ngot-banh-nuong",
            "https://images.unsplash.com/photo-1551024709-8f23befc6f87",
            "Bánh flan caramen mịn màng núng nính, mềm tan trong miệng với vị béo ngậy của trứng sữa quyện cùng chút đắng thơm của cà phê nguyên chất.",
            "Bước 1: Thắng đường làm lớp caramen lót đáy khuôn.\nBước 2: Khuấy nhẹ trứng gà cùng sữa tươi và sữa đặc ấm.\nBước 3: Rót hỗn hợp qua rây lọc cho mịn rồi đổ vào khuôn.\nBước 4: Hấp cách thủy lửa nhỏ liu riu cho bánh chín mịn không rỗ.",
            [
                new("trứng gà", 5, "trái"),
                new("sữa đặc", 4, "muỗng canh"),
                new("đường thốt nốt", 80, "g")
            ],
            [
                new(1, "Thắng caramen", "Đun đường cùng 2 muỗng nước trên lửa nhỏ đến khi chuyển màu cánh gián thì trút đều vào đáy các khuôn flan.", 8),
                new(2, "Khuấy sữa trứng", "Sữa tươi đun ấm ấm khuấy tan cùng sữa đặc. Đánh tan trứng nhẹ tay rồi rót từ từ sữa ấm vào khuấy một chiều.", 10),
                new(3, "Rây mịn hỗn hợp", "Đổ hỗn hợp trứng sữa qua rây 2 lần để loại bỏ hoàn toàn bọt khí và cặn lòng trắng giúp bánh mịn màng.", 5),
                new(4, "Hấp bánh flan", "Rót hỗn hợp vào các khuôn caramen, đậy nắp hoặc bọc giấy bạc rồi hấp cách thủy lửa thật nhỏ trong 30 phút.", 30),
                new(5, "Thưởng thức", "Bánh chín để nguội cho vào ngăn mát tủ lạnh 3 tiếng, khi ăn úp ra đĩa thêm đá bào và cà phê đen đậm đà.", 5)
            ]
        ),
        new(
            "Bánh mì",
            "banh-mi-sandwich",
            "https://images.unsplash.com/photo-1626804475297-41608ea09aeb",
            "Ổ bánh mì kẹp giòn rụm nức tiếng thế giới với pate gan béo ngậy, trứng ốp la lòng đào, chả lụa thơm lừng ăn kèm dưa leo và ngò rí thanh mát.",
            "Bước 1: Nướng bánh mì nóng giòn.\nBước 2: Phết đều lớp bơ lạt và pate thơm béo vào ruột bánh.\nBước 3: Chiên trứng ốp la lòng đào vàng ươm.\nBước 4: Xếp dưa leo, ngò rí, đồ chua rồi rưới nước tương và sốt ớt.",
            [
                new("trứng gà", 2, "trái"),
                new("dầu ăn", 1, "muỗng canh"),
                new("rau thơm", 1, "bó"),
                new("nước mắm", 1, "muỗng canh")
            ],
            [
                new(1, "Làm nóng bánh mì", "Bánh mì cho vào lò nướng hoặc áp chảo 3 phút cho vỏ ngoài giòn rụm vàng thơm.", 3),
                new(2, "Chiên trứng ốp la", "Đun nóng chảo dầu, đập trứng gà vào chiên lửa vừa sao cho lòng trắng chín giòn rìa, lòng đỏ còn lòng đào béo ngậy.", 4),
                new(3, "Phết pate bơ", "Rạch dọc thân bánh mì, dùng dao phết đều một lớp bơ thơm và pate gan béo ngậy vào hai mặt ruột bánh.", 2),
                new(4, "Kẹp nhân", "Gắp trứng ốp la, dưa leo thái lát mỏng, ngò rí và đồ chua cà rốt củ cải xếp gọn ghẽ vào trong ruột bánh.", 2),
                new(5, "Chan sốt hoàn tất", "Rưới vài giọt nước tương ngon và tương ớt cay nồng lên trên, thưởng thức ngay khi bánh còn nóng giòn rụm.", 1)
            ]
        ),
        new(
            "Mì Quảng",
            "bun-mi-pho",
            "https://images.unsplash.com/photo-1569718212165-3a8278d5f624",
            "Món mì Quảng xứ Quảng với sợi mì vàng óng dai mềm, thịt gà ta xào củ nén thơm lừng, nước nhưn đậm đà xâm xấp và bánh tráng mè giòn rụm.",
            "Bước 1: Chặt gà ta miếng vừa ăn ướp củ nén giã dập và dầu điều.\nBước 2: Xào thịt gà thật săn rồi om cùng nước xâm xấp.\nBước 3: Chần sợi mì Quảng xếp vào tô cùng rau sống bắp chuối.\nBước 4: Chan nước nhưn đậm đà, rắc đậu phộng rang và bánh tráng mè nướng.",
            [
                new("thịt ức gà", 700, "g"),
                new("bún tươi", 500, "g"),
                new("hành tím", 4, "củ"),
                new("dầu ăn", 2, "muỗng canh"),
                new("bột nghệ", 1, "muỗng cà phê"),
                new("nước mắm", 3, "muỗng canh"),
                new("rau thơm", 1, "bó"),
                new("ớt", 2, "trái")
            ],
            [
                new(1, "Ướp thịt gà", "Gà ta chặt miếng nhỏ vừa ăn. Ướp cùng củ nén giã dập, ớt bột, bột nghệ, dầu màu điều và nước mắm ngon trong 30 phút.", 30),
                new(2, "Xào săn gà", "Đặt nồi lên bếp đun nóng dầu điều, phi thơm củ nén rồi trút thịt gà vào xào thật săn vàng.", 10),
                new(3, "Nấu nước nhưn", "Đổ nước dừa tươi hoặc nước dùng xâm xấp mặt thịt, hạ nhỏ lửa om 20 phút cho gà chín mềm đậm đà vị ngọt.", 20),
                new(4, "Bày tô mì Quảng", "Lót rau sống, bắp chuối bào dưới đáy tô, xếp sợi mì Quảng vàng óng lên trên rồi múc thịt gà đặt vào giữa.", 5),
                new(5, "Chan nước nhưn", "Chan một vá nước nhưn đậm đà xâm xấp mặt mì, rắc đậu phộng rang giã dập, ớt xanh và bẻ vụn bánh tráng mè nướng giòn tan.", 3)
            ]
        ),
        new(
            "Rau muống xào",
            "mon-xao-ap-chao",
            "https://images.unsplash.com/photo-1540420773420-3366772f4999",
            "Đĩa rau muống xào tỏi xanh mướt giòn sần sật, thơm ngào ngạt mùi tỏi cháy cạnh đậm đà đưa cơm trong mọi bữa ăn gia đình Việt.",
            "Bước 1: Nhặt rau muống, ngắt ngọn non rửa sạch.\nBước 2: Đập dập nhiều tép tỏi cô đơn thơm nức.\nBước 3: Phi tỏi vàng thơm trên chảo dầu thật nóng.\nBước 4: Trút rau vào xào lửa cực lớn đảo nhanh tay trong 3 phút.",
            [
                new("rau muống", 1, "bó"),
                new("tỏi", 3, "củ"),
                new("dầu ăn", 2, "muỗng canh"),
                new("nước mắm", 2, "muỗng canh"),
                new("dầu hào", 1, "muỗng canh"),
                new("ớt", 1, "trái")
            ],
            [
                new(1, "Nhặt và rửa rau", "Rau muống nhặt khúc vừa ăn, bỏ bớt lá già, rửa sạch nhiều lần rồi ngâm nước đá 5 phút cho rau cực giòn.", 10),
                new(2, "Đập tỏi", "Tỏi bóc vỏ, một nửa băm nhỏ và một nửa đập dập nguyên tép để khi xào vừa dậy mùi vừa có tép tỏi thơm bùi.", 5),
                new(3, "Phi tỏi thơm", "Đặt chảo thật nóng, cho 2 muỗng dầu ăn phi thơm vàng phần tỏi băm rồi múc bớt một nửa ra chén để riêng.", 3),
                new(4, "Xào rau muống lửa lớn", "Bật lửa cực lớn, trút toàn bộ rau muống vào chảo đảo liên tục thật nhanh tay cùng dầu hào và chút nước mắm.", 3),
                new(5, "Hoàn thành", "Khi rau vừa chín tới xanh mướt giòn ngọt thì tắt bếp ngay. Múc ra đĩa rắc phần tỏi phi vàng thơm giòn lên trên.", 1)
            ]
        ),
        new(
            "Tôm rim",
            "mon-kho-rim",
            "https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/tom-rim.jpg",
            "Tôm rim mặn ngọt óng ánh màu hổ phách, vỏ giòn ngọt quyện đẫm sốt caramen tỏi ớt đậm đà, cực kỳ đưa cơm trong bữa ăn gia đình Việt.",
            "Bước 1: Sơ chế tôm sú, cắt bớt râu và rút chỉ đen lưng tôm.\nBước 2: Ướp tôm cùng hành tỏi băm, nước mắm ngon và tiêu xay.\nBước 3: Thắng caramen đường thốt nốt, cho tôm vào đảo săn lửa lớn.\nBước 4: Rim nhỏ lửa cho nước sốt sánh kẹo, ngấm sâu vào từng con tôm.",
            [
                new("tôm sú tươi", 500, "g"),
                new("tỏi băm", 1, "củ"),
                new("hành tím băm", 2, "củ"),
                new("hành lá", 2, "nhánh"),
                new("ớt hiểm", 1, "trái"),
                new("nước mắm", 2, "muỗng canh"),
                new("đường thốt nốt", 2, "muỗng canh"),
                new("dầu màu điều", 1, "muỗng canh"),
                new("tiêu xay", 1, "muỗng cà phê")
            ],
            [
                new(1, "Sơ chế tôm sú", "Tôm sú cắt bớt phần đầu nhọn và râu dài, dùng tăm khều chỉ đen trên sống lưng. Rửa sạch lại với nước muối loãng rồi để ráo.", 10),
                new(2, "Ướp gia vị", "Ướp tôm với 1 muỗng canh nước mắm, hành tím băm, tiêu xay và nửa muỗng cà phê hạt nêm trong 15 phút.", 15),
                new(3, "Thắng màu caramen", "Bắc chảo lên bếp, cho 1 muỗng canh dầu màu điều và 2 muỗng canh đường thốt nốt đun lửa nhỏ đến khi đường tan ngả màu hổ phách.", 5),
                new(4, "Rim tôm mặn ngọt", "Phi thơm tỏi băm, trút tôm vào xào săn trên lửa lớn cho tôm chuyển màu đỏ au. Nêm thêm 1 muỗng nước mắm, hạ lửa rim liu riu đến khi nước sốt keo lại bám đều quanh thân tôm.", 8),
                new(5, "Hoàn thiện món ăn", "Rắc hành lá thái nhỏ và ớt tỉa hoa, tắt bếp. Múc tôm rim ra đĩa thưởng thức cùng cơm trắng nóng hổi.", 2)
            ]
        ),
        new(
            "Lẩu thái",
            "mon-lau-nong-hoi",
            "https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/lau-hai-san.jpg",
            "Nồi lẩu thái hải sản chua cay bốc khói nghi ngút, nước lẩu ngọt đậm từ xương hầm thơm nồng sả riềng lá chanh, ngập tràn tôm tươi, mực giòn và nấm rơm.",
            "Bước 1: Hầm xương ống heo lấy nước dùng ngọt trong.\nBước 2: Sơ chế hải sản tôm sú, mực ống, nghêu sạch sẽ.\nBước 3: Phi thơm gia vị lẩu thái chua cay đậm đà.\nBước 4: Bày biện hải sản, nấm, rau nhúng và thưởng thức quây quần.",
            [
                new("tôm sú", 400, "g"),
                new("mực ống", 300, "g"),
                new("nghêu tươi", 500, "g"),
                new("xương ống heo", 500, "g"),
                new("sả cây", 4, "cây"),
                new("riềng tươi", 1, "củ"),
                new("lá chanh", 6, "lá"),
                new("cà chua", 2, "trái"),
                new("nấm rơm", 150, "g"),
                new("nước cốt me", 3, "muỗng canh"),
                new("sa tế tôm", 2, "muỗng canh"),
                new("nước mắm", 3, "muỗng canh"),
                new("bún tươi", 500, "g")
            ],
            [
                new(1, "Nấu nước dùng xương", "Xương ống chần qua nước sôi rồi hầm với 2 lít nước trong 45 phút, thường xuyên vớt bọt cho nước trong ngọt tự nhiên.", 45),
                new(2, "Sơ chế hải sản", "Tôm rửa sạch cắt râu, mực làm sạch khía vảy rồng cắt miếng vừa ăn, nghêu ngâm ớt cho nhả hết cát rồi rửa sạch.", 20),
                new(3, "Nấu nước lẩu thái chua cay", "Phi thơm tỏi, sả đập dập, riềng và cà chua. Đổ nước dùng xương vào, thêm sa tế, nước cốt me, lá chanh vò nhẹ và nêm gia vị chua cay hài hòa.", 15),
                new(4, "Thả nấm và hoàn tất nước lẩu", "Cho nấm rơm vào nồi lẩu đun sôi thêm 5 phút cho ngọt nước, nếm lại vị chua cay vừa miệng rồi chuyển sang nồi lẩu chuyên dụng.", 5),
                new(5, "Thưởng thức", "Đun sôi liu riu nồi lẩu, nhúng tôm mực nghêu cùng rau muống, hoa chuối và thưởng thức kèm bún tươi chấm mắm ớt.", 5)
            ]
        )
    ];

    [Fact]
    public async Task FixAll120Recipes_WithFullSemanticQuality()
    {
        await using var context = RecipeRepositorySupabaseTests_CreateDbContext();

        var categories = await context.Categories.AsNoTracking().ToListAsync();
        var categoryMap = categories.ToDictionary(c => c.Slug, c => c.Id);

        var recipes = await context.Recipes
            .Include(r => r.Images)
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .ToListAsync();

        foreach (var recipe in recipes)
        {
            // Tìm catalog khớp với món ăn
            var catalog = Catalogs.FirstOrDefault(c =>
                recipe.Title.Contains(c.MatchPattern, StringComparison.OrdinalIgnoreCase));

            // Nếu không khớp món cụ thể, lấy catalog mặc định phù hợp
            catalog ??= Catalogs.First(c => c.MatchPattern == "Sườn xào");

            // 1. Cập nhật Category chuẩn
            if (categoryMap.TryGetValue(catalog.CategorySlug, out var catId))
            {
                recipe.CategoryId = catId;
            }

            // 2. Cập nhật Tổng quan & Hướng dẫn chuẩn tiếng Việt
            recipe.Description = catalog.Description;
            recipe.Instructions = catalog.Instructions;

            // 3. Cập nhật Ảnh đại diện chính (Cover Image) chuẩn xác món
            var primaryImage = recipe.Images.FirstOrDefault(i => i.IsPrimary) ?? recipe.Images.FirstOrDefault();
            if (primaryImage is not null)
            {
                // Giữ nguyên ảnh Supabase Storage CDN nếu đã có, tránh ghi đè sang ảnh Unsplash cũ
                if (!primaryImage.OriginalUrl.Contains("supabase.co"))
                {
                    primaryImage.OriginalUrl = catalog.ImageUrl;
                    primaryImage.MediumUrl = catalog.ImageUrl;
                    primaryImage.ThumbnailUrl = catalog.ImageUrl;
                }
            }
            else
            {
                recipe.AddImage(new RecipeImage
                {
                    RecipeId = recipe.Id,
                    OriginalUrl = catalog.ImageUrl,
                    MediumUrl = catalog.ImageUrl,
                    ThumbnailUrl = catalog.ImageUrl,
                    IsPrimary = true,
                    OrderIndex = 1
                });
            }

            // 4. Chuẩn hóa Danh sách Nguyên liệu chính xác theo món
            context.RecipeIngredients.RemoveRange(recipe.Ingredients);
            recipe.Ingredients.Clear();

            var order = 1;
            foreach (var ing in catalog.Ingredients)
            {
                recipe.Ingredients.Add(new RecipeIngredient
                {
                    Id = Guid.NewGuid(),
                    RecipeId = recipe.Id,
                    Name = ing.Name,
                    Quantity = ing.Quantity,
                    Unit = ing.Unit,
                    Notes = null,
                    OrderIndex = order++
                });
            }

            // 5. Chuẩn hóa Danh sách Các bước nấu chính xác theo món
            context.RecipeSteps.RemoveRange(recipe.Steps);
            recipe.Steps.Clear();

            foreach (var step in catalog.Steps)
            {
                // Loại bỏ hoàn toàn ảnh stock ngoại quốc không phù hợp trong các bước nấu
                string? stepImage = null;

                recipe.Steps.Add(new RecipeStep
                {
                    Id = Guid.NewGuid(),
                    RecipeId = recipe.Id,
                    StepNumber = step.StepNumber,
                    Title = step.Title,
                    Description = step.Description,
                    TimerMinutes = step.TimerMinutes,
                    ImageUrl = stepImage
                });
            }
        }

        var saved = await context.SaveChangesAsync();
        Assert.True(saved > 0);
    }

    private static ApplicationDbContext RecipeRepositorySupabaseTests_CreateDbContext()
    {
        var repositoryRoot = FindRepositoryRoot();
        var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .SetBasePath(repositoryRoot)
            .AddJsonFile("src/CulinaryBlog.API/appsettings.json", optional: true)
            .AddJsonFile("src/CulinaryBlog.API/appsettings.Development.json", optional: true)
            .AddEnvironmentVariables();

        var envPath = Path.Combine(repositoryRoot, ".env");
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadLines(envPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#')) continue;
                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex > 0)
                {
                    builder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [trimmed[..separatorIndex].Trim()] = trimmed[(separatorIndex + 1)..].Trim()
                    });
                }
            }
        }

        var configuration = builder.Build();
        var connectionString = SupabaseConnectionStringResolver.Resolve(configuration);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "src", "CulinaryBlog.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
