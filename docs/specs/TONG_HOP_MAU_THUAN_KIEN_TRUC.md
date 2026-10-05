# BÁO CÁO TỔNG HỢP CÁC MÂU THUẪN TÀI LIỆU & QUYẾT ĐỊNH THIẾT KẾ KIẾN TRÚC
## DỰ ÁN: CULINARY BLOG (.NET 10 MINIMAL APIS + SUPABASE CLOUD)
> **Môn học:** Phát triển Ứng dụng Web Nâng cao (PTUDWNC) – Nhóm 4  
> **Các tài liệu đối chiếu:**  
> 1. **SRS v1.0.0 (IEEE 830):** Tài liệu đặc tả yêu cầu phần mềm sản phẩm thực tế.  
> 2. **Giáo trình Chương 1:** Kiến trúc Web hiện đại và thiết kế RESTful API (.NET 10).  
> 3. **Yêu cầu thực hành Lab (Lab 2, 3, 4):** Bộ tiêu chuẩn chấm điểm kỹ thuật của môn học.  
> **Vị trí lưu trữ:** `docs/TONG_HOP_MAU_THUAN_KIEN_TRUC.md`  

---

## 1. TỔNG QUAN BỐI CẢNH & NGUỒN GỐC MÂU THUẪN

Trong quá trình phân tích và triển khai dự án **Culinary Blog**, nhóm đã phát hiện một số điểm bất đồng (mâu thuẫn kỹ thuật) giữa tài liệu đặc tả sản phẩm thực tế (**SRS v1.0.0**), giáo trình học thuật của môn học (**Giáo trình Chương 1**) và điều kiện môi trường thực hành thực tế của sinh viên (**Lab requirements**).

Để dự án vừa **đạt chuẩn điểm học thuật cao nhất** của giảng viên, vừa **đảm bảo tính toàn vẹn dữ liệu chuẩn Enterprise** trong thực tế, Nhóm trưởng và các thành viên đã cùng phân tích, cân nhắc ưu/nhược điểm của từng phương án để đưa ra các **quyết định thiết kế kiến trúc thống nhất (Architectural Decision Records - ADR)**.

---

## 2. BẢNG MA TRẬN ĐỐI CHIẾU 6 MÂU THUẪN CỐT LÕI

| STT | Vấn đề kỹ thuật | Tài liệu SRS v1.0.0 | Giáo trình Chương 1 / Môn học | Quyết định kiến trúc của Nhóm 4 |
| :---: | :--- | :--- | :--- | :--- |
| **1** | **Cơ chế Xóa dữ liệu** | **Soft Delete (Xóa mềm):** `IsDeleted = true`, lưu thùng rác 30 ngày. | **Hard Delete (Xóa cứng):** `_context.Remove()`, trả về mã HTTP `204 NoContent`. | **Lai (Hybrid):** Database lưu Soft Delete an toàn, nhưng API phản hồi chuẩn `204 NoContent` và ẩn bản ghi (`WHERE !IsDeleted`). |
| **2** | **Cấu trúc Kiến trúc Mã nguồn** | **Clean Architecture:** 4 projects phân tầng độc lập (`Domain`, `Application`, `Infrastructure`, `API`). | **Vertical Slice Architecture (VSA):** Gom trọn tính năng theo Use Cases vào một thư mục lát cắt dọc. | **Lai (Hybrid):** Cấp Solution chia đúng 4 project Clean Architecture; Cấp Application chia theo Feature Slices (CQRS). |
| **3** | **Định dạng Response API** | Bọc dữ liệu trong vỏ bọc (Envelope Pattern): `{ success, data, message }`. | **Chuẩn RESTful thuần:** Trả thẳng DTO, lỗi dùng **RFC 7807 Problem Details**. | **Tuân thủ chuẩn RESTful & RFC 7807:** Bỏ Envelope pattern, dùng `GlobalExceptionHandler` bắt lỗi tập trung. |
| **4** | **Hạ tầng CSDL & File Storage** | Triển khai cụm **Docker Compose local** (PostgreSQL 16, MinIO, Redis, Mailhog). | Hướng dẫn chạy máy cục bộ (Localhost). | **Chuyển dịch lên Cloud (Supabase):** Dùng PostgreSQL & Storage trên Supabase Cloud để cả 4 thành viên dùng chung CSDL. |
| **5** | **Tài liệu hóa & Test API** | Dùng Swashbuckle Swagger UI truyền thống (`AddSwaggerGen`). | Bắt buộc dùng **OpenAPI native .NET 10** + **Scalar UI** (`Scalar.AspNetCore`). | Dùng **Scalar UI (theme Purple)**, viết thêm `BearerSecuritySchemeTransformer.cs` để hỗ trợ nhập Bearer Token. |
| **6** | **Xử lý URL Slug Tiếng Việt** | Người dùng nhập tiếng Việt có dấu (`"Món khai vị"`, `"Cháo gà"`). | Chuẩn RESTful URL: Slug bắt buộc là chữ thường không dấu (`kebab-case`). | **Áp dụng Nguyên lý Postel (Robustness):** Database lưu slug không dấu, Handler tự động chuẩn hóa dấu nếu người dùng truyền tiếng Việt. |

