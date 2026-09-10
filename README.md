# DỰ ÁN: CULINARY BLOG – BLOG ẨM THỰC VÀ NẤU ĂN
> **Môn học:** Phát triển Ứng dụng Web Nâng cao 
> **Hệ thống:** API-Driven Architecture (.NET 10 Minimal APIs + Next.js 15 App Router)  
> **Nhóm thực hiện:** Nhóm 4  

---

## 1. THÔNG TIN SINH VIÊN VÀ PHÂN CÔNG CHỨC NĂNG (NHÓM 4 THÀNH VIÊN)

> **Ghi chú:** Chức năng được phân chia dựa trên Chương 3 của tài liệu Đặc tả Yêu cầu SRS v1.0.0. Mỗi thành viên chịu trách nhiệm trọn vẹn cả 3 phần (**Database + Backend + Frontend**) cho gói chức năng của mình.

### 👤 Thành viên 1 (Trưởng nhóm): Nguyễn Thị Trà My — MSSV: 2312693
* **Module đảm nhiệm:** Module Quản lý Danh mục (FR-CAT) & Kiểm tra Hệ thống (FR-OBS)
* **Chi tiết chức năng (theo mục lục tài liệu):**
  - `FR-CAT-001`: Xem Danh sách Danh mục (Get Categories, IMemoryCache 60m)
  - `FR-CAT-002`: Xem Chi tiết Danh mục và Công thức (Get Category by Slug)
  - `FR-CAT-003`: Tạo Danh mục Mới [Admin] (Create Category, auto-slugify)
  - `FR-CAT-004`: Cập nhật Danh mục [Admin] (Update Category, giữ nguyên Slug)
  - `FR-CAT-005`: Xóa Danh mục [Admin] (Delete Category, cấm xóa khi còn công thức)
  - `FR-OBS-001`: Health Check Endpoints (`/health`, `/health/live`, `/health/ready`)
* **Nhiệm vụ điều phối nhóm:** Khởi tạo Solution Clean Architecture ban đầu, quản lý kho mã nguồn Git và điều phối công việc chung.

---

### 👤 Thành viên 2: Phan Khánh Vương — MSSV: 2312802
* **Module đảm nhiệm:** Module Xác thực và Quản lý Người dùng (FR-AUTH) & Background Email (FR-JOB)
* **Chi tiết chức năng (theo mục lục tài liệu):**
  - `FR-AUTH-001`: Đăng ký Tài khoản (User Registration - Hash PBKDF2, auto-login)
  - `FR-AUTH-002`: Đăng nhập bằng Email/Mật khẩu (Local Login - Lockout 5 lần)
  - `FR-AUTH-003`: Đăng nhập bằng Google OAuth 2.0 (Authorization Code Flow + PKCE)
  - `FR-AUTH-004`: Làm mới Access Token (Token Refresh - Rotation & Reuse Detection)
  - `FR-AUTH-005`: Đăng xuất (Logout / Token Revocation)
  - `FR-AUTH-006`: Xem Hồ sơ Cá nhân (View Profile)
  - `FR-AUTH-007`: Cập nhật Hồ sơ Cá nhân (Update Profile - PATCH DisplayName, Avatar, Bio)
  - `FR-JOB-001`: Welcome Email Job (Tác vụ gửi email chào mừng qua Hangfire fire-and-forget)

---

### 👤 Thành viên 3: Lê Phạm Mi Đoan — MSSV: 2312597
* **Module đảm nhiệm:** Module Quản lý Công thức Nấu ăn Lõi (FR-RCP), Tệp tin (FR-FILE) & Resize Ảnh (FR-JOB)
* **Chi tiết chức năng (theo mục lục tài liệu):**
  - `FR-RCP-003`: Tạo Công thức Nấu ăn Mới [Author/Admin] (Khởi tạo trạng thái Draft, auto-slug)
  - `FR-RCP-004`: Cập nhật Công thức [Author-Owner/Admin] (Kiểm soát đồng thời Concurrency RowVersion)
  - `FR-RCP-007`: Xóa Công thức [Author-Owner/Admin] (Cascade delete steps/ingredients, dọn ảnh MinIO)
  - `FR-RCP-008`: Quản lý Ảnh Công thức (Upload ảnh, tự gán IsPrimary, xóa ảnh)
  - `FR-RCP-009`: Quản lý Nguyên liệu (CRUD RecipeIngredient - Tên, Số lượng, Đơn vị, Thứ tự)
  - `FR-RCP-010`: Quản lý Các bước Thực hiện (CRUD RecipeStep - Tự động renumbering khi xóa bước)
  - `FR-FILE-001`: Upload File lên MinIO (Validate MIME, Magic Bytes, GUID filename, tối đa 5MB)
  - `FR-FILE-002`: Xóa File khỏi MinIO (Idempotent file removal)
  - `FR-JOB-002`: Image Resize / Thumbnail Job (Hangfire tự sinh ảnh 300x300 và 800x600)

