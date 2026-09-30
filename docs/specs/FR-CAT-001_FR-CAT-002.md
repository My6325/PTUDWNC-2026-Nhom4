# FR-CAT-001 & FR-CAT-002 — Quản lý Xem Danh sách và Chi tiết Danh mục Ẩm thực

## Trạng thái

**Đã duyệt và triển khai.** Tích hợp bộ nhớ đệm In-Memory Cache (TTL 60 phút), Mapster projection và tài liệu tương tác Scalar UI.

## Mục tiêu

1. **FR-CAT-001:** Cung cấp endpoint công khai truy xuất toàn bộ danh mục ẩm thực đang hoạt động để hiển thị trên thanh điều hướng (Navbar), trang chủ và bộ lọc tìm kiếm. Tích hợp bộ đệm để giảm tải truy vấn lặp lại đến CSDL Supabase.
2. **FR-CAT-002:** Cung cấp endpoint công khai xem thông tin chi tiết của một danh mục dựa trên đường dẫn tĩnh thân thiện SEO (`slug`), đồng thời nạp kèm danh sách tóm tắt các món ăn đã xuất bản thuộc danh mục đó.

---

## Đặc tả API

### 1. Endpoint FR-CAT-001 — Lấy danh sách danh mục
| Thuộc tính | Đặc tả |
| :--- | :--- |
| **Method & Route** | `GET /api/v1/categories` |
| **Quyền truy cập** | Công khai (Anonymous / Guest), không yêu cầu Token |
| **Phản hồi thành công** | `200 OK`, body là `List<CategoryDto>` |
| **Chiến lược Cache** | In-Memory Cache (`IMemoryCache`), thời hạn 3600 giây (60 phút), cache key: `"categories:all"` |
| **Query Parameters** | Không có (số lượng danh mục giới hạn $\le 50$, tải toàn bộ không phân trang) |

### 2. Endpoint FR-CAT-002 — Xem chi tiết danh mục theo Slug
| Thuộc tính | Đặc tả |
| :--- | :--- |
| **Method & Route** | `GET /api/v1/categories/{slug}` |
| **Quyền truy cập** | Công khai (Anonymous / Guest), không yêu cầu Token |
| **Phản hồi thành công** | `200 OK`, body là `CategoryDetailDto` |
| **Phản hồi khi lỗi** | `404 Not Found` nếu slug không tồn tại hoặc đã xóa mềm |

#### Route parameters:
| Tên | Kiểu | Bắt buộc | Quy tắc định dạng |
| :--- | :--- | :--- | :--- |
| `slug` | string | Có | Chuỗi chữ thường không dấu, phân tách bằng dấu gạch ngang (ví dụ: `mon-chinh-dam-da`) |

---

## Quy tắc dữ liệu và truy vấn

1. **Tính hợp lệ của bản ghi:** Chỉ truy xuất các danh mục có `IsDeleted == false`. Tuyệt đối không để lộ các danh mục đã bị xóa mềm.
2. **Tối ưu hóa hiệu năng truy vấn:** Tất cả truy vấn đọc danh mục và bài viết liên quan bắt buộc sử dụng `.AsNoTracking()` của EF Core.
3. **Thứ tự sắp xếp mặc định (`FR-CAT-001`):**
   - Sắp xếp tăng dần theo `OrderIndex` (`c.OrderIndex ASC`).
   - Nếu `OrderIndex` trùng nhau, sắp xếp theo tên danh mục (`c.Name ASC`).
4. **Đếm số lượng công thức (`RecipeCount`):**
   - `RecipeCount` chỉ tính các công thức thỏa mãn đồng thời 2 điều kiện: `Status == RecipeStatus.Published` VÀ `IsDeleted == false`. Bản nháp (`Draft`) hoặc bài viết đã xóa không được tính vào số lượng hiển thị.
5. **Giới hạn trường tóm tắt công thức (`FR-CAT-002`):**
   - Danh sách công thức con trong `CategoryDetailDto` chỉ nạp các trường tóm tắt gọn nhẹ phục vụ hiển thị thẻ món ăn (`RecipeCard`).
   - **Tuyệt đối KHÔNG tải** các trường dữ liệu nặng: `Instructions`, thông tin dinh dưỡng (`Nutrition`), các bước làm (`Steps`), nguyên liệu (`Ingredients`) để tiết kiệm băng thông và tối ưu I/O.

---

## Cấu trúc Response DTOs

### 1. `CategoryDto` (Dùng cho FR-CAT-001)
| Trường | Kiểu dữ liệu | Mô tả & Nguồn gốc |
| :--- | :--- | :--- |
| `Id` | Guid | Định danh duy nhất của danh mục (`Category.Id`) |
| `Name` | string | Tên hiển thị tiếng Việt (`Category.Name`) |
| `Slug` | string | Chuỗi định danh URL thân thiện (`Category.Slug`) |
| `Description` | string? | Mô tả ngắn gọn về danh mục (`Category.Description`) |
| `ImageUrl` | string? | URL hình ảnh đại diện danh mục (`Category.ImageUrl`) |
| `OrderIndex` | int | Thứ tự ưu tiên hiển thị trên giao diện (`Category.OrderIndex`) |
| `RecipeCount` | int | Tổng số công thức đã xuất bản thuộc danh mục |

