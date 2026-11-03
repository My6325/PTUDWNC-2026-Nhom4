-- ==============================================================================
-- CHUẨN HÓA TOÀN DIỆN DỮ LIỆU CÔNG THỨC ẨM THỰC (DATA QUALITY FIX)
-- 1. Chuẩn hóa hình ảnh món ăn chính (Cover Image) theo đúng văn hóa món Việt
-- 2. Dọn sạch ảnh phong cảnh/resort ở các bước nấu và thay bằng ảnh thao tác bếp
-- 3. Chuẩn hóa tên nguyên liệu, định lượng, đơn vị đo thực tế và xóa bỏ văn bản rác
-- 4. Chuẩn hóa nội dung mẫu đẹp cho món Canh chua cá lóc #120
-- ==============================================================================

-- 1. XÓA BỎ VĂN BẢN VÔ NGHĨA (NOTES) Ở TẤT CẢ NGUYÊN LIỆU
UPDATE "RecipeIngredients"
SET "Notes" = NULL;

-- 2. CHUẨN HÓA ĐƠN VỊ VÀ ĐỊNH LƯỢNG HỢP LÝ CHO TOÀN BỘ NGUYÊN LIỆU (KHÔNG CÒN SỐ LẺ KỲ QUẶC)
UPDATE "RecipeIngredients"
SET 
  "Unit" = CASE 
    WHEN "Name" IN ('nước mắm', 'dầu hào', 'sa tế', 'sữa đặc') THEN 'muỗng canh'
    WHEN "Name" IN ('muối biển', 'tiêu xay', 'bột nghệ', 'đường thốt nốt') THEN 'muỗng cà phê'
    WHEN "Name" IN ('nước dừa') THEN 'ml'
    WHEN "Name" IN ('thịt ba chỉ', 'thịt bò', 'ức gà', 'cá lóc', 'tôm tươi', 'mực ống') THEN 'g'
    WHEN "Name" IN ('bún tươi', 'gạo thơm', 'đậu xanh', 'nấm hương') THEN 'g'
    WHEN "Name" IN ('hành tím', 'tỏi', 'gừng') THEN 'củ'
    WHEN "Name" IN ('cà chua', 'chanh', 'ớt', 'trứng gà') THEN 'trái'
    WHEN "Name" IN ('sả') THEN 'nhánh'
    WHEN "Name" IN ('rau muống', 'rau thơm') THEN 'bó'
    WHEN "Name" IN ('bắp cải') THEN 'bắp'
    WHEN "Name" IN ('đậu hũ') THEN 'miếng'
    ELSE 'g'
  END,
  "Quantity" = CASE 
    WHEN "Name" IN ('nước mắm', 'dầu hào', 'sa tế', 'sữa đặc') THEN 2
    WHEN "Name" IN ('muối biển', 'tiêu xay', 'bột nghệ') THEN 1
    WHEN "Name" IN ('đường thốt nốt') THEN 2
    WHEN "Name" IN ('nước dừa') THEN 350
    WHEN "Name" IN ('thịt ba chỉ', 'thịt bò', 'ức gà', 'cá lóc') THEN 500
    WHEN "Name" IN ('tôm tươi', 'mực ống') THEN 300
    WHEN "Name" IN ('bún tươi', 'gạo thơm') THEN 400
    WHEN "Name" IN ('đậu xanh', 'nấm hương') THEN 150
    WHEN "Name" IN ('hành tím', 'tỏi') THEN 3
    WHEN "Name" IN ('gừng') THEN 1
    WHEN "Name" IN ('cà chua', 'trứng gà') THEN 2
    WHEN "Name" IN ('chanh', 'ớt') THEN 1
    WHEN "Name" IN ('sả') THEN 3
    WHEN "Name" IN ('rau muống', 'rau thơm', 'bắp cải') THEN 1
    WHEN "Name" IN ('đậu hũ') THEN 2
    ELSE 100
  END;

-- 3. XỬ LÝ ẢNH CÁC BƯỚC NẤU (RECIPE STEPS): THAY ẢNH BIỂN/RESORT BẰNG ẢNH BẾP THỰC TẾ
UPDATE "RecipeSteps"
SET "ImageUrl" = CASE 
  WHEN "StepNumber" = 1 THEN 'https://images.unsplash.com/photo-1556910103-1c02745aae4d?w=800&auto=format&fit=crop&q=80' -- Sơ chế nguyên liệu
  WHEN "StepNumber" = 2 THEN 'https://images.unsplash.com/photo-1506368249639-73a05d6f6488?w=800&auto=format&fit=crop&q=80' -- Ướp gia vị
  WHEN "StepNumber" = 3 THEN 'https://images.unsplash.com/photo-1556911220-e15b29be8c8f?w=800&auto=format&fit=crop&q=80' -- Xào nấu bếp
  ELSE NULL -- Các bước sau để trống ảnh cho gọn gàng, sạch đẹp