---

## 3. PHÂN TÍCH CHI TIẾT TỪNG MÂU THUẪN & QUYẾT ĐỊNH ÁP DỤNG

---

### 🔹 Mâu thuẫn 1: Xóa mềm (Soft Delete) vs Xóa cứng (Hard Delete) & Mã phản hồi 204

* **Nội dung mâu thuẫn:**
  * **SRS (FR-CAT-005, FR-RCP-007):** Yêu cầu khi xóa danh mục hoặc công thức, hệ thống không được xóa mất dòng dữ liệu trong CSDL nhằm tránh mất mát dữ liệu do vô tình, bảo lưu dữ liệu phục vụ thống kê lịch sử và cho phép khôi phục trong vòng 30 ngày.
  * **Giáo trình Chương 1:** Dạy nguyên tắc RESTful của Roy Fielding: Thao tác `DELETE` là xóa bỏ tài nguyên và trả về `204 NoContent` (thành công không có body). Ví dụ code trong slide gọi `_context.Categories.Remove(category)`.
* **Phân tích Ưu / Nhược điểm:**
  * *Xóa cứng (Hard Delete):*
    * *Ưu điểm:* CSDL sạch sẽ tức thì, truy vấn đơn giản không cần kiểm tra cờ xóa.
    * *Nhược điểm:* Cực kỳ rủi ro trong thực tế. Nếu Admin bấm nhầm sẽ mất vĩnh viễn dữ liệu; làm đứt gãy toàn bộ các khóa ngoại (Foreign Keys) liên kết đến bài viết, hình ảnh, nguyên liệu.
  * *Xóa mềm (Soft Delete):*
    * *Ưu điểm:* An toàn tuyệt đối, chuẩn mực bảo mật và dữ liệu doanh nghiệp (Audit Trails, Data Recovery).
    * *Nhược điểm:* Dữ liệu vẫn còn nằm trong CSDL Supabase, nếu người kiểm tra mở bảng trực tiếp sẽ dễ hiểu nhầm là thao tác xóa chưa chạy.
