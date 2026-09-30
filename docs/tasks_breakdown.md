# BẢNG PHÂN RÃ CÔNG VIỆC CHI TIẾT THEO TÍNH NĂNG (WORK BREAKDOWN STRUCTURE - WBS)
## DỰ ÁN: CULINARY BLOG (.NET 10 MINIMAL APIS + NEXT.JS 15 + SUPABASE)
> **Mã định danh:** `WBS-CULINARY-BLOG-V2.1`
> **Môn học:** Phát triển Ứng dụng Web Nâng cao (PTUDWNC) – Nhóm 4
> **Căn cứ yêu cầu:** Bám sát 100% tài liệu đặc tả gốc **SRS v1.0.0 (IEEE 830)** gồm đúng **27 Yêu cầu Chức năng (FR)** cốt lõi.
> **Vị trí lưu trữ:** `docs/tasks_breakdown.md`

---

## 1. CẤU TRÚC 4 THƯ MỤC CỐT LÕI 

Hệ thống đã dọn dẹp sạch sẽ và tạo sẵn 4 thư mục tương ứng với 4 project trong Solution Clean Architecture:

```text
d:\Nhom4_WebNangCao/src/
├── CulinaryBlog.Domain/                           # [TẦNG 1: LÕI TRUNG TÂM - DOMAIN]
│   ├── Common/                                    # BaseEntity.cs, SlugHelper.cs
│   ├── Entities/                                  # ApplicationUser, RefreshToken, Category, Recipe,
│   │                                              # RecipeStep, RecipeIngredient, RecipeImage, RecipeNutrition
│   ├── Enums/                                     # RecipeDifficulty.cs, RecipeStatus.cs
│   ├── Exceptions/                                # DomainException.cs, NotFoundException.cs, UnauthorizedException.cs
│   └── Settings/                                  # JwtSettings.cs
│
├── CulinaryBlog.Application/                      # [TẦNG 2: ĐIỀU PHỐI USE CASES - CQRS]
│   ├── Common/Models/                             # PaginatedResult.cs
│   ├── Contracts/                                 # Interfaces: IRepository, IRecipeRepository, IUnitOfWork,
│   │                                              # IJwtService, ISupabaseStorageService, IEmailService
│   ├── DTOs/                                      # CategoryDto, AuthDtos, RecipeDtos
│   ├── Features/                                  # Slices tính năng theo thành viên:
│   │   ├── Categories/                            # [TV1 - Leader]: Commands, Queries, Validators, Handlers
│   │   ├── Auth/                                  # [TV2]: Register, Login, Refresh, Google, Profile
│   │   └── Recipes/                               # [TV3 & TV4]: CreateDraft, Update, Publish, Search FTS
│   └── DependencyInjection.cs                     # Đăng ký MediatR Pipelines & Mapster
│
├── CulinaryBlog.Infrastructure/                   # [TẦNG 3: HẠ TẦNG & CSDL SUPABASE]
│   ├── Persistence/                               # ApplicationDbContext.cs, UnitOfWork.cs
│   │   ├── Configurations/                        # Fluent API Configurations (GIN index, Concurrency, Cascade)
│   │   ├── Repositories/                          # Repository.cs, RecipeRepository.cs (FTS SplitQuery)
│   │   └── Seeders/                               # CulinaryBlogSeeder.cs (Bogus Data)
│   ├── Services/                                  # JwtService.cs, SupabaseStorageService.cs, MailKitEmailService.cs
│   ├── Authorization/                             # RecipeAuthorizationHandler.cs (Resource-Based AuthZ)
│   └── DependencyInjection.cs                     # Đăng ký Npgsql DbContext, Identity, Hangfire, Storage
│
├── CulinaryBlog.API/                              # [TẦNG 4: PRESENTATION - MINIMAL APIS]
│   ├── Endpoints/                                 # CategoryEndpoints.cs, AuthEndpoints.cs, RecipeEndpoints.cs
│   ├── Middlewares/                               # CorrelationIdMiddleware.cs, GlobalExceptionMiddleware.cs
│   ├── Program.cs                                 # Khởi tạo App, Middleware Pipeline, Scalar UI (/scalar/v1)
│   ├── appsettings.json                           # Cấu hình chung hệ thống
│   └── appsettings.Development.json               # Cấu hình Logging môi trường Dev (không chứa secrets)
│
├── .env.example                                   # Mẫu biến môi trường (commit lên Git)
└── .env                                           # Biến môi trường & mật khẩu thật (được .gitignore bảo vệ)
```

### 1.1. Danh sách Bảng CSDL Thực tế trên Supabase Cloud (Khởi tạo bởi EF Core Migration)

Toàn bộ CSDL trên Supabase Cloud PostgreSQL bao gồm 14 bảng chuẩn mực:

```text
Danh sách bảng trên Supabase:
├── 📁 BẢNG NGHIỆP VỤ (Khớp 100% với ERD đặc tả)
│   ├── Categories            --> Bảng Danh mục ẩm thực (CATEGORY trong ERD)
│   ├── Recipes               --> Bảng Công thức (RECIPE trong ERD, đã nhúng luôn Dinh dưỡng)
│   ├── RecipeSteps           --> Bảng Các bước nấu (RECIPE_STEP trong ERD)
│   ├── RecipeIngredients     --> Bảng Nguyên liệu (RECIPE_INGREDIENT trong ERD)
│   ├── RecipeImages          --> Bảng Hình ảnh món ăn (RECIPE_IMAGE trong ERD)
│   ├── RefreshTokens         --> Bảng Token bảo mật (REFRESH_TOKEN trong ERD)
│   └── AspNetUsers           --> Bảng Tài khoản người dùng (APPLICATION_USER trong ERD)
│
├── 📁 HỆ THỐNG BẢNG BẢO MẬT CHUẨN CỦA ASP.NET CORE IDENTITY
│   ├── AspNetRoles           --> Lưu danh sách vai trò (Admin, Author, User)
│   ├── AspNetUserRoles       --> Bảng liên kết N-N: Người dùng nào giữ vai trò nào
│   ├── AspNetRoleClaims      --> Lưu quyền hạn chi tiết (Permissions/Claims) theo vai trò
│   ├── AspNetUserClaims      --> Lưu quyền hạn riêng biệt của từng người dùng
│   ├── AspNetUserLogins      --> Lưu thông tin khi đăng nhập qua Google, Facebook OAuth
│   └── AspNetUserTokens      --> Quản lý mã OTP, Token đổi mật khẩu, xác nhận email
│
└── 📁 BẢNG QUẢN TRỊ NỘI BỘ CỦA ENTITY FRAMEWORK
    └── __EFMigrationsHistory --> Ghi nhớ bản migration nào đã chạy (giúp không chạy trùng lặp)
```