END
WHERE "ImageUrl" LIKE '%picsum.photos%' OR "ImageUrl" LIKE '%photo-1547592180%';

-- 4. CẬP NHẬT ẢNH CHÍNH (COVER IMAGE) CHUẨN XÁC THEO TỪNG MÓN ĂN VIỆT NAM
UPDATE "RecipeImages" ri
SET 
  "OriginalUrl" = sub.url || '?w=1200&auto=format&fit=crop&q=80',
  "MediumUrl"   = sub.url || '?w=800&auto=format&fit=crop&q=80',
  "ThumbnailUrl"= sub.url || '?w=300&auto=format&fit=crop&q=80'
FROM (
  SELECT r."Id",
    CASE 
      -- Canh chua / Canh cá (Ảnh bát canh nước trong chuẩn vị Việt)
      WHEN r."Title" LIKE '%Canh chua%' OR r."Title" LIKE '%Canh%' THEN 'https://images.unsplash.com/photo-1547496502-affa22d38842'
      -- Cháo gà / Cháo nấm
      WHEN r."Title" LIKE '%Cháo%' THEN 'https://images.unsplash.com/photo-1541832676-9b763b0239ab'
      -- Phở bò
      WHEN r."Title" LIKE '%Phở bò%' OR r."Title" LIKE '%Phở%' THEN 'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43'
      -- Bún bò Huế
      WHEN r."Title" LIKE '%Bún bò%' THEN 'https://images.unsplash.com/photo-1569058242253-92a9c755a0ec'
      -- Bún thịt nướng / Bún chả
      WHEN r."Title" LIKE '%Bún thịt nướng%' OR r."Title" LIKE '%Bún chả%' THEN 'https://images.unsplash.com/photo-1559847844-5315695dadae'
      -- Mì Quảng
      WHEN r."Title" LIKE '%Mì Quảng%' THEN 'https://images.unsplash.com/photo-1569718212165-3a8278d5f624'
      -- Cơm tấm
      WHEN r."Title" LIKE '%Cơm tấm%' THEN 'https://images.unsplash.com/photo-1504674900247-0877df9cc836'
      -- Cơm chiên
      WHEN r."Title" LIKE '%Cơm chiên%' THEN 'https://images.unsplash.com/photo-1536304993881-ff6e9eefa2a6'
      -- Xôi gấc / Xôi
      WHEN r."Title" LIKE '%Xôi%' THEN 'https://images.unsplash.com/photo-1509722747041-616f39b57569'
      -- Bánh mì
      WHEN r."Title" LIKE '%Bánh mì%' THEN 'https://images.unsplash.com/photo-1626804475297-41608ea09aeb'
      -- Nem rán / Chả giò
      WHEN r."Title" LIKE '%Nem rán%' OR r."Title" LIKE '%Chả giò%' THEN 'https://images.unsplash.com/photo-1541544741938-0af808871cc0'
      -- Bánh xèo
      WHEN r."Title" LIKE '%Bánh xèo%' THEN 'https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8'
      -- Gỏi cuốn / Món cuốn
      WHEN r."Title" LIKE '%Gỏi cuốn%' OR r."Title" LIKE '%cuốn%' THEN 'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb'
      -- Gỏi / Nộm
      WHEN r."Title" LIKE '%Gỏi%' OR r."Title" LIKE '%Nộm%' THEN 'https://images.unsplash.com/photo-1540420773420-3366772f4999'
      -- Bò lúc lắc / Món bò
      WHEN r."Title" LIKE '%Bò lúc lắc%' OR r."Title" LIKE '%bò%' THEN 'https://images.unsplash.com/photo-1558030006-450675393462'
      -- Gà nướng / Sườn nướng BBQ
      WHEN r."Title" LIKE '%nướng%' THEN 'https://images.unsplash.com/photo-1555939594-58d7cb561ad1'
      -- Gà luộc / Gà hấp
      WHEN r."Title" LIKE '%Gà ta%' OR r."Title" LIKE '%hấp%' OR r."Title" LIKE '%luộc%' THEN 'https://images.unsplash.com/photo-1587593810167-a84920ea0781'
      -- Cá kho tộ / Thịt kho tàu
      WHEN r."Title" LIKE '%kho%' OR r."Title" LIKE '%rim%' THEN 'https://images.unsplash.com/photo-1544025162-d76694265947'
      -- Tôm / Mực xào
      WHEN r."Title" LIKE '%Tôm%' OR r."Title" LIKE '%Mực%' THEN 'https://images.unsplash.com/photo-1534939561126-855b8675edd7'
      -- Lẩu
      WHEN r."Title" LIKE '%Lẩu%' THEN 'https://images.unsplash.com/photo-1547496502-affa22d38842'
      -- Đậu hũ
      WHEN r."Title" LIKE '%Đậu hũ%' THEN 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c'
      -- Chè / Tráng miệng
      WHEN r."Title" LIKE '%Chè%' OR r."Title" LIKE '%Bánh flan%' THEN 'https://images.unsplash.com/photo-1551024709-8f23befc6f87'
      -- Sinh tố / Nước ép
      WHEN r."Title" LIKE '%Sinh tố%' OR r."Title" LIKE '%Nước ép%' THEN 'https://images.unsplash.com/photo-1553530666-ba11a7da3888'
      -- Cà phê / Trà
      WHEN r."Title" LIKE '%Cà phê%' OR r."Title" LIKE '%Trà%' THEN 'https://images.unsplash.com/photo-1509042239860-f550ce710b93'
      -- Mặc định món ăn hấp dẫn
      ELSE 'https://images.unsplash.com/photo-1504674900247-0877df9cc836'
    END AS url
  FROM "Recipes" r
) sub
WHERE ri."RecipeId" = sub."Id";

