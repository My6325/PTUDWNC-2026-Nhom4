-- ==============================================================================
-- CẬP NHẬT CATEGORY_ID CHUẨN NGỮ NGHĨA CHO TOÀN BỘ RECIPES (1 CÂU LỆNH DUY NHẤT)
-- ==============================================================================

UPDATE "Recipes"
SET "CategoryId" = CASE 
  -- 1. Bún, Mì & Phở
  WHEN "Title" LIKE '%Phở%' OR "Title" LIKE '%Bún%' OR "Title" LIKE '%Mì Quảng%' 
    THEN '11111111-1111-1111-1111-111111111108'::uuid

  -- 2. Món Kho & Rim
  WHEN "Title" LIKE '%kho%' OR "Title" LIKE '%rim%' 
    THEN '11111111-1111-1111-1111-111111111103'::uuid

  -- 3. Trà & Cà Phê
  WHEN "Title" LIKE '%Cà phê%' OR "Title" LIKE '%Trà%' 
    THEN '11111111-1111-1111-1111-111111111122'::uuid

  -- 4. Đồ Uống & Sinh Tố
  WHEN "Title" LIKE '%Sinh tố%' 
    THEN '11111111-1111-1111-1111-111111111121'::uuid

  -- 5. Chè & Tráng Miệng
  WHEN "Title" LIKE '%Chè%' 
    THEN '11111111-1111-1111-1111-111111111120'::uuid

  -- 6. Bánh Ngọt & Bánh Nướng
  WHEN "Title" LIKE '%Bánh flan%' OR "Title" LIKE '%Bánh bông lan%' 
    THEN '11111111-1111-1111-1111-111111111119'::uuid

  -- 7. Bánh Mì & Sandwich
  WHEN "Title" LIKE '%Bánh mì%' 
    THEN '11111111-1111-1111-1111-111111111118'::uuid

  -- 8. Cơm & Xôi
  WHEN "Title" LIKE '%Cơm tấm%' OR "Title" LIKE '%Xôi%' 
    THEN '11111111-1111-1111-1111-111111111109'::uuid

  -- 9. Món Cuốn & Gỏi
  WHEN "Title" LIKE '%Gỏi%' 
    THEN '11111111-1111-1111-1111-111111111110'::uuid

  -- 10. Món Chay Thanh Tịnh
  WHEN "Title" LIKE '%Đậu hũ%' OR "Title" LIKE '%Rau muống%' 
    THEN '11111111-1111-1111-1111-111111111116'::uuid

  -- 11. Món Chiên & Rán
  WHEN "Title" LIKE '%Bánh xèo%' OR "Title" LIKE '%Nem rán%' 
    THEN '11111111-1111-1111-1111-111111111105'::uuid

  -- 12. Món Nướng BBQ
  WHEN "Title" LIKE '%nướng%' 
    THEN '11111111-1111-1111-1111-111111111106'::uuid

  -- 13. Món Lẩu Nóng Hổi
  WHEN "Title" LIKE '%Lẩu%' 
    THEN '11111111-1111-1111-1111-111111111111'::uuid

  -- 14. Món Bò Đậm Vị
  WHEN "Title" LIKE '%Bò lúc lắc%' 
    THEN '11111111-1111-1111-1111-111111111113'::uuid

  -- 15. Món Gà & Vịt
  WHEN "Title" LIKE '%Vịt%' 
    THEN '11111111-1111-1111-1111-111111111115'::uuid

  -- 16. Hải Sản Biển
  WHEN "Title" LIKE '%Cá hồi%' OR "Title" LIKE '%Mực%' OR "Title" LIKE '%Chả cá%' 
    THEN '11111111-1111-1111-1111-111111111112'::uuid

  -- 17. Món Canh & Súp
  WHEN "Title" LIKE '%Canh chua%' OR "Title" LIKE '%Cháo%' 
    THEN '11111111-1111-1111-1111-111111111102'::uuid

  ELSE "CategoryId"
END;
