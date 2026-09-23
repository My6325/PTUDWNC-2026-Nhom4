# DỰ ÁN: CULINARY BLOG – BLOG ẨM THỰC VÀ NẤU ĂN
> **Môn học:** Phát triển Ứng dụng Web Nâng cao (PTUDWNC)  
> **Hệ thống:** API-Driven Architecture (.NET 10 Minimal APIs + Next.js 15 App Router + Supabase Cloud)  
> **Nhóm thực hiện:** Nhóm 4  

---

## 1. THÔNG TIN SINH VIÊN VÀ PHÂN CÔNG CHỨC NĂNG (NHÓM 4 THÀNH VIÊN)

> **Ghi chú phân bổ cân bằng:** Toàn bộ danh mục chức năng dưới đây bám sát **100% tài liệu đặc tả gốc SRS v1.0.0 (IEEE 830)** gồm đúng **27 Yêu cầu Chức năng (FR)** (được chi tiết hóa thành các đầu việc cụ thể).  
> Khối lượng công việc đã được phân bổ lại đồng đều: Nhóm trưởng nhận thêm 2 chức năng liên quan (`FR-OBS-002` và `FR-OBS-003`) để quản trị trọn vẹn toàn bộ Module Giám sát & Quan sát Hệ thống, đảm bảo độ khó từ **Trung bình đến Khá** và số lượng chức năng ít hơn 1 đầu việc so với thành viên khác để cân đối với vai trò quản trị dự án.

### 👤 Thành viên 1 (Trưởng nhóm): Nguyễn Thị Trà My — MSSV: 2312693
* **Module đảm nhiệm:** Module Quản lý Danh mục (FR-CAT) & Module Giám sát & Quan sát Hệ thống (FR-OBS)
* **Mức độ** (CRUD Danh mục, Thuật toán Slug, Health Checks, Structured Logging và Distributed Tracing).
* **Chi tiết chức năng (theo đúng SRS v1.0.0):**
  - `FR-CAT-001`: Xem Danh sách Danh mục (Get Categories, IMemoryCache 60m).
  - `FR-CAT-002`: Xem Chi tiết Danh mục (Get Category by Slug qua `/api/v1/categories/{slug}`, nạp danh sách tóm tắt các món thuộc danh mục; lưu ý: chức năng xem chi tiết công thức nấu ăn chuyên sâu FR-RCP-002 do TV4 phụ trách).
  - `FR-CAT-003`: Tạo Danh mục Mới [Admin] (Create Category, auto-slugify).
  - `FR-CAT-004`: Cập nhật Danh mục [Admin] (Update Category, giữ nguyên Slug).
  - `FR-CAT-005`: Xóa Danh mục [Admin] (Delete Category, cấm xóa khi còn công thức).
  - `FR-OBS-001`: Health Check Endpoints (`/health`, `/health/live`, `/health/ready` kiểm tra kết nối Supabase).
  - `FR-OBS-002`: Structured Logging (Serilog với CorrelationId, cảnh báo request chậm > 500ms).
  - `FR-OBS-003`: Distributed Tracing & Metrics (OpenTelemetry .NET SDK thu thập traces OTLP, cấu hình Scalar UI).
* **Nhiệm vụ điều phối nhóm:** Khởi tạo Solution Clean Architecture, kết nối Supabase Cloud, quản lý kho mã nguồn Git, phân công và kiểm duyệt Pull Request.

---