-- 5. CHUẨN HÓA RIÊNG MÓN ĐINH "Canh chua cá lóc #120"
UPDATE "Recipes"
SET 
  "Description" = 'Món canh chua cá lóc Nam Bộ chuẩn vị với thịt cá béo ngọt, nước dùng chua thanh đậm đà từ me cùng hương thơm nồng nàn của ngò gai, ngò ôm.',
  "Instructions" = 'Bước 1: Sơ chế cá lóc thật sạch với muối và chanh để khử nhớt và mùi tanh. Rửa sạch các loại rau bổi gồm cà chua, đậu bắp, bạc hà, giá đỗ và ngò om.' || E'\n\n' ||
                   'Bước 2: Bắc nồi lên bếp, phi thơm tỏi băm với một chút dầu ăn rồi trút cà chua vào xào sơ để tạo màu nước canh tự nhiên.' || E'\n\n' ||
                   'Bước 3: Cho khoảng 1.2 lít nước vào đun sôi, chần nước cốt me lọc lấy vị chua rồi thả cá lóc vào nấu chín tới trong khoảng 7-10 phút.' || E'\n\n' ||
                   'Bước 4: Nêm 2 muỗng nước mắm ngon, 1 muỗng đường, 1/2 muỗng muối sao cho vị canh đạt độ chua ngọt hài hòa.' || E'\n\n' ||
                   'Bước 5: Thả bạc hà, đậu bắp, giá đỗ vào đun sôi lại 1 phút rồi tắt bếp. Múc canh ra tô, rắc ngò ôm, ngò gai thái nhỏ và vài lát ớt tươi lên bề mặt.'
WHERE "Slug" LIKE '%canh-chua-ca-loc%';

-- Cập nhật mô tả các bước cho Canh chua cá lóc
UPDATE "RecipeSteps"
SET "Description" = CASE 
  WHEN "StepNumber" = 1 THEN 'Cá lóc đánh vảy, làm sạch ruột, xát muối và nước cốt chanh để khử sạch nhớt tanh, cắt khúc vừa ăn. Rau thơm, cà chua, đậu bắp rửa sạch thái lát.'
  WHEN "StepNumber" = 2 THEN 'Ướp cá với 1 muỗng cà phê muối, 1 muỗng canh nước mắm và tiêu xay trong 15 phút cho thịt cá săn chắc và ngấm đều gia vị.'
  WHEN "StepNumber" = 3 THEN 'Đặt nồi lên bếp, phi thơm tỏi băm và hành tím với dầu ăn. Cho cà chua vào xào chín mềm để nước dùng có màu đỏ tự nhiên đẹp mắt.'
  WHEN "StepNumber" = 4 THEN 'Cho nước vào đun sôi, hòa nước cốt me dầm lấy vị chua thanh rồi cho cá lóc vào đun lửa vừa khoảng 10 phút đến khi cá vừa chín tới.'
  WHEN "StepNumber" = 5 THEN 'Nêm nếm lại gia vị cho vừa miệng với vị chua ngọt đậm đà, sau đó cho đậu bắp, bạc hà và giá đỗ vào đun thêm 2 phút là hoàn tất.'
  ELSE 'Múc canh ra bát lớn, rắc thêm ngò gai, ngò ôm thái nhỏ, ớt lát và tỏi phi vàng thơm lừng lên trên. Dùng nóng kèm cơm trắng và bát nước mắm ớt.'
END
WHERE "RecipeId" IN (SELECT "Id" FROM "Recipes" WHERE "Slug" LIKE '%canh-chua-ca-loc%');
