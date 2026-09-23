# FR-RCP-001 — Xem danh sách công thức

## Trạng thái

**Đã duyệt và triển khai.** Giới hạn PageSize tối đa 50 theo yêu cầu SRS.

## Mục tiêu

Cung cấp endpoint công khai để tìm kiếm, lọc, sắp xếp và phân trang các công thức đã xuất bản. Kết quả là một danh sách gọn, không tải các tập hợp `Steps` hoặc `Ingredients`.

## API

| Thuộc tính | Đặc tả |
| --- | --- |
| Method và route | `GET /api/v1/recipes` |
| Truy cập | Công khai, không yêu cầu xác thực |
| Thành công | `200 OK`, body là `PaginatedResult<RecipeListDto>` |
| Cache | ASP.NET Core Output Cache, thời hạn 900 giây, tag `recipes` |

### Query parameters

| Tên | Kiểu | Mặc định | Quy tắc |
| --- | --- | --- | --- |
| `SearchTerm` | string? | Không có | Từ khóa FTS; bỏ qua khi null/rỗng sau Trim |
| `CategoryId` | Guid? | Không có | Lọc chính xác theo ID danh mục |
| `Difficulty` | `RecipeDifficulty`? | Không có | Nhận giá trị enum hợp lệ: `Easy`, `Medium`, `Hard` |
| `SortBy` | string? | `newest` | Giá trị được hỗ trợ phải được xác định rõ trong phần sắp xếp bên dưới |
| `PageIndex` | int | `1` | Tối thiểu là 1 |
| `PageSize` | int | `10` | Trong khoảng 1–50, bao gồm hai đầu (theo giới hạn SRS) |

Tên query parameter dùng PascalCase theo yêu cầu; cần đảm bảo binder của Minimal API xử lý không phân biệt hoa thường theo cấu hình mặc định.

## Quy tắc dữ liệu và truy vấn

1. Chỉ trả về recipe có `Status = Published` và `IsDeleted = false`. Đây là endpoint công khai; bản nháp, bài lưu trữ và bản ghi đã xóa mềm không được lộ ra.
2. Bắt đầu truy vấn từ EF Core với `.AsNoTracking()`.
3. Áp dụng các bộ lọc tùy chọn trước khi đếm và phân trang.
4. Khi có `SearchTerm`, tìm kiếm trên tiêu đề và mô tả với PostgreSQL Full-Text Search cùng `unaccent`, theo quy ước đã có trong `RecipeRepository`. Không tải dữ liệu để lọc phía ứng dụng.
5. Tính `TotalCount` trên tập kết quả đã lọc, sau đó áp dụng `Skip((PageIndex - 1) * PageSize)` và `Take(PageSize)`. Trả metadata qua `PaginatedResult<T>` hiện có: `Items`, `TotalCount`, `PageIndex`, `PageSize`, `TotalPages`.
6. Project trực tiếp sang DTO ở truy vấn để chỉ lấy các cột cần thiết; không include hoặc serialize `Steps`/`Ingredients`.

## Sắp xếp

- `newest` (mặc định): `CreatedAt` giảm dần, rồi `Id` tăng dần để thứ tự ổn định giữa các trang.
- `popular`: **chưa thể định nghĩa chính xác theo schema hiện tại**. `Recipe` hiện không có lượt xem, lượt yêu thích hay điểm phổ biến. Trước khi triển khai giá trị này cần chọn nguồn đo lường (và có thể cần schema/thống kê mới). Nếu chưa có quyết định, chỉ `newest` được hỗ trợ và giá trị `popular` phải bị từ chối validation thay vì ngụy trang bằng một thứ tự khác.
- Không nhận tên cột hoặc biểu thức sắp xếp tùy ý từ client.

## Response DTO

`RecipeListDto` chỉ gồm các trường sau:

| Trường | Kiểu dự kiến | Nguồn |
| --- | --- | --- |
| `Id` | Guid | `Recipe.Id` |
| `Title` | string | `Recipe.Title` |
| `Slug` | string | `Recipe.Slug` |
| `CoverImageUrl` | string? | Ảnh chính (`IsPrimary`), ưu tiên URL Medium nếu có, fallback Original |
| `CategoryName` | string? | `Category.Name` nếu có danh mục |
| `AuthorName` | string | `ApplicationUser.DisplayName` |
| `Difficulty` | RecipeDifficulty | `Recipe.Difficulty` |
| `CreatedAt` | DateTime | `Recipe.CreatedAt` (UTC) |

Không đưa `Description`, `Instructions`, `Nutrition`, `Steps` hoặc `Ingredients` vào DTO. Nếu không có ảnh chính, `CoverImageUrl` là null. Cần xác nhận thứ tự ảnh fallback trong trường hợp dữ liệu có nhiều ảnh chính hoặc chưa có ảnh chính.

## Validation và lỗi đầu vào

`GetRecipesQueryValidator` kiểm tra:

- `PageIndex >= 1`.
- `1 <= PageSize <= 50`.
- `Difficulty` phải là giá trị enum đã định nghĩa.
- `SortBy` chỉ nhận giá trị được hỗ trợ (hiện tại `newest`; `popular` chờ định nghĩa metric).

Cách biểu diễn lỗi validation trên HTTP cần theo quy ước Problem Details hiện có của API. Mã trạng thái dự kiến `400 Bad Request`.

## Output caching

- Cấu hình Output Cache cho endpoint với `Duration = 900` giây và tag `recipes`.
- Dùng các query parameter trong cache key để các trang/bộ lọc khác nhau không dùng chung nội dung.
- Vì response là công khai và phụ thuộc query string, không đưa dữ liệu người dùng hoặc header xác thực vào response cache.
- Tag cho phép xóa các bản cache liên quan khi công thức được xuất bản, cập nhật hoặc gỡ khỏi công khai.

## Tiêu chí chấp nhận

1. `GET /api/v1/recipes` trả trang đầu gồm tối đa 10 recipe công khai mới nhất.
2. Có thể kết hợp tìm kiếm, danh mục và độ khó; `TotalCount` phản ánh tập đã áp dụng mọi bộ lọc.
3. Phân trang trả đúng metadata, không có bản ghi lặp/đảo thứ tự bất định khi các `CreatedAt` trùng nhau.
4. Payload mỗi item chỉ có tám trường DTO đã nêu; không tải Steps/Ingredients.
5. `PageIndex < 1`, `PageSize` ngoài [1,50], enum hoặc sort không hợp lệ trả lỗi validation theo chuẩn API.
6. Truy vấn dùng EF Core server-side, `AsNoTracking`, FTS `unaccent` khi có từ khóa, và projection trước khi materialize.
7. Output Cache giữ response 900 giây, phân biệt query string, gắn tag `recipes` và hỗ trợ eviction theo tag.

## Ghi chú phù hợp với codebase

- Domain đã có `RecipeDifficulty`, `RecipeStatus`, `PaginatedResult<T>` và các quan hệ Recipe–Category–Images; author dùng khóa string kiểu ASP.NET Identity.
- `RecipeRepository` hiện đã dùng `.AsNoTracking()` và biểu thức FTS `unaccent`, nhưng chưa lọc trạng thái công khai, chưa project DTO, chưa hỗ trợ Category/Difficulty/SortBy và hiện sắp xếp theo Title.
- Giới hạn `PageSize` được chốt theo SRS: tối đa 50.
- Metric cho `popular` và hành vi ảnh đại diện khi dữ liệu không nhất quán cần được chốt trước khi triển khai đầy đủ các nhánh tương ứng.