### 👤 Thành viên 2: Hoàng Trịnh Việt Linh — MSSV: 2312664
* **Module đảm nhiệm:** Module Xác thực và Quản lý Người dùng (FR-AUTH) & Background Email (FR-JOB)
* **Mức độ** (An ninh mật mã, Token Rotation, chống tấn công Replay và OAuth 2.0 PKCE).
* **Chi tiết chức năng (theo đúng SRS v1.0.0):**
  - `FR-AUTH-001`: Đăng ký Tài khoản (User Registration - Hash PBKDF2, auto-login).
  - `FR-AUTH-002`: Đăng nhập bằng Email/Mật khẩu (Local Login - Lockout 5 lần).
  - `FR-AUTH-003`: Đăng nhập bằng Google OAuth 2.0 (Authorization Code Flow + PKCE).
  - `FR-AUTH-004`: Làm mới Access Token (Token Refresh - Rotation & Reuse Detection).
  - `FR-AUTH-005`: Đăng xuất (Logout / Token Revocation).
  - `FR-AUTH-006`: Xem Hồ sơ Cá nhân (View Profile qua `/api/v1/auth/me`).
  - `FR-AUTH-007`: Cập nhật Hồ sơ Cá nhân (Update Profile - PATCH DisplayName, Avatar, Bio).
  - `FR-JOB-001`: Welcome Email Job (Tác vụ gửi email chào mừng qua Hangfire fire-and-forget).

---

### 👤 Thành viên 3: Phan Khánh Vương — MSSV: 2312802
* **Module đảm nhiệm:** Module Quản lý Công thức Lõi (FR-RCP), Tệp tin (FR-FILE) & Resize Ảnh (FR-JOB)
* **Mức độ** (Aggregate Root phức hợp, Concurrency Token RowVersion, Supabase Storage).
* **Chi tiết chức năng (theo đúng SRS v1.0.0):**
  - `FR-RCP-003`: Tạo Công thức Nấu ăn Mới [Author/Admin] (Khởi tạo trạng thái Draft, auto-slug).
  - `FR-RCP-004`: Cập nhật Công thức [Author-Owner/Admin] (Kiểm soát đồng thời Concurrency RowVersion).
  - `FR-RCP-007`: Xóa Công thức [Author-Owner/Admin] (Cascade delete steps/ingredients, dọn ảnh Supabase Storage).
  - `FR-RCP-008`: Quản lý Ảnh Công thức (Upload nhiều ảnh, tự gán IsPrimary, xóa ảnh).
  - `FR-RCP-009`: Quản lý Nguyên liệu (CRUD RecipeIngredient - Tên, Số lượng, Đơn vị, Thứ tự).
  - `FR-RCP-010`: Quản lý Các bước Thực hiện (CRUD RecipeStep - Tự động renumbering khi xóa bước).
  - `FR-FILE-001`: Upload File lên Supabase Storage (Validate MIME, Magic Bytes, GUID filename, tối đa 5MB).
  - `FR-FILE-002`: Xóa File khỏi Supabase Storage (Idempotent file removal).
  - `FR-JOB-002`: Image Resize / Thumbnail Job (Hangfire tự sinh ảnh 300x300 và 800x600 đưa lên Supabase).

---

### 👤 Thành viên 4: Lê Phạm Mi Đoan — MSSV: 2312597
* **Module đảm nhiệm:** Module Xuất bản, Tìm kiếm & Phân trang (FR-SRCH) & SEO Sitemap (FR-JOB)
* **Mức độ** (Tối ưu hóa FTS tiếng Việt unaccent, phân trang đa tiêu chí, Projection và Schema.org).
* **Chi tiết chức năng (theo đúng SRS v1.0.0):**
  - `FR-RCP-001`: Xem Danh sách Công thức (Paginated + Filtered + Sorted, Output Cache 15m).
  - `FR-RCP-002`: Xem Chi tiết Công thức Nấu ăn (Recipe Detail Eager Loading qua `/api/v1/recipes/{slug}`, nạp đầy đủ Steps, Ingredients, Nutrition, Images và nhúng SEO Schema.org).
  - `FR-RCP-005`: Xuất bản / Hủy Xuất bản Công thức (Publish/Unpublish - Ràng buộc >= 1 bước & nguyên liệu).
  - `FR-RCP-006`: Lưu trữ Công thức (Archive / Unarchive - Ẩn khỏi trang chủ).
  - `FR-SRCH-001`: Tìm kiếm Toàn văn bản (Supabase PostgreSQL FTS tiếng Việt `tsvector`/`tsquery`, `unaccent`, `ts_rank`).
  - `FR-SRCH-002`: Lọc đa tiêu chí (Faceted Search: Danh mục, Độ khó, Thời gian nấu, Khẩu phần).
  - `FR-SRCH-003`: Sắp xếp động (Dynamic Sorting theo Ngày tạo, Thời gian nấu, Độ liên quan FTS).
  - `FR-SRCH-004`: Phân trang an toàn (`PaginatedResult<T>` giới hạn tối đa 50 kết quả/trang).
  - `FR-JOB-003`: Sitemap Generation Job (Hangfire Recurring Job 02:00 AM UTC tạo sitemap.xml).

