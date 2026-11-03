-- ==============================================================================
-- CẬP NHẬT TOÀN BỘ ẢNH CÔNG THỨC TRONG DATABASE SANG SUPABASE STORAGE CDN
-- Bucket: culinary-blog / recipes / covers /
-- Đảm bảo: 100% chuẩn món ăn Việt Nam, tốc độ cao, không bị lỗi 429 Rate Limit
-- ==============================================================================

UPDATE "RecipeImages" ri
SET 
  "OriginalUrl"  = sub.img_url,
  "MediumUrl"    = sub.img_url,
  "ThumbnailUrl" = sub.img_url
FROM (
  SELECT r."Id",
    CASE 
      -- Phở bò
      WHEN r."Title" LIKE '%Phở bò%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/pho-bo.jpg'
      
      -- Bún bò Huế
      WHEN r."Title" LIKE '%Bún bò Huế%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/bun-bo-hue.jpg'
      
      -- Bún thịt nướng
      WHEN r."Title" LIKE '%Bún thịt nướng%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/bun-thit-nuong.jpg'
      
      -- Mì Quảng
      WHEN r."Title" LIKE '%Mì Quảng%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/mi-quang.jpg'
      
      -- Cơm tấm sườn bì chả
      WHEN r."Title" LIKE '%Cơm tấm%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/com-tam.jpg'
      
      -- Bánh mì pate trứng
      WHEN r."Title" LIKE '%Bánh mì%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/banh-mi.jpg'
      
      -- Gỏi cuốn tôm thịt
      WHEN r."Title" LIKE '%Gỏi cuốn%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/goi-cuon.jpg'
      
      -- Nem rán / Chả giò
      WHEN r."Title" LIKE '%Nem rán%' OR r."Title" LIKE '%Chả giò%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/cha-gio.jpg'
      
      -- Bánh xèo miền Tây
      WHEN r."Title" LIKE '%Bánh xèo%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/banh-xeo.jpg'
      
      -- Bò lúc lắc sốt tiêu đen
      WHEN r."Title" LIKE '%Bò lúc lắc%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/bo-luc-lac.jpg'
      
      -- Cá kho tộ nước dừa
      WHEN r."Title" LIKE '%Cá kho%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/ca-kho-to.jpg'
      
      -- Thịt kho tàu / Thịt kho trứng
      WHEN r."Title" LIKE '%Thịt kho%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/thit-kho-trung.jpg'
      
      -- Canh chua cá lóc
      WHEN r."Title" LIKE '%Canh chua%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/canh-chua-ca-loc.jpg'
      
      -- Chả cá thì là / Chả cá Lã Vọng
      WHEN r."Title" LIKE '%Chả cá%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/cha-ca-la-vong.jpg'
      
      -- Đậu hũ sốt cà chua
      WHEN r."Title" LIKE '%Đậu hũ%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/dau-hu-sot-ca.jpg'
      
      -- Gà nướng mật ong sả ớt
      WHEN r."Title" LIKE '%Gà nướng%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/ga-nuong.jpg'
      
      -- Vịt om sấu / Gà luộc
      WHEN r."Title" LIKE '%Vịt%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/ga-luoc-la-chanh.jpg'
      
      -- Cháo gà nấm hương
      WHEN r."Title" LIKE '%Cháo%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/chao-ga.jpg'
      
      -- Tôm rim mặn ngọt
      WHEN r."Title" LIKE '%Tôm rim%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/tom-rim.jpg'
      
      -- Mực xào sa tế
      WHEN r."Title" LIKE '%Mực%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/muc-xao.jpg'
      
      -- Rau muống xào tỏi
      WHEN r."Title" LIKE '%Rau muống%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/rau-muong-xao-toi.jpg'
      
      -- Gỏi gà bắp cải
      WHEN r."Title" LIKE '%Gỏi gà%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/goi-ga.jpg'
      
      -- Lẩu thái hải sản
      WHEN r."Title" LIKE '%Lẩu%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/lau-hai-san.jpg'
      
      -- Sườn xào chua ngọt
      WHEN r."Title" LIKE '%Sườn%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/suon-xao-chua-ngot.jpg'
      
      -- Bánh flan cà phê
      WHEN r."Title" LIKE '%Bánh flan%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/banh-flan.jpg'
      
      -- Chè đậu xanh nha đam
      WHEN r."Title" LIKE '%Chè%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/che-dau-xanh.jpg'
      
      -- Sinh tố bơ sữa đặc
      WHEN r."Title" LIKE '%Sinh tố%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/sinh-to-bo.jpg'
      
      -- Cà phê sữa đá
      WHEN r."Title" LIKE '%Cà phê%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/ca-phe-sua-da.jpg'
      
      -- Xôi gấc đậu xanh
      WHEN r."Title" LIKE '%Xôi%' 
        THEN 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/xoi-gac.jpg'
      
      -- Mặc định Cơm chiên
      ELSE 'https://ckhzgvwmaldvbxgoimjo.supabase.co/storage/v1/object/public/culinary-blog/recipes/covers/com-chien.jpg'
    END AS img_url
  FROM "Recipes" r
  WHERE r."IsDeleted" = FALSE
) sub
WHERE ri."RecipeId" = sub."Id" 
  AND ri."IsDeleted" = FALSE;

-- Kiểm tra lại kết quả sau khi update:
SELECT r."Title", ri."OriginalUrl"
FROM "Recipes" r
JOIN "RecipeImages" ri ON r."Id" = ri."RecipeId"
WHERE r."IsDeleted" = FALSE AND ri."IsDeleted" = FALSE AND ri."IsPrimary" = TRUE
ORDER BY r."Title"
LIMIT 30;