---

## 2. MA TRẬN PHÂN ĐỊNH TRÁCH NHIỆM DATABASE & ĐẢM NHẬN 3 VAI TRÒ (FE - BE - DB)

Mỗi thành viên trong nhóm đều trực tiếp đảm nhận đầy đủ 3 vai trò: **Frontend (UI/Hooks)**, **Backend (CQRS/Services/APIs)** và **Database (Entity/Configuration/Seeder)** theo đúng phân rã nghiệp vụ:

| Thành viên | 🎨 Vai trò FRONTEND | ⚙️ Vai trò BACKEND | 🗄️ Vai trò DATABASE (Entity, Config, Seeder) | Bảng CSDL phụ trách |
| :--- | :--- | :--- | :--- | :--- |
| **1. Nguyễn Thị Trà My** (Trưởng nhóm - 2312693) — Module: **Category & Observability** | Giao diện danh mục (`/categories`); Trang chi tiết danh mục; Trang Admin quản trị Category | Category CRUD (CQRS, MediatR); In-Memory Cache `categories:all`; HealthChecks, Serilog, Tracing | Entity: `BaseEntity.cs`, `Category.cs`; Config: `CategoryConfiguration.cs`; DbContext: `ApplicationDbContext.cs` (Khung chung); Seeder: `CategorySeeder.cs` ($\ge 20$ danh mục ẩm thực) | `Categories`, `SlugRedirects` |
| **2. Hoàng Trịnh Việt Linh** (Thành viên - 2312664) — Module: **Auth & Background Job** | Trang Đăng ký (`/register`); Trang Đăng nhập (`/login`); Trang Profile cá nhân; Nút Google OAuth PKCE | Đăng ký, Đăng nhập Local/Google; Token Rotation, Grace Period 30s; JwtService (HS256, CSPRNG); WelcomeEmailJob (Hangfire) | Entity: `ApplicationUser.cs`, `RefreshToken.cs`; Config: Identity Core, Index `RefreshToken`; Seeder: `UserSeeder.cs` (tài khoản Admin & Author mẫu làm tác giả bài viết) | `AspNetUsers`, `RefreshTokens` |
| **3. Phan Khánh Vương** (Thành viên - 2312802) — Module: **Recipe Core & Media Storage** | Trang soạn thảo nháp Recipe; Trang chỉnh sửa công thức; Giao diện thêm Steps, Ingredients; Component upload ảnh | Recipe CRUD, Concurrency (OCC); Tự động renumbering Steps; Supabase Storage Upload/Delete; ImageResizeJob (Hangfire) | Entity: `Recipe.cs`, `RecipeStep.cs`, `RecipeIngredient.cs`, `RecipeImage.cs`, `RecipeNutrition.cs`; Config: `RecipeConfiguration.cs` (Cascade Delete, OCC); Seeder: `RecipeSeeder.cs` ($\ge 100$ Recipes, $\ge 10$ nguyên liệu, $\ge 5$ bước) | `Recipes`, `RecipeSteps`, `RecipeIngredients`, `RecipeImages` |
| **4. Lê Phạm Mi Đoan** (Thành viên - 2312597) — Module: **FTS Search, Publish & Sitemap** | Trang chủ hiển thị danh sách Recipe; Thanh tìm kiếm tiếng Việt không dấu; Bộ lọc đa tiêu chí (Faceted Search); Phân trang giao diện | Repository FTS (unaccent, plainto_tsquery); Logic Publish ($\ge 1$ step, $\ge 1$ ingr); Projection bỏ qua Nutrition khi list; SitemapGenerationJob | PostgreSQL Extensions: Kích hoạt `unaccent`, `pg_trgm`; Config: Cột `SearchVector` (tsvector) & GIN Index trong `RecipeConfiguration.cs`; Data Testing: Kiểm thử dữ liệu phân trang & FTS trên 100 công thức | Đồng sở hữu `Recipes` (Tối ưu Index & FTS) |

> ⚠️ **Quy tắc phối hợp CSDL & Git:**
> 1. Mỗi thành viên chỉ code Entity/Configuration trên nhánh Git của mình, tuyệt đối **không tự chạy lệnh CLI migration cá nhân lên Supabase Cloud** để tránh xung đột lịch sử `__EFMigrationsHistory`.
> 2. Thứ tự nạp dữ liệu (Seeder): **TV2 (UserSeeder)** $\rightarrow$ **TV1 (CategorySeeder)** $\rightarrow$ **TV3 (RecipeSeeder)** $\rightarrow$ **TV4 (FTS Data Validation)**.
> 3. Khi toàn bộ code của các bạn đã merge vào nhánh `main`, **Trưởng nhóm (TV1)** đại diện chạy lệnh EF Core Migration và điều phối Seeder tập trung.

---

## 3. BẢNG PHÂN RÃ TỪNG BƯỚC CẦN LÀM CHO 4 THÀNH VIÊN (ACTION STEPS ONLY)

---

### 👤 THÀNH VIÊN 1 (TRƯỞNG NHÓM): NGUYỄN THỊ TRÀ MY — MSSV: 2312693
**Module đảm nhiệm:** Module Quản lý Danh mục (FR-CAT) & Module Giám sát & Quan sát Hệ thống (FR-OBS)
**Phạm vi chức năng:** `FR-CAT-001` đến `FR-CAT-005` và `FR-OBS-001`, `FR-OBS-002`, `FR-OBS-003` (**Tổng cộng 8 chức năng**)
**Mức độ phức tạp:** **Trung bình đến Khá** (CRUD Danh mục, Thuật toán Slug, Health Checks, Structured Logging và Distributed Tracing)