---

### 👤 Thành viên 4: Hoàng Trịnh Việt Linh — MSSV: 2312664
* **Module đảm nhiệm:** Module Xuất bản, Tìm kiếm & Phân trang (FR-SRCH), SEO Sitemap & Giám sát (FR-OBS)
* **Chi tiết chức năng (theo mục lục tài liệu):**
  - `FR-RCP-001`: Xem Danh sách Công thức (Paginated + Filtered + Sorted, Output Cache 15m)
  - `FR-RCP-002`: Xem Chi tiết Công thức (Recipe Detail Eager Loading, Output Cache 60m)
  - `FR-RCP-005`: Xuất bản / Hủy Xuất bản Công thức (Publish/Unpublish - Ràng buộc $\ge 1$ bước & nguyên liệu)
  - `FR-RCP-006`: Lưu trữ Công thức (Archive / Unarchive - Ẩn khỏi trang chủ)
  - `FR-SRCH-001`: Tìm kiếm Toàn văn bản (PostgreSQL FTS tiếng Việt `tsvector`/`tsquery`, `unaccent`, `ts_rank`)
  - `FR-SRCH-002/003/004`: Lọc đa tiêu chí, Sắp xếp động và Phân trang `PaginatedResult<T>`
  - `FR-JOB-003`: Sitemap Generation Job (Hangfire Recurring Job 02:00 AM UTC tạo sitemap.xml)
  - `FR-OBS-002`: Structured Logging (Serilog với CorrelationId, cảnh báo > 500ms)
  - `FR-OBS-003`: Distributed Tracing & Metrics (OpenTelemetry .NET SDK, cấu hình Scalar UI)

---

## 2. THÔNG TIN MÔ TẢ CHƯƠNG TRÌNH CƠ BẢN

### 2.1. Tiêu đề Dự án
**CULINARY BLOG — NỀN TẢNG CHIA SẺ VÀ KHÁM PHÁ CÔNG THỨC ẨM THỰC CHUYÊN NGHIỆP**  

### 2.2. Giới thiệu Tổng quan
Culinary Blog là giải pháp ứng dụng web toàn diện phục vụ cộng đồng đam mê nấu nướng và sáng tạo ẩm thực:
- **Dành cho Độc giả:** Khám phá hàng ngàn công thức nấu ăn qua công cụ tìm kiếm tiếng Việt thông minh (không cần gõ dấu), bộ lọc theo độ khó, thời gian nấu và khẩu phần; hướng dẫn chi tiết từng bước có hẹn giờ cùng bảng phân tích giá trị dinh dưỡng chuẩn xác.
- **Dành cho Tác giả:** Không gian xuất bản bài viết chuyên nghiệp với giao diện kéo thả ảnh đa kích thước, định lượng nguyên liệu linh hoạt, quản lý vòng đời bài viết (Soạn thảo `Draft` $\rightarrow$ Xuất bản `Published` $\rightarrow$ Lưu trữ `Archived`).
- **Nền tảng kiến trúc hiện đại:** Xây dựng theo mô hình **API-Driven Architecture** tách rời hoàn toàn giữa Backend (.NET 10 Minimal APIs tuân thủ Clean Architecture + CQRS) và Frontend (Next.js 15 App Router tối ưu SEO và Core Web Vitals).

### 2.3. Các Công nghệ Chủ đạo
- **Backend API:** .NET 10 Minimal APIs, C# 13, MediatR (CQRS), Mapster, FluentValidation.
- **Frontend Web:** Next.js 15 (App Router), React 19, TypeScript, Tailwind CSS, TanStack Query, React Hook Form, Zod.
- **Cơ sở dữ liệu & Cache:** PostgreSQL 16 (EF Core 10 Code-First), Redis 7, .NET Output Cache.
- **Lưu trữ & Tác vụ ngầm:** MinIO S3-Compatible Storage, Hangfire (PostgreSQL storage).
- **Tài liệu hóa & Quan sát:** Scalar UI (OpenAPI 3.x native), Serilog, OpenTelemetry.

---