### 2. `CategoryDetailDto` (Dùng cho FR-CAT-002)
| Trường | Kiểu dữ liệu | Mô tả & Nguồn gốc |
| :--- | :--- | :--- |
| `Id` | Guid | Định danh duy nhất (`Category.Id`) |
| `Name` | string | Tên danh mục (`Category.Name`) |
| `Slug` | string | Slug định danh URL (`Category.Slug`) |
| `Description` | string? | Mô tả danh mục (`Category.Description`) |
| `ImageUrl` | string? | Hình ảnh đại diện danh mục (`Category.ImageUrl`) |
| `Recipes` | `List<CategoryRecipeSummaryDto>` | Danh sách tóm tắt các công thức đã xuất bản |

### 3. `CategoryRecipeSummaryDto` (Thông tin tóm tắt công thức)
| Trường | Kiểu dữ liệu | Mô tả & Nguồn gốc |
| :--- | :--- | :--- |
| `Id` | Guid | ID công thức (`Recipe.Id`) |
| `Title` | string | Tiêu đề món ăn (`Recipe.Title`) |
| `Slug` | string | Slug xem chi tiết công thức (`Recipe.Slug`) |
| `CoverImageUrl` | string? | Ảnh đại diện chính (`IsPrimary == true`) |
| `CookTimeMinutes` | int | Thời gian nấu (phút) |
| `Difficulty` | `RecipeDifficulty` | Mức độ khó (`Easy`, `Medium`, `Hard`) |
| `CreatedAt` | DateTime | Thời điểm tạo bài viết (UTC) |

---

## Cơ chế Lưu đệm (Caching Strategy) & Thu hồi (Invalidation)

- **In-Memory Cache:** Danh sách danh mục được lưu đệm trong bộ nhớ RAM máy chủ bằng `IMemoryCache` với TTL 60 phút (3600 giây).
- **Thu hồi bộ đệm (Cache Invalidation):**
  - Khi thực hiện các thao tác quản trị làm thay đổi danh mục (Tạo mới `FR-CAT-003`, Cập nhật `FR-CAT-004`, Xóa `FR-CAT-005`), hệ thống bắt buộc kích hoạt lệnh xóa key:
    ```csharp
    _memoryCache.Remove("categories:all");
    ```
  - Lần truy vấn danh sách danh mục tiếp theo sẽ tự động đọc lại từ Supabase CSDL và nạp dữ liệu mới vào cache.

---

## Xử lý Ngoại lệ và Chuẩn lỗi (RFC 7807 Problem Details)

- Khi tìm kiếm danh mục theo `slug` mà không tìm thấy bản ghi hoặc bản ghi đã bị xóa mềm (`IsDeleted == true`):
  - Trả về mã lỗi: **`404 Not Found`**.
  - Content-Type: **`application/problem+json`**.
  - Cấu trúc phản hồi chuẩn RFC 7807:
    ```json
    {
      "status": 404,
      "title": "Not Found",
      "detail": "Không tìm thấy danh mục ẩm thực với slug 'mon-khong-ton-tai'",
      "instance": "/api/v1/categories/mon-khong-ton-tai"
    }
    ```

---

## Tiêu chí Nghiệm thu (Acceptance Criteria)

1. `GET /api/v1/categories` trả về mã `200 OK` kèm danh sách toàn bộ các danh mục ẩm thực đã sắp xếp theo `OrderIndex`.
2. Truy vấn danh sách lần thứ 2 có tốc độ phản hồi tức thì ($< 10\text{ms}$) do dữ liệu đã được nạp sẵn trong `IMemoryCache`.
3. Số lượng `RecipeCount` của mỗi danh mục phản ánh chính xác số bài viết ở trạng thái `Published` và chưa bị xóa mềm.
4. `GET /api/v1/categories/mon-chinh-dam-da` trả về mã `200 OK`, nạp đúng thông tin danh mục và mảng `recipes` chứa các bài viết liên quan.
5. Mỗi phần tử trong mảng `recipes` chỉ mang đúng 7 trường tóm tắt cần thiết; không tải thêm `Nutrition`, `Steps` hay `Ingredients`.
6. Truy vấn với slug không tồn tại (ví dụ: `/api/v1/categories/slug-sai`) trả về mã `404 Not Found` kèm thông điệp tiếng Việt theo chuẩn Problem Details.
7. Toàn bộ hai endpoint xuất hiện đầy đủ metadata (Summary, Description, Response Types) trên giao diện tài liệu tương tác **Scalar UI** (`http://localhost:5000/scalar/v1`).