#### Bước 1: Khởi tạo Solution, Cấu hình Kết nối Supabase & Hạ Tầng CSDL (Đảm nhận phần Database của TV1)
- [x] Tạo file Solution `CulinaryBlog.slnx` liên kết 4 project: `CulinaryBlog.Domain`, `CulinaryBlog.Application`, `CulinaryBlog.Infrastructure`, `CulinaryBlog.API`.
- [x] Cấu hình bảo mật biến môi trường Supabase Cloud PostgreSQL trong `.env.example` và `.env` (`SUPABASE_URL`, `SUPABASE_KEY`, `SUPABASE_DB_PASSWORD`).
- [x] Tạo lớp `BaseEntity.cs` trong `CulinaryBlog.Domain/Common/` (`Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `RowVersion`).
- [x] Tạo hàm tiện ích `GenerateSlug(string text)` trong `CulinaryBlog.Domain/Common/SlugHelper.cs` (chuyển đổi tiếng Việt có dấu thành chuỗi URL không dấu).
- [x] Tạo lớp `ApplicationDbContext.cs` trong `CulinaryBlog.Infrastructure/Persistence/` (Kế thừa `IdentityDbContext<ApplicationUser>`, quản lý các DbSet: `Categories`, `Recipes`, `RecipeSteps`, `RecipeIngredients`, `RecipeImages`, `RefreshTokens`, kích hoạt `unaccent`/`pg_trgm`).
- [x] Tạo cấu hình Fluent API `CategoryConfiguration.cs` trong `CulinaryBlog.Infrastructure/Persistence/Configurations/`.
- [x] Viết `CategorySeeder.cs` trong `CulinaryBlog.Infrastructure/Persistence/Seeders/`: Sinh danh sách dữ liệu mẫu $\ge 20$ danh mục ẩm thực thực tế (Món Việt, Món Á, Món Âu, Món Chay, Đồ Uống...).
- [x] Tích hợp và điều phối chạy EF Core Migration (`InitialCreate`) và bộ Seeder tổng hợp trong `Program.cs`.

#### Bước 2: Hiện thực FR-CAT-001 & FR-CAT-002 (Xem Danh sách & Chi tiết Danh mục)
- [x] **Domain Entity:** Tạo thực thể `Category.cs` trong `CulinaryBlog.Domain/Entities/` (Đã hoàn thành từ Bước 1 phục vụ DbContext & Seeder).
- [x] **DTOs:** Tạo `CategoryDto.cs`, `CategoryRecipeSummaryDto.cs` và `CategoryDetailDto.cs` trong `CulinaryBlog.Application/DTOs/`.
- [x] **CQRS Queries & Handlers:**
  - Tạo `GetCategoriesQuery` (`record GetCategoriesQuery() : IRequest<List<CategoryDto>>;`).
  - Tạo `GetCategoriesQueryHandler`: Triển khai cache in-memory (`IMemoryCache` TTL 60 phút) với key `"categories:all"`, sử dụng Mapster `.ProjectToType<CategoryDto>()`.
  - Tạo `GetCategoryBySlugQuery` (`record GetCategoryBySlugQuery(string Slug) : IRequest<CategoryDetailDto?>;`).
  - Tạo `GetCategoryBySlugQueryHandler`: Tìm kiếm danh mục theo Slug, nạp danh sách tóm tắt các công thức liên quan bằng `.AsNoTracking()` (chỉ lấy Title, Slug, PrimaryImage, CookTime, Difficulty; lưu ý: chức năng xem toàn bộ chi tiết công thức nấu ăn chuyên sâu FR-RCP-002 do TV4 phụ trách).
- [x] **API Endpoints:** Đăng ký các endpoints trong `CulinaryBlog.API/Endpoints/CategoryEndpoints.cs`:
  - `app.MapGet("/api/v1/categories", ...)` với OpenAPI metadata.
  - `app.MapGet("/api/v1/categories/{slug}", ...)`.
- [ ] **Frontend Hooks & UI:**
  - Tạo hook `useCategories()` trong `src/frontend/hooks/useRecipes.ts` sử dụng TanStack Query v5.
  - Tạo trang hiển thị danh mục `src/frontend/app/categories/page.tsx` và `src/frontend/app/categories/[slug]/page.tsx`.

#### Bước 3: Hiện thực FR-CAT-003 (Tạo Danh mục Mới [Admin])
- [ ] **Request Record:** Tạo `public record CreateCategoryRequest(string Name, string? Description, string? ImageUrl, int OrderIndex);`
- [ ] **CQRS Command & Handler:**
  - Tạo `CreateCategoryCommand(CreateCategoryRequest Request) : IRequest<Guid>;`
  - Tạo `CreateCategoryValidator`: Kiểm tra `Name` không được rỗng, độ dài từ 2 đến 100 ký tự.
  - Tạo `CreateCategoryCommandHandler`: Gọi `SlugHelper.GenerateSlug(request.Name)`, kiểm tra nếu trùng thì thêm hậu tố số tăng dần (`-2`, `-3`), lưu vào Supabase DB qua `IUnitOfWork`, đồng thời gọi `_cache.Remove("categories:all")`.
- [ ] **API Endpoint:** Cài đặt endpoint `POST /api/v1/categories` trong `CategoryEndpoints.cs` yêu cầu `[Authorize(Roles = "Admin")]`.

#### Bước 4: Hiện thực FR-CAT-004 (Cập nhật Danh mục [Admin])
- [ ] **Request Record:** Tạo `public record UpdateCategoryRequest(string Name, string? Description, string? ImageUrl, int OrderIndex);`
- [ ] **CQRS Command & Handler:**
  - Tạo `UpdateCategoryCommand(Guid Id, UpdateCategoryRequest Request) : IRequest;`
  - Tạo `UpdateCategoryValidator`: Kiểm tra `Id` và `Name`.
  - Tạo `UpdateCategoryCommandHandler`: Tìm category theo ID; nếu không thấy ném `NotFoundException`; cập nhật `Name`, `Description`, `ImageUrl`, `OrderIndex` (giữ nguyên `Slug` ban đầu để bảo vệ liên kết SEO); xóa cache `"categories:all"`.
- [ ] **API Endpoint:** Cài đặt endpoint `PUT /api/v1/categories/{id}` trong `CategoryEndpoints.cs`.

#### Bước 5: Hiện thực FR-CAT-005 (Xóa Danh mục [Admin])
- [ ] **CQRS Command & Handler:**
  - Tạo `DeleteCategoryCommand(Guid Id) : IRequest;`
  - Tạo `DeleteCategoryCommandHandler`: Kiểm tra trong Supabase DB nếu còn công thức đang trực thuộc danh mục (`_context.Recipes.Any(r => r.CategoryId == id && !r.IsDeleted)`); nếu còn -> ném ngoại lệ trả về `HTTP 409 Conflict`; nếu 0 công thức -> tiến hành xóa danh mục và xóa cache `"categories:all"`.
- [ ] **API Endpoint:** Cài đặt endpoint `DELETE /api/v1/categories/{id}` trong `CategoryEndpoints.cs`.
- [ ] **Giao diện Quản trị Frontend:** Tạo trang quản trị danh mục `src/frontend/app/admin/categories/page.tsx` cho phép Admin thêm, sửa, xóa danh mục trực quan.

#### Bước 6: Hiện thực FR-OBS-001 (Health Check Endpoints)
- [ ] **Cấu hình Health Checks:** Cài đặt gói `AspNetCore.HealthChecks.Npgsql` trong `CulinaryBlog.API.csproj`.
- [ ] **Đăng ký Endpoints trong `Program.cs`:**
  - `GET /health/live`: Liveness probe kiểm tra process API.
  - `GET /health/ready`: Readiness probe kiểm tra kết nối Supabase PostgreSQL.
  - `GET /health`: Báo cáo chi tiết dạng JSON về tài nguyên hệ thống.

#### Bước 7: Hiện thực FR-OBS-002 (Structured Logging với Serilog & CorrelationId)
- [ ] **Cài đặt Middleware Truy Vết:** Tạo `CorrelationIdMiddleware.cs` trong `CulinaryBlog.API/Middlewares/`: Đọc mã `X-Correlation-ID` từ HTTP Header gửi lên hoặc tự sinh GUID mới, gắn vào Response Header và đẩy vào `LogContext.PushProperty("CorrelationId", correlationId)`.
- [ ] **Cảnh Báo Request Chạy Chậm:** Viết MediatR Pipeline Behavior `LoggingBehavior.cs` đo thời gian bằng `Stopwatch`: Nếu thời gian xử lý request vượt quá **500ms**, tự động ghi log cảnh báo mức `Log.Warning("Long Running Request: {RequestName} ({ElapsedMs} milliseconds) with CorrelationId: {CorrelationId}", ...)`.

#### Bước 8: Hiện thực FR-OBS-003 (Distributed Tracing & Cấu hình Scalar UI)
- [ ] **Cấu hình OpenTelemetry Tracing:** Đăng ký `.AddOpenTelemetry().WithTracing(builder => builder.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddNpgsql())` trong `Program.cs` để giám sát vết phân tán từ request qua EF Core xuống CSDL Supabase.
- [x] **Cấu hình Scalar UI Native:** Đăng ký `app.MapOpenApi()` và `app.MapScalarApiReference(options => options.WithTheme(ScalarTheme.Purple))` trong `Program.cs` để cung cấp giao diện tài liệu OpenAPI trực quan tại `http://localhost:5000/scalar/v1`.

---

### 👤 THÀNH VIÊN 2: Hoàng Trịnh Việt Linh — MSSV: 2312664
**Module đảm nhiệm:** Module Xác thực, Quản lý Người dùng (FR-AUTH) & Background Email (FR-JOB)
**Phạm vi chức năng:** `FR-AUTH-001` đến `FR-AUTH-007` và `FR-JOB-001` (**Tổng cộng 8 chức năng**)
**Mức độ** (Xử lý an ninh mật mã, Token Rotation, Grace Period, Reuse Detection, OAuth 2.0 PKCE, Hangfire Mailer)

#### Bước 1: Cấu hình Identity Core, JWT Token Service & Database Auth (Đảm nhận phần Database của TV2)
- [x] **Domain Entities:**
  - Tạo `ApplicationUser.cs` trong `CulinaryBlog.Domain/Entities/` (kế thừa `IdentityUser` bổ sung `DisplayName`, `AvatarUrl`, `Bio`, `IsActive`, `CreatedAt`).
  - Tạo `RefreshToken.cs` (`Id`, `UserId`, `TokenHash`, `ExpiresAt`, `RevokedAt`, `ReplacedByTokenHash`, `CreatedByIp`).
- [x] **JWT Service:** Tạo interface `IJwtService` và cài đặt `JwtService.cs` trong Infrastructure:
  - Phương thức `GenerateAccessToken(ApplicationUser user, IList<string> roles)` (HS256, hạn 15 phút).
  - Phương thức `GenerateRefreshToken()` (chuỗi ngẫu nhiên 64 bytes).
  - Phương thức `HashToken(string token)` (mã hóa băm SHA-256 lưu CSDL).
  - Phương thức `GetPrincipalFromExpiredToken(string token)` (giải mã token đã hết hạn).
- [x] **Database Seeder:** Viết `UserSeeder.cs` trong `CulinaryBlog.Infrastructure/Persistence/Seeders/`: Khởi tạo sẵn tài khoản Admin (`admin@culinary.local`) và Author mẫu (`chef_admin@culinary.local`) có `PasswordHash` chuẩn ASP.NET Core Identity để TV3 lấy `AuthorId` làm tác giả sở hữu công thức.

#### Bước 2: Hiện thực FR-AUTH-001 & FR-AUTH-002 (Đăng ký & Đăng nhập Local)
- [x] **Request Records:**
  - `public record RegisterRequest(string Email, string Password, string DisplayName, string UserName);`
  - `public record LoginRequest(string Email, string Password);`
- [x] **CQRS Commands, Validators & Handlers:**
  - Tạo `RegisterCommand` & `RegisterValidator` (Email regex chuẩn, mật khẩu tối thiểu 8 ký tự đủ hoa, thường, số, ký tự đặc biệt).
  - Tạo `RegisterCommandHandler`: Gọi `UserManager.CreateAsync()` (mật khẩu băm PBKDF2), gán role mặc định `"Author"`, sinh cặp token, lưu RefreshToken băm SHA-256 vào Supabase DB, trả về `AuthResponseDto` và tự động enqueue `WelcomeEmailJob`.
  - Tạo `LoginCommand` & `LoginValidator`.
  - Tạo `LoginCommandHandler`: Gọi `UserManager.FindByEmailAsync()`, kiểm tra mật khẩu. Cài đặt cơ chế **Account Lockout**: nếu nhập sai 5 lần liên tiếp (`AccessFailedCount >= 5`), tạm khóa tài khoản trong 15 phút và trả về `HTTP 423 Locked`.
- [x] **API Endpoints trong `AuthEndpoints.cs`:**
  - `POST /api/v1/auth/register`
  - `POST /api/v1/auth/login`
- [ ] **Frontend UI:** Xây dựng trang `src/frontend/app/register/page.tsx` và `src/frontend/app/login/page.tsx` với React Hook Form và Zod.

#### Bước 3: Hiện thực FR-AUTH-003 (Đăng nhập Google OAuth 2.0 PKCE)
- [ ] **Request Record:** `public record GoogleLoginRequest(string IdToken);`
- [ ] **CQRS Command & Handler:**
  - Tạo `GoogleLoginCommand(GoogleLoginRequest Request) : IRequest<AuthResponseDto>;`
  - Tạo `GoogleLoginCommandHandler`: Sử dụng `GoogleJsonWebSignature.ValidateAsync(idToken)` xác thực với máy chủ Google. Triển khai logic **Account Linking**: nếu email đã tồn tại thì liên kết tài khoản Google; nếu chưa thì tạo user mới với `EmailConfirmed = true`.
- [ ] **API Endpoint:** `POST /api/v1/auth/google` trong `AuthEndpoints.cs`.
- [ ] **Frontend Integration:** Tích hợp nút "Đăng nhập với Google" trong Next.js sử dụng Auth.js v5 (`signIn('google')`).

#### Bước 4: Hiện thực FR-AUTH-004 & FR-AUTH-005 (Token Rotation & Đăng xuất)
- [ ] **Request Record:** `public record RefreshTokenRequest(string RefreshToken);`
- [ ] **CQRS Refresh Command & Handler:**
  - Tạo `RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponseDto>;`
  - Tạo `RefreshTokenCommandHandler`: Băm token gửi lên và so khớp với bảng `RefreshTokens` trong Supabase DB.
  - **Token Rotation:** Thu hồi token cũ (`RevokedAt = UtcNow`), phát sinh token mới và lưu vết `ReplacedByTokenHash`.
  - **Phát hiện tái sử dụng (Reuse Detection):** Nếu token đã bị thu hồi trước đó được gửi lại ngoài thời gian ân hạn 30 giây -> kích hoạt thu hồi toàn bộ token của user (`RevokeTokenFamily`) và ném lỗi `AUTH_REFRESH_TOKEN_REVOKED` (HTTP 401).
- [ ] **CQRS Logout Command & Handler:**
  - Tạo `LogoutCommand(string RefreshToken) : IRequest;`
  - Đánh dấu thu hồi RefreshToken trong CSDL Supabase.
- [ ] **API Endpoints:**
  - `POST /api/v1/auth/refresh`
  - `POST /api/v1/auth/logout`
- [ ] **Frontend Interceptor:** Cài đặt cơ chế chặn request trùng lặp (Mutex Lock) trong `src/frontend/lib/api/axios.ts`.

#### Bước 5: Hiện thực FR-AUTH-006 & FR-AUTH-007 (Xem & Cập nhật Hồ sơ Cá nhân)
- [ ] **Request Record:** `public record UpdateProfileRequest(string? DisplayName, string? AvatarUrl, string? Bio);`
- [ ] **CQRS Commands, Queries & Handlers:**
  - Tạo `GetCurrentUserProfileQuery() : IRequest<UserDto>;` và handler trả về thông tin người dùng hiện tại (loại bỏ hoàn toàn các trường nhạy cảm `PasswordHash`, `SecurityStamp`).
  - Tạo `UpdateProfileCommand(UpdateProfileRequest Request) : IRequest<UserDto>;` và handler thực hiện PATCH các trường `DisplayName`, `AvatarUrl`, `Bio` (cấm tuyệt đối việc thay đổi `Email` hoặc `UserName` tại đây).
- [ ] **API Endpoints:**
  - `GET /api/v1/auth/me`
  - `PATCH /api/v1/auth/profile`
- [ ] **Frontend UI:** Xây dựng trang hồ sơ cá nhân `src/frontend/app/profile/page.tsx`.

#### Bước 6: Hiện thực FR-JOB-001 (Welcome Email Job)
- [ ] **Cài đặt Email Service:** Tạo interface `IEmailService` và cài đặt `MailKitEmailService.cs` trong `Infrastructure/Services/` gửi mail qua SMTP.
- [ ] **Tạo Job:** Tạo `WelcomeEmailJob.cs` trong `Infrastructure/Jobs/` triển khai phương thức `SendWelcomeEmailAsync(string userId)` với template HTML chào mừng.
- [ ] **Kích hoạt Job:** Enqueue tác vụ ngầm `BackgroundJob.Enqueue<WelcomeEmailJob>(x => x.SendWelcomeEmailAsync(user.Id))` ngay sau khi đăng ký thành công.

---

### 👤 THÀNH VIÊN 3: Phan Khánh Vương — MSSV: 2312802
**Module đảm nhiệm:** Module Quản lý Công thức Lõi (FR-RCP), Tệp tin (FR-FILE) & Resize Ảnh (FR-JOB)
**Phạm vi chức năng:** `FR-RCP-003`, `FR-RCP-004`, `FR-RCP-007`, `FR-RCP-008`, `FR-RCP-009`, `FR-RCP-010`, `FR-FILE-001`, `FR-FILE-002`, `FR-JOB-002` (**Tổng cộng 9 chức năng**)
**Mức độ** (Aggregate Root phức hợp, Concurrency Token, Tự động renumber bước nấu, Tích hợp Supabase Storage, Cascade Delete)

#### Bước 1: Thiết kế Domain Entity Recipe, Aggregate Root & Database Recipe (Đảm nhận phần Database của TV3)
- [x] **Domain Entities:**
  - Tạo Aggregate Root `Recipe.cs` trong `CulinaryBlog.Domain/Entities/` (`Title`, `Slug`, `Description`, `Instructions`, `PrepTimeMinutes`, `CookTimeMinutes`, `Servings`, `Difficulty`, `Status`, `CategoryId`, `AuthorId`, `RowVersion`).
  - Tạo Owned Entity `RecipeNutrition.cs` (`Calories`, `Protein`, `Carbohydrates`, `Fat`, `Fiber`, `Sodium`).
  - Tạo Entity `RecipeStep.cs` (`StepNumber`, `Title`, `Description`, `TimerMinutes`, `ImageUrl`).
  - Tạo Entity `RecipeIngredient.cs` (`Name`, `Quantity`, `Unit`, `Notes`, `OrderIndex`).
  - Tạo Entity `RecipeImage.cs` (`OriginalUrl`, `MediumUrl`, `ThumbnailUrl`, `IsPrimary`, `OrderIndex`).
- [x] **EF Core Configurations:** Tạo `RecipeConfiguration.cs` trong `Infrastructure/Persistence/Configurations/` cấu hình:
  - Khóa ngoại `CategoryId` là **Nullable** (`builder.HasOne(r => r.Category).WithMany().HasForeignKey(r => r.CategoryId).OnDelete(DeleteBehavior.SetNull);`).
  - Cấu hình Concurrency Token: `builder.Property(r => r.RowVersion).IsRowVersion();`.
  - Cấu hình Cascade Delete cho Steps, Ingredients, Images.
- [x] **Database Seeder:** Viết `RecipeSeeder.cs` trong `CulinaryBlog.Infrastructure/Persistence/Seeders/` sử dụng thư viện `Bogus`: Sinh tự động $\ge 100$ Recipes ngẫu nhiên (lấy `CategoryId` từ TV1 và `AuthorId` từ TV2; mỗi Recipe tự động sinh từ 10–14 `RecipeIngredient` và từ 5–8 `RecipeStep` có `StepNumber = 1, 2, 3...` tăng dần, nhúng đầy đủ `RecipeNutrition`).

#### Bước 2: Hiện thực FR-RCP-003 & FR-RCP-004 (Tạo Bản Nháp & Cập nhật Công thức)
- [x] **Request Records:**
  - `public record CreateRecipeDraftRequest(string Title, string? Description, Guid? CategoryId);`
  - `public record UpdateRecipeRequest(string Title, string Description, string? Instructions, Guid? CategoryId, int PrepTimeMinutes, int CookTimeMinutes, int Servings, RecipeDifficulty Difficulty, byte[] RowVersion);`
- [x] **CQRS Commands, Validators & Handlers:**
  - Tạo `CreateRecipeDraftCommand` & `CreateRecipeDraftValidator` (Chỉ kiểm tra `Title` từ 5 đến 200 ký tự, KHÔNG bắt buộc steps/ingredients khi lưu nháp).
  - Tạo `CreateRecipeDraftCommandHandler`: Khởi tạo công thức ở trạng thái `RecipeStatus.Draft`, gán `AuthorId = currentUserId`, sinh slug tự động, commit vào Supabase DB qua `IUnitOfWork`.
  - Tạo `UpdateRecipeCommand` & `UpdateRecipeCommandHandler`: Kiểm tra quyền sở hữu bằng `RecipeAuthorizationHandler` (chỉ tác giả hoặc Admin). Kiểm tra xung đột đồng thời **Optimistic Concurrency Control**: so khớp `RowVersion`, nếu khác nhau ném `RecipeConcurrencyConflictException` (HTTP 409).
- [x] **API Endpoints trong `RecipeEndpoints.cs`:**
  - `POST /api/v1/recipes`
  - `PUT /api/v1/recipes/{id}`
- [ ] **Frontend UI:** Xây dựng trang soạn thảo công thức `src/frontend/app/recipes/create/page.tsx` và `src/frontend/app/recipes/[id]/edit/page.tsx`.

#### Bước 3: Hiện thực FR-RCP-010 (Quản lý Bước Nấu & Tự Động Renumbering)
- [ ] **Logic Domain:** Viết phương thức `RemoveStep(Guid stepId)` trong `Recipe.cs` tự động chạy vòng lặp đánh lại số thứ tự `StepNumber = 1, 2, 3...` liên tục khi xóa một bước bất kỳ.
- [ ] **Request Records:**
  - `public record AddRecipeStepRequest(string Title, string Description, int? TimerMinutes, string? ImageUrl);`
  - `public record UpdateRecipeStepRequest(string Title, string Description, int? TimerMinutes, string? ImageUrl);`
- [ ] **CQRS Commands & Handlers:**
  - `AddRecipeStepCommand`: Tự động gán `StepNumber = recipe.Steps.Count + 1`.
  - `UpdateRecipeStepCommand`: Cập nhật nội dung bước làm.
  - `DeleteRecipeStepCommand`: Gọi `recipe.RemoveStep(stepId)` và commit DB.
- [ ] **API Endpoints:**
  - `POST /api/v1/recipes/{recipeId}/steps`
  - `PUT /api/v1/recipes/{recipeId}/steps/{stepId}`
  - `DELETE /api/v1/recipes/{recipeId}/steps/{stepId}`

#### Bước 4: Hiện thực FR-RCP-009 (Quản lý Nguyên liệu Món ăn)
- [ ] **Request Records:**
  - `public record AddIngredientRequest(string Name, decimal? Quantity, string? Unit, string? Notes);`
  - `public record UpdateIngredientRequest(string Name, decimal? Quantity, string? Unit, string? Notes);`
- [ ] **CQRS Commands & Handlers:**
  - Tạo `AddIngredientCommand`, `UpdateIngredientCommand`, `DeleteIngredientCommand` và các handlers tương ứng thao tác qua Repository.
- [ ] **API Endpoints:**
  - `POST /api/v1/recipes/{recipeId}/ingredients`
  - `PUT /api/v1/recipes/{recipeId}/ingredients/{ingredientId}`
  - `DELETE /api/v1/recipes/{recipeId}/ingredients/{ingredientId}`

#### Bước 5: Hiện thực FR-FILE-001 & FR-FILE-002 (Tích hợp Supabase Storage & Thẩm duyệt Magic Bytes)
- [ ] **Cài đặt Supabase Storage Service:** Tạo interface `ISupabaseStorageService` và lớp `SupabaseStorageService.cs` trong `Infrastructure/Services/`:
  - Phương thức `UploadFileAsync(Stream fileStream, string fileName, string contentType)` tải lên bucket `culinary-blog`.
  - Phương thức `DeleteFileAsync(string fileUrl)` xóa tệp trên Supabase Storage.
  - Phương thức `GetPublicUrl(string filePath)` trả về CDN URL công khai.
- [ ] **Thẩm duyệt Magic Bytes:** Viết hàm kiểm tra bytes nhị phân đầu tiên của stream (JPEG: `FF D8 FF`, PNG: `89 50 4E 47`, WebP: `52 49 46 46`).
- [ ] **Chống Path Traversal:** Sinh tên tệp ngẫu nhiên `recipes/{recipeId}/{Guid.NewGuid()}.webp`.

#### Bước 6: Hiện thực FR-RCP-008 (Quản lý Ảnh Công thức)
- [ ] **Upload Ảnh:** Endpoint `POST /api/v1/recipes/{id}/images` nhận tệp, upload lên Supabase Storage, tự động gán `IsPrimary = true` cho ảnh đầu tiên.
- [ ] **Xóa Ảnh:** Khi xóa ảnh đại diện chính, tự động chỉ định ảnh đầu tiên còn lại làm ảnh đại diện chính mới.

#### Bước 7: Hiện thực FR-RCP-007 & FR-JOB-002 (Xóa Công thức & Thumbnail Job)
- [ ] **Xóa Công thức (FR-RCP-007):** Tạo `DeleteRecipeCommand` và handler thực hiện Cascade Delete bản ghi trong Supabase DB, trả về HTTP 204 No Content, đồng thời đẩy danh sách URL ảnh vào Hangfire Job để dọn dẹp trên Supabase Storage.
- [ ] **Tạo Job Resize Ảnh (FR-JOB-002):** Tạo `ImageResizeJob.cs` trong Hangfire tự động nén và sinh 2 kích thước 800×600 và 300×300 đưa lên Supabase Storage sau khi tải ảnh lên.

---

### 👤 THÀNH VIÊN 4: Lê Phạm Mi Đoan — MSSV: 2312597
**Module đảm nhiệm:** Module Xuất bản, Tìm kiếm & Phân trang (FR-SRCH) & SEO Sitemap (FR-JOB)
**Phạm vi chức năng:** `FR-RCP-001`, `FR-RCP-002`, `FR-RCP-005`, `FR-RCP-006`, `FR-SRCH-001` đến `FR-SRCH-004`, `FR-JOB-003` (**Tổng cộng 9 chức năng**)
**Mức độ** (Tối ưu hóa FTS tiếng Việt unaccent, phân trang đa tiêu chí, Projection và Schema.org)

#### Bước 1: Hiện thực FR-SRCH-001 (Cấu hình FTS Tiếng Việt Supabase & Kiểm thử Dữ liệu CSDL - Đảm nhận phần Database của TV4)
- [ ] **Cấu hình CSDL FTS:**
  - [x] Kích hoạt extension `unaccent` và `pg_trgm` trong `ApplicationDbContext.OnModelCreating` (TV1 đã tạo khung sườn).
  - [ ] Cấu hình Generated Column `SearchVector` tự động cập nhật từ `Title` (trọng số A) và `Description` (trọng số B) trong `RecipeConfiguration.cs`.
  - [ ] Đánh chỉ mục **GIN Index** trên cột `SearchVector`.
- [x] **Repository LINQ FTS:** Tạo `IRecipeRepository` và cài đặt `RecipeRepository.cs` sử dụng `EF.Functions.ToTsVector()` kết hợp `EF.Functions.PlainToTsQuery()` và hàm `unaccent()` để hỗ trợ tìm kiếm không dấu tiếng Việt bản địa hóa.
- [x] **Kiểm thử Toàn vẹn Dữ liệu CSDL:** Đã viết kịch bản kiểm thử/nghiệm thu chất lượng dữ liệu FTS không dấu (`unaccent`) và thuật toán phân trang (`PaginatedResult`) trên tập dữ liệu 100 công thức sau khi được Seed vào Supabase. Việc chạy nghiệm thu cần môi trường có kết nối Supabase.

#### Bước 2: Hiện thực FR-SRCH-002, 003, 004 (Lọc Đa Tiêu Chí, Sắp Xếp & Phân Trang)
- [x] **Request Model:** Tạo `GetRecipesQuery` (nhận `SearchTerm`, `CategoryId`, `Difficulty`, `SortBy`, `PageIndex`, `PageSize`).
- [x] **CQRS Query & Handler:**
  - Tạo `GetRecipesQuery` và `GetRecipesQueryValidator` (phân trang an toàn với giới hạn trần `PageSize <= 50`).
  - Tạo `GetRecipesQueryHandler`: Xếp hạng kết quả, áp dụng các bộ lọc Category, Difficulty, sắp xếp `newest`, tự động join tác giả và ảnh đại diện, phân trang `PaginatedResult<RecipeListDto>`.
- [x] **API Endpoint:** `GET /api/v1/recipes` trong `RecipeEndpoints.cs` tích hợp Output Cache 15 phút (`RecipesCache`).

#### Bước 3: Hiện thực FR-RCP-001 & FR-RCP-002 (Xem Danh Sách & Chi Tiết Công Thức)
- [x] **Tối ưu Hóa Truy Vấn Projection (FR-RCP-001):**
  - Trong `RecipeRepository` / `GetRecipesQueryHandler`: Chiết xuất trực tiếp sang `RecipeListDto` chỉ `SELECT` các cột cần thiết, **loại bỏ hoàn toàn việc tải 6 cột `Nutrition_*`**, các bảng Steps và Ingredients khi lấy danh sách.
- [ ] **Truy Vấn Chi Tiết Eager Loading (FR-RCP-002):**
  - Trong `GetRecipeDetailQueryHandler`: Nạp đầy đủ Steps, Ingredients, Images và Nutrition bằng kỹ thuật `.AsSplitQuery()`.
  - Cache chi tiết bài viết qua Output Cache với TTL 60 phút.
- [ ] **SEO Schema.org Recipe (JSON-LD):** Phía Next.js (`src/frontend/app/recipes/[slug]/page.tsx`), tự động nhúng thẻ script `@type: "Recipe"` đạt chứng nhận Google Rich Snippets.

#### Bước 4: Hiện thực FR-RCP-005 & FR-RCP-006 (Xuất Bản & Lưu Trữ Công Thức)
- [ ] **Quy Tắc Xuất Bản Nghiêm Ngặt (FR-RCP-005):**
  - Tạo `PublishRecipeCommand(Guid Id) : IRequest;`
  - Tạo `PublishRecipeCommandHandler`: Kiểm tra điều kiện nghiệp vụ:
    ```csharp
    recipe.Steps.Count >= 1 && recipe.Ingredients.Count >= 1
    ```
    Nếu không thỏa mãn, ném `DomainException` trả về `HTTP 422 Unprocessable Entity` (`RECIPE_PUBLISH_INCOMPLETE`). Nếu thỏa mãn, đổi trạng thái sang `RecipeStatus.Published` và gán `PublishedAt = UtcNow`.
- [ ] **Lưu Trữ Công Thức (FR-RCP-006):**
  - Tạo `ArchiveRecipeCommand(Guid Id) : IRequest;` và handler đổi trạng thái sang `RecipeStatus.Archived` để ẩn khỏi trang chủ.
- [ ] **API Endpoints:**
  - `POST /api/v1/recipes/{id}/publish`
  - `POST /api/v1/recipes/{id}/unpublish`
  - `POST /api/v1/recipes/{id}/archive`

#### Bước 5: Hiện thực FR-JOB-003 (Sitemap Generation Job)
- [ ] **Tạo Job:** Tạo `SitemapGenerationJob.cs` trong `Infrastructure/Jobs/`.
- [ ] **Phương thức `ExecuteAsync()`:** Quét toàn bộ công thức `Published` và danh mục trong Supabase DB, tạo file `sitemap.xml` chuẩn SEO, lưu vào webroot và phát tín hiệu ping đến Google Search Console.
- [ ] **Đăng ký Định kỳ:** Đăng ký lịch chạy trong `Program.cs` chạy vào lúc **02:00 AM UTC** hàng ngày (`RecurringJob.AddOrUpdate<SitemapGenerationJob>("sitemap-job", job => job.ExecuteAsync(), "0 2 * * *")`).

---

## 📊 BẢNG TỔNG HỢP PHÂN BỔ CÔNG VIỆC CÂN BẰNG TOÀN NHÓM

```text
+-------------------------+----------------------+--------------------+--------------------+
| Thành viên              | Module Đảm nhiệm     | Số chức năng SRS   | Mức độ Đánh giá    |
+-------------------------+----------------------+--------------------+--------------------+
| 1. Nguyễn Thị Trà My    | FR-CAT (001 -> 005)  | 8 chức năng        | Trung bình đến Khá |
| (Trưởng nhóm - 2312693) | FR-OBS (001,002,003) |                    | (Kèm điều phối Git)|
|                         |                      |                    |                    |
| 2. Hoàng Trịnh Việt Linh| FR-AUTH (001 -> 007) | 8 chức năng        | Khá                |
| (Thành viên - 2312664)  | FR-JOB-001           |                    | (Bảo mật & Mailer) |
|                         |                      |                    |                    |
| 3. Phan Khánh Vương     | FR-RCP (003,004,007-010)| 9 chức năng     | Khá                |
| (Thành viên - 2312802)  | FR-FILE (001,002)    |                    | (Aggregate Root &  |
|                         | FR-JOB-002           |                    |  Supabase Storage) |
|                         |                      |                    |                    |
| 4. Lê Phạm Mi Đoan      | FR-RCP (001,002,005,006)| 9 chức năng     | Khá                |
| (Thành viên - 2312597)  | FR-SRCH (001 -> 004) |                    | (FTS Tiếng Việt,   |
|                         | FR-JOB-003           |                    |  Faceted & Sitemap)|
+-------------------------+----------------------+--------------------+--------------------+
| TỔNG CỘNG               | 7 MODULES CỐT LÕI    | 34 ĐẦU MỤC CHI TIẾT| 100% CÂN BẰNG      |
|                         |                      | (ĐỦ 27 CHỨC NĂNG)  | CHUẨN ĐỒ ÁN        |
+-------------------------+----------------------+--------------------+--------------------+
```

---
*Tài liệu phân rã WBS được lưu trữ chính thức tại:* `d:\Nhom4_WebNangCao\docs\tasks_breakdown.md`