## 3. HƯỚNG DẪN CÀI ĐẶT CHƯƠNG TRÌNH & MÔI TRƯỜNG CHẠY

### 3.1. Các Chương trình và Môi trường Khuyến nghị
1. **Hệ điều hành:** Windows 10/11 hoặc Ubuntu Linux.
2. **Môi trường Lập trình (IDE / Editor):**
   - *Khuyên dùng cho Backend:* **Visual Studio 2022** (v17.12+) hoặc **JetBrains Rider 2024+** (hoặc VS Code với extension *C# Dev Kit*).
   - *Khuyên dùng cho Frontend:* **Visual Studio Code** (với extensions: *Tailwind CSS IntelliSense, Prettier, ESLint*).
3. **Môi trường Thực thi (Runtimes & SDKs):**
   - **.NET 10 SDK** (phiên bản `10.0.x` trở lên)
   - **Node.js** (phiên bản `20.x LTS` hoặc `22.x LTS` kèm `npm 10+`)
4. **Hệ quản trị CSDL & Dịch vụ Phụ trợ:**
   - **PostgreSQL 16.x** (bắt buộc kích hoạt extensions: `unaccent` và `pg_trgm`)
   - **Docker Desktop** (khuyên dùng để chạy tự động PostgreSQL, Redis, MinIO, Seq)
   - **Scalar UI** (tích hợp sẵn trong .NET 10, chạy tại `http://localhost:5000/scalar/v1`)

---

### 3.2. Hướng dẫn Cài đặt Công cụ Dòng lệnh Toàn cục (CLI Tools)
Mở PowerShell chạy lệnh:
```powershell
# Cài đặt công cụ EF Core CLI để quản lý database migration
dotnet tool install --global dotnet-ef
```

---

### 3.3. Khởi chạy Dịch vụ Nền tảng với Docker Compose (Cách nhanh nhất)
Chỉ với 1 câu lệnh để dựng toàn bộ CSDL và dịch vụ lưu trữ:
```powershell
# Khởi động PostgreSQL 16, Redis 7, MinIO, Seq, Mailhog
docker compose up -d

# Kiểm tra danh sách container
docker compose ps
```

---

### 3.4. Cài đặt và Chạy Backend API (.NET 10)
```powershell
# 1. Di chuyển vào thư mục dự án API
cd src/backend/CulinaryBlog.API

# 2. Khôi phục các gói NuGet
dotnet restore

# 3. Chạy cập nhật database (Migration)
dotnet ef database update --project ../CulinaryBlog.Infrastructure --startup-project .

# 4. Khởi chạy máy chủ API (chế độ hot-reload)
dotnet watch run
```
- **Địa chỉ API Backend:** `http://localhost:5000`
- **Tài liệu API tương tác Scalar UI:** `http://localhost:5000/scalar/v1`
- **Hangfire Dashboard:** `http://localhost:5000/hangfire`

---

### 3.5. Cài đặt và Chạy Frontend (Next.js 15)
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


## 4. QUY CÁCH LÀM VIỆC NHÓM (TEAM WORKFLOW)

### 4.1. Phân nhánh Git (Git Branching)
- **Nhánh `main`:** Mã nguồn chính thức, được bảo vệ. **Chỉ Trưởng nhóm có quyền Merge vào nhánh này**.
- **Nhánh tính năng:** Các thành viên tạo nhánh riêng từ `main` theo cú pháp:
  - `feature/<tên-thành-viên>-<tên-chức-năng>` (Ví dụ: `feature/nam-auth`, `feature/tuan-recipes`).
### 4.2. Quy trình Nộp bài & Hợp nhất (Pull Request & Merge)
1. **Tự kiểm tra:** Trước khi tạo PR, thành viên bắt buộc kiểm tra chạy thử trên máy cá nhân:
   - Backend: `dotnet build` (0 Error).
   - Frontend: `npm run build` (0 Error).
2. **Tạo Pull Request:** Tạo PR từ nhánh cá nhân vào nhánh `main` và thông báo cho Trưởng nhóm.
3. **Kiểm duyệt & Merge:** **Trưởng nhóm trực tiếp review code, giải quyết conflict (nếu có) và bấm Merge vào nhánh `main`**.
### 4.3. Quy chuẩn Commit (Conventional Commits)
Ghi rõ loại thay đổi ở đầu thông điệp commit:
- `feat:` Thêm chức năng mới
- `fix:` Sửa lỗi
- `refactor:` Tối ưu / dọn dẹp mã nguồn
- `docs:` Cập nhật tài liệu