---

## 2. THÔNG TIN MÔ TẢ CHƯƠNG TRÌNH CƠ BẢN

### 2.1. Tiêu đề Dự án
**CULINARY BLOG — NỀN TẢNG CHIA SẺ VÀ KHÁM PHÁ CÔNG THỨC ẨM THỰC CHUYÊN NGHIỆP**  

### 2.2. Giới thiệu Tổng quan
Culinary Blog là giải pháp ứng dụng web toàn diện phục vụ cộng đồng đam mê nấu nướng và sáng tạo ẩm thực:
- **Dành cho Độc giả:** Khám phá hàng ngàn công thức nấu ăn qua công cụ tìm kiếm tiếng Việt thông minh (không cần gõ dấu), bộ lọc theo độ khó, thời gian nấu và khẩu phần; hướng dẫn chi tiết từng bước có hẹn giờ cùng bảng phân tích giá trị dinh dưỡng chuẩn xác.
- **Dành cho Tác giả:** Không gian xuất bản bài viết chuyên nghiệp với giao diện kéo thả ảnh lên Supabase Storage, định lượng nguyên liệu linh hoạt, quản lý vòng đời bài viết (Soạn thảo `Draft` <-> Xuất bản `Published` <-> Lưu trữ `Archived`).
- **Nền tảng kiến trúc hiện đại:** Xây dựng theo mô hình **API-Driven Architecture** tách rời hoàn toàn giữa Backend (.NET 10 Minimal APIs tuân thủ Clean Architecture + CQRS) và Frontend (Next.js 15 App Router tối ưu SEO và Core Web Vitals).

### 2.3. Các Công nghệ Chủ đạo
- **Backend API:** .NET 10 Minimal APIs, C# 13, MediatR (CQRS), Mapster, FluentValidation.
- **Frontend Web:** Next.js 15 (App Router), React 19, TypeScript, Tailwind CSS, TanStack Query, React Hook Form, Zod.
- **Cơ sở dữ liệu & Lưu trữ Đám mây:** **Supabase Cloud PostgreSQL 16** (EF Core 10 Code-First), **Supabase Storage** (Bucket `culinary-blog`).
- **Bộ nhớ đệm & Tác vụ ngầm:** In-Memory Cache (IMemoryCache TTL 60m cho danh mục, Output Cache cho bài viết), Hangfire (PostgreSQL storage).
- **Tài liệu hóa & Quan sát:** Scalar UI (OpenAPI 3.x native), Serilog, OpenTelemetry.

---

## 3. HƯỚNG DẪN CÀI ĐẶT CHƯƠNG TRÌNH & MÔI TRƯỜNG CHẠY