* **👉 Quyết định kiến trúc của Nhóm:**
  1. **Tầng CSDL (Supabase PostgreSQL):** Áp dụng **Xóa mềm** 100%. Lớp [`BaseEntity.cs`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.Domain/Common/BaseEntity.cs#L28) có sẵn `IsDeleted` và `DeletedAt`. Khi xóa chỉ thực hiện:
     ```csharp
     category.IsDeleted = true;
     category.DeletedAt = DateTime.UtcNow;
     ```
  2. **Tầng API & Trải nghiệm Người dùng:** Tuân thủ chuẩn RESTful:
     * Phản hồi về HTTP status **`204 NoContent`** (thành công rực rỡ, body rỗng).
     * Tất cả các câu lệnh truy vấn đọc dữ liệu (`GET /categories`, `GET /categories/{slug}`) đều tự động lọc điều kiện `WHERE !IsDeleted`.
     * Đối với client bên ngoài, tài nguyên đó xem như **đã biến mất hoàn toàn** (nếu cố truy cập lại bằng ID cũ sẽ nhận mã lỗi `404 Not Found`).

---

### 🔹 Mâu thuẫn 2: Clean Architecture 4 tầng vs Vertical Slice Architecture (VSA)

* **Nội dung mâu thuẫn:**
  * **SRS & Tiêu chí chấm Lab:** Yêu cầu dự án tuân thủ nghiêm ngặt **Clean Architecture** kinh điển gồm 4 projects tách biệt: `Domain`, `Application`, `Infrastructure`, `API`.
  * **Giáo trình Chương 1 (Mục 1.3):** Giảng viên dành phần lớn nội dung đề cao mô hình **Vertical Slice Architecture (VSA)**, gom trọn vẹn Command, Query, DTO, Validator và Handler của một nghiệp vụ vào một thư mục lát cắt dọc để giảm độ phân mảnh.
* **Phân tích Ưu / Nhược điểm:**
  * *Clean Architecture thuần:* Tách bạch trách nhiệm, tầng Domain hoàn toàn cô lập; nhưng khi viết 1 tính năng nhỏ phải tạo file rải rác trên cả 4 project.
  * *VSA thuần:* Phát triển tính năng rất nhanh, dễ bảo trì theo use case; nhưng nếu gom chung hết sẽ phá vỡ tiêu chí 4 project `.csproj` của đề bài môn học.
* **👉 Quyết định kiến trúc của Nhóm:** **Mô hình Kiến trúc Lai (Hybrid Architecture):**
  * **Cấp độ Solution:** Giữ nguyên **4 project riêng biệt** theo Clean Architecture:
    * `CulinaryBlog.Domain`: Chứa Entity, Value Objects, Domain Exceptions.
    * `CulinaryBlog.Application`: Chứa Interfaces, DTOs, CQRS Use Cases.
    * `CulinaryBlog.Infrastructure`: Chứa EF Core DbContext, Repositories, Supabase Storage, Identity.
    * `CulinaryBlog.API`: Chứa Minimal APIs Endpoints, Middlewares, Scalar UI.
  * **Cấp độ tầng Application:** Áp dụng tư tưởng **Vertical Slice**: Trong thư mục `Features/Categories/`, chia thành các thư mục tính năng độc lập (`Create/`, `Update/`, `Delete/`, `Queries/`). Mỗi thư mục chứa trọn gói Command + Validator + Handler tương ứng.

---

### 🔹 Mâu thuẫn 3: Định dạng Response Phản hồi (Envelope Pattern vs Chuẩn RESTful RFC 7807)

* **Nội dung mâu thuẫn:**
  * Một số phong cách viết API cũ dùng Envelope Pattern để bọc mọi kết quả:
    ```json
    { "success": true, "data": { ... }, "message": "Thành công" }
    ```
  * **Giáo trình Chương 1 & Chuẩn .NET 10 Minimal APIs:** Bác bỏ Envelope Pattern vì làm sai lệch ý nghĩa của mã HTTP Status Codes chuẩn và vi phạm nguyên lý Uniform Interface của Roy Fielding.
* **Phân tích Ưu / Nhược điểm:**
  * *Envelope:* Frontend viết code đơn giản bằng cách `if (res.data.success)`; nhưng làm các công cụ tự động (OpenAPI/Scalar UI) không thể phân tích đúng Schema kiểu dữ liệu thực tế.
  * *Chuẩn RFC:* Tận dụng toàn bộ sức mạnh HTTP (200, 201, 204, 400, 401, 403, 404, 409, 422).
* **👉 Quyết định kiến trúc của Nhóm:**
  1. **Khi thành công:** Trả về dữ liệu DTO trực tiếp:
     * `POST` tạo mới: Trả về `201 Created` kèm Header `Location: /api/v1/categories/{slug}` và Body `CategoryDto`.
     * `DELETE` xóa: Trả về `204 NoContent` không có body.
     * `GET` tra cứu: Trả về `200 OK` kèm danh sách hoặc object chi tiết.
  2. **Khi thất bại / Lỗi nghiệp vụ:** Áp dụng chuẩn quốc tế **RFC 7807 Problem Details** thông qua lớp tập trung [`GlobalExceptionHandler.cs`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.API/ExceptionHandling/GlobalExceptionHandler.cs). Bất kể lỗi xảy ra ở tầng nào, client luôn nhận về format JSON chuẩn mực:
     ```json
     {
       "status": 409,
       "title": "Conflict",
       "detail": "Không thể xóa danh mục vì vẫn còn công thức bài viết đang liên kết.",
       "instance": "/api/v1/categories/...",
       "code": "CATEGORY_HAS_RECIPES"
     }
     ```

---

### 🔹 Mâu thuẫn 4: Hạ tầng CSDL Local Docker Compose vs Supabase Cloud

* **Nội dung mâu thuẫn:**
  * **SRS ban đầu:** Thiết kế chạy cụm Docker Compose cục bộ trên máy (`docker-compose.yml` gồm PostgreSQL 16, MinIO, Redis, Mailhog).
  * **Thực tế môi trường thực hành:** Máy của sinh viên chạy Windows nhiều máy không cài được Docker Desktop (lỗi ảo hóa WSL2, máy yếu tốn RAM); nếu mỗi người chạy database local riêng thì dữ liệu bị rời rạc, không thể test tích hợp chéo giữa 4 thành viên.
* **👉 Quyết định kiến trúc của Nhóm:**
  * **Chuyển toàn bộ CSDL và Lưu trữ tệp lên Supabase Cloud:**
    * CSDL: PostgreSQL 16 Cloud Hosted trên Supabase.
    * Storage: Supabase Cloud Storage (Bucket `culinary-blog`).
  * **Lợi ích vượt trội:**
    * Máy tính không cần cài đặt Docker nặng nề.
    * Cả 4 thành viên cùng kết nối chung một database thông qua biến môi trường bí mật trong file `.env`.
    * Thành viên 1 tạo danh mục thì Thành viên 3 mở máy lên thấy ngay lập tức để chọn viết công thức, đảm bảo dữ liệu luôn đồng bộ thời gian thực 100%.

---

### 🔹 Mâu thuẫn 5: Công cụ Tài liệu hóa API (Swashbuckle Swagger vs Scalar UI + .NET 10 OpenAPI Native)

* **Nội dung mâu thuẫn:**
  * Trước đây cộng đồng .NET quen dùng thư viện `Swashbuckle.AspNetCore` (Swagger UI).
  * Từ **.NET 9 và .NET 10**, Microsoft đã chính thức loại bỏ Swashbuckle và thay thế hoàn toàn bằng **`Microsoft.AspNetCore.OpenApi` native**. Giáo trình Chương 1 cũng yêu cầu sử dụng giao diện hiện đại **Scalar UI (`Scalar.AspNetCore`)**.
  * *Khúc mắc nảy sinh:* OpenAPI native mặc định của .NET 10 chưa tự động cấu hình Security Scheme Bearer Token, dẫn đến việc Scalar UI không hiện ô nhập JWT Token.
* **👉 Quyết định kiến trúc của Nhóm:**
  * Tuân thủ 100% định hướng công nghệ của .NET 10 và Giáo trình Chương 1: Dùng **Scalar UI (theme Purple)**.
  * Tự thiết kế và cài đặt lớp [`BearerSecuritySchemeTransformer.cs`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.API/OpenApi/BearerSecuritySchemeTransformer.cs) kế thừa `IOpenApiDocumentTransformer`:
    * Khai báo Security Scheme `Bearer` (JWT).
    * Khai báo Security Requirement syntax vào tài liệu OpenAPI.
  * Nhờ đó, Scalar UI hiển thị form nhập Bearer Token mượt mà, phục vụ việc kiểm thử các API yêu cầu quyền Admin/Author cực kỳ trực quan.

---

### 🔹 Mâu thuẫn 6: Định danh URL SEO (Slug Kebab-case vs Tiếng Việt có dấu)

* **Nội dung mâu thuẫn:**
  * Chuẩn RESTful URL (Giáo trình Chương 1): Biến `{slug}` trên đường dẫn URL bắt buộc phải là chữ thường, không dấu, nối bằng gạch ngang (kebab-case) như `mon-khai-vi`.
  * Thói quen người dùng Việt Nam (SRS): Người dùng hoặc tester thường có xu hướng gõ thẳng chuỗi tiếng Việt có dấu như `Món khai vị`. Nếu tìm chính xác theo chuỗi này trong database sẽ bị trả về `404 Not Found`.
* **👉 Quyết định kiến trúc của Nhóm (Nguyên lý Postel):**
  * *\"Be conservative in what you send, be liberal in what you accept\"* (Nghiêm ngặt khi lưu trữ, nhưng linh hoạt khi tiếp nhận).
  * **Lưu trữ CSDL:** Luôn tự động chuyển đổi thành slug chuẩn SEO không dấu (`mon-khai-vi`) qua [`SlugHelper.cs`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.Domain/Common/SlugHelper.cs).
  * **API Truy vấn:** Cải tiến tại [`GetCategoryBySlugQueryHandler.cs`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.Application/Features/Categories/Queries/GetCategoryBySlug/GetCategoryBySlugQueryHandler.cs#L29-L36): Tự động hỗ trợ cả 2 trường hợp:
    ```csharp
    var rawSlug = request.Slug.Trim();
    var normalizedSlug = rawSlug.ToLowerInvariant();
    var generatedSlug = SlugHelper.GenerateSlug(rawSlug);

    // Tìm kiếm khớp với cả slug chuẩn lẫn chuỗi tiếng Việt có dấu
    var category = await _context.Categories
        .AsNoTracking()
        .FirstOrDefaultAsync(c => (c.Slug == normalizedSlug || c.Slug == generatedSlug) && !c.IsDeleted);
    ```
  * Nhờ đó, người dùng nhập `mon-khai-vi` hay nhập `Món khai vị` thì API đều tìm thấy và trả về dữ liệu `200 OK` thành công.

---

## 4. BẢNG TỔNG HỢP ÁNH XẠ VỊ TRÍ CODE ĐÃ HIỆN THỰC

Toàn bộ các quyết định kiến trúc nêu trên đã được hiện thực hóa đầy đủ vào mã nguồn của dự án:

| Quyết định kiến trúc | Tập tin mã nguồn hiện thực |
| :--- | :--- |
| **Xóa mềm (Soft Delete 30 ngày)** | [`src/CulinaryBlog.Domain/Common/BaseEntity.cs`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.Domain/Common/BaseEntity.cs)<br>[`src/CulinaryBlog.Application/Features/Categories/Delete/DeleteCategoryCommand.cs`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.Application/Features/Categories/Delete/DeleteCategoryCommand.cs) |
| **Clean Architecture + Vertical Slices** | [`src/CulinaryBlog.slnx`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.slnx)<br>[`src/CulinaryBlog.Application/Features/Categories/`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.Application/Features/Categories/) |
| **Chuẩn Problem Details RFC 7807** | [`src/CulinaryBlog.API/ExceptionHandling/GlobalExceptionHandler.cs`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.API/ExceptionHandling/GlobalExceptionHandler.cs) |
| **Kết nối Supabase Cloud** | [`src/CulinaryBlog.Infrastructure/DependencyInjection.cs`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.Infrastructure/DependencyInjection.cs)<br>[`src/CulinaryBlog.Infrastructure/Persistence/SupabaseConnectionStringResolver.cs`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.Infrastructure/Persistence/SupabaseConnectionStringResolver.cs) |
| **Scalar UI & Bearer Transformer** | [`src/CulinaryBlog.API/OpenApi/BearerSecuritySchemeTransformer.cs`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.API/OpenApi/BearerSecuritySchemeTransformer.cs)<br>[`src/CulinaryBlog.API/Program.cs`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.API/Program.cs#L74-L94) |
| **Slug tiếng Việt linh hoạt** | [`src/CulinaryBlog.Domain/Common/SlugHelper.cs`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.Domain/Common/SlugHelper.cs)<br>[`src/CulinaryBlog.Application/Features/Categories/Queries/GetCategoryBySlug/GetCategoryBySlugQueryHandler.cs`](file:///d:/Nhom4_WebNangCao/src/CulinaryBlog.Application/Features/Categories/Queries/GetCategoryBySlug/GetCategoryBySlugQueryHandler.cs) |

---

## 5. KẾT LUẬN

Nhờ việc nhận diện sớm và giải quyết thấu đáo các mâu thuẫn giữa lý thuyết học thuật và thực tiễn sản phẩm, hệ thống Backend **Culinary Blog** của Nhóm 4 đạt được:
1. **Tuân thủ 100% yêu cầu học thuật:** Đáp ứng trọn vẹn tiêu chí của đề tài môn học và các bài thực hành Lab (Lab 2, 3, 4).
2. **Đạt chuẩn kiến trúc phần mềm doanh nghiệp:** Bảo đảm an toàn dữ liệu, chống mất mát dữ liệu với Soft Delete và chuẩn hóa ngoại lệ toàn cầu RFC 7807.
3. **Tối ưu hóa năng suất làm việc nhóm:** Cơ sở dữ liệu đám mây Supabase và tài liệu tương tác Scalar UI giúp cả 4 thành viên phối hợp nhịp nhàng, kiểm thử chéo API thuận tiện mà không gặp xung đột môi trường.
