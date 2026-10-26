-- ==============================================================================
-- CẬP NHẬT TOÀN BỘ 120 ẢNH MÓN ĂN SANG UNSPLASH (1 LỆNH DUY NHẤT)
-- ==============================================================================

UPDATE "RecipeImages" ri
SET 
  "OriginalUrl" = sub.url || '?w=1200&auto=format&fit=crop&q=80',
  "MediumUrl"   = sub.url || '?w=800&auto=format&fit=crop&q=80',
  "ThumbnailUrl"= sub.url || '?w=300&auto=format&fit=crop&q=80'
FROM (
  SELECT r."Id",
    CASE 
      WHEN r."Title" LIKE '%Phở bò%' THEN 'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43'
      WHEN r."Title" LIKE '%Bún bò Huế%' THEN 'https://images.unsplash.com/photo-1569058242253-92a9c755a0ec'
      WHEN r."Title" LIKE '%Cơm tấm%' THEN 'https://images.unsplash.com/photo-1504674900247-0877df9cc836'
      WHEN r."Title" LIKE '%Bánh mì%' THEN 'https://images.unsplash.com/photo-1626804475297-41608ea09aeb'
      WHEN r."Title" LIKE '%Nem rán%' THEN 'https://images.unsplash.com/photo-1541544741938-0af808871cc0'
      WHEN r."Title" LIKE '%Gỏi cuốn%' THEN 'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb'
      WHEN r."Title" LIKE '%Bò lúc lắc%' THEN 'https://images.unsplash.com/photo-1558030006-450675393462'
      WHEN r."Title" LIKE '%Gà nướng%' THEN 'https://images.unsplash.com/photo-1598515214211-89d3c73ae83b'
      WHEN r."Title" LIKE '%Lẩu thái%' THEN 'https://images.unsplash.com/photo-1547496502-affa22d38842'
      WHEN r."Title" LIKE '%Canh chua%' THEN 'https://images.unsplash.com/photo-1547592166-23ac45744acd'
      WHEN r."Title" LIKE '%kho%' THEN 'https://images.unsplash.com/photo-1544025162-d76694265947'
      WHEN r."Title" LIKE '%Bánh xèo%' THEN 'https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8'
      WHEN r."Title" LIKE '%Bún thịt nướng%' THEN 'https://images.unsplash.com/photo-1559847844-5315695dadae'
      WHEN r."Title" LIKE '%Mì Quảng%' THEN 'https://images.unsplash.com/photo-1569718212165-3a8278d5f624'
      WHEN r."Title" LIKE '%Tôm%' OR r."Title" LIKE '%Mực%' THEN 'https://images.unsplash.com/photo-1534939561126-855b8675edd7'
      WHEN r."Title" LIKE '%Rau%' OR r."Title" LIKE '%Gỏi gà%' THEN 'https://images.unsplash.com/photo-1540420773420-3366772f4999'
      WHEN r."Title" LIKE '%Đậu hũ%' THEN 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c'
      WHEN r."Title" LIKE '%Cà phê%' THEN 'https://images.unsplash.com/photo-1509042239860-f550ce710b93'
      ELSE 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c'
    END AS url
  FROM "Recipes" r
) sub
WHERE ri."RecipeId" = sub."Id";