### 3.1. Các Chương trình và Môi trường Khuyến nghị
1. **Hệ điều hành:** Windows 10/11, macOS hoặc Linux.
2. **Môi trường Lập trình (IDE / Editor):**
   - *Backend:* **Visual Studio 2022** (v17.12+) hoặc **JetBrains Rider 2024+** (hoặc VS Code với extension *C# Dev Kit*).
   - *Frontend:* **Visual Studio Code** (với extensions: *Tailwind CSS IntelliSense, Prettier, ESLint*).
3. **Môi trường Thực thi (Runtimes & SDKs):**
   - **.NET 10 SDK** (phiên bản `10.0.x` trở lên)
   - **Node.js** (phiên bản `20.x LTS` hoặc `22.x LTS` kèm `npm 10+`)
4. **Hạ tầng CSDL & Lưu trữ (Supabase):**
   - **KHÔNG CẦN CÀI ĐẶT DOCKER HAY POSTGRESQL CỤC BỘ**.
   - Dự án kết nối trực tiếp đến **Supabase Cloud PostgreSQL 16** và **Supabase Storage** đã cấu hình sẵn.

---

### 3.2. Hướng dẫn Cài đặt Công cụ Dòng lệnh Toàn cục (CLI Tools)
Mở PowerShell chạy lệnh:
```powershell
# Cài đặt công cụ EF Core CLI để quản lý database migration
dotnet tool install --global dotnet-ef
```

---

### 3.3. Cài đặt và Chạy Backend API (.NET 10)

#### Cách 1: Chạy trực tiếp từ thư mục gốc dự án (Không cần chuyển thư mục vào src)
```powershell
# Khởi chạy máy chủ API thông thường:
dotnet run --project src/CulinaryBlog.API

# Hoặc khởi chạy với chế độ Hot-Reload (tự động nhận code mới khi sửa file mà không cần tắt/bật lại server):
dotnet watch --project src/CulinaryBlog.API run
```

#### Cách 2: Di chuyển vào thư mục API rồi mới chạy
```powershell
cd src/CulinaryBlog.API
dotnet run             # hoặc: dotnet watch run (Hot-Reload)
```
- **Địa chỉ API Backend:** `http://localhost:5000`
- **Tài liệu API tương tác Scalar UI:** `http://localhost:5000/scalar/v1` (hoặc vào thẳng `http://localhost:5000`)
- **Hangfire Dashboard:** `http://localhost:5000/hangfire`

---

### 3.4. Cài đặt và Chạy Frontend (Next.js 15 App Router)
Mở một cửa sổ Terminal mới:
```powershell
# 1. Di chuyển vào thư mục Frontend
cd src/frontend

# 2. Cài đặt các gói thư viện
npm install

# 3. Khởi chạy môi trường phát triển
npm run dev
```
- **Địa chỉ Giao diện Web:** `http://localhost:3000`

---

## 4. QUY CÁCH LÀM VIỆC NHÓM (TEAM WORKFLOW)

### 4.1. Phân nhánh Git (Git Branching)
- **Nhánh `main`:** Mã nguồn chính thức, được bảo vệ. **Chỉ Trưởng nhóm có quyền Merge vào nhánh này**.
- **Nhánh tính năng:** Các thành viên tạo nhánh riêng từ `main` theo cú pháp:
  - `feature/<tên-thành-viên>-<tên-chức-năng>`  
    *(Ví dụ: `feature/my-category-observability`, `feature/linh-auth-mailer`, `feature/vuong-recipe-media`, `feature/doan-search-publish`)*.

### 4.2. Quy trình Nộp bài & Hợp nhất (Pull Request & Merge)
1. **Tự kiểm tra:** Trước khi tạo PR, thành viên bắt buộc kiểm tra chạy thử trên máy cá nhân:
   - Backend: `dotnet build` (**0 Error, 0 Warning**).
   - Frontend: `npm run build` (**0 Error**).
2. **Tạo Pull Request:** Tạo PR từ nhánh cá nhân vào nhánh `main` và thông báo cho Trưởng nhóm.
3. **Kiểm duyệt & Merge:** **Trưởng nhóm trực tiếp review code, giải quyết conflict (nếu có) và bấm Merge vào nhánh `main`**.

### 4.3. Quy chuẩn Commit (Conventional Commits)
Ghi rõ loại thay đổi ở đầu thông điệp commit:
- `feat:` Thêm chức năng mới
- `fix:` Sửa lỗi
- `refactor:` Tối ưu / dọn dẹp mã nguồn
- `docs:` Cập nhật tài liệu
- `test:` Bổ sung kiểm thử
