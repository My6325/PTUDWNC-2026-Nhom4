# SYSTEM CONTEXT & PROJECT RULES: CULINARY BLOG V1

## 1. Project Overview & AI Role
You are an expert full-stack engineer acting as the lead developer for **Culinary Blog** (`CULINARY-BLOG-V1`).
- **Architecture Style**: API-Driven Architecture with complete separation of Backend (.NET 10) and Frontend (Next.js).
- **Baseline Specification**: IEEE 830 / ISO/IEC/IEEE 29148:2018 SRS v1.0.0.

---

## 2. Tech Stack Specification
- **Backend Framework**: .NET 10 Minimal APIs (C#).
- **Frontend Framework**: Next.js (App Router), TypeScript, Tailwind CSS[cite: 4].
- **Database**: PostgreSQL 16 (Extensions: `unaccent`, `pg_trgm` required)[cite: 4].
- **ORM**: Entity Framework Core 10 (Code-First Migrations)[cite: 4].
- **Cache & Session**: Redis 7 (StackExchange.Redis)[cite: 4].
- **Object Storage**: MinIO S3-Compatible Object Storage[cite: 4].
- **Background Jobs**: Hangfire (In-process, PostgreSQL storage)[cite: 4].
- **Authentication**: ASP.NET Core Identity + JWT (HS256) + Google OAuth 2.0[cite: 4].
- **Observability**: Serilog (Structured Logging), OpenTelemetry (Tracing & Metrics)[cite: 4].

---

## 3. Mandatory Design Constraints (Non-Negotiable)
* **CONS-001 (Clean Architecture)**: Backend MUST strictly adhere to 4 layers: `Domain`, `Application`, `Infrastructure`, `Presentation`[cite: 4]. The `Domain` layer must have ZERO external library dependencies[cite: 4].
* **CONS-002 (CQRS Pattern)**: All business operations must be implemented as MediatR `Command` or `Query` Handlers[cite: 4].
* **CONS-003 (Minimal APIs)**: Use .NET 10 Minimal APIs only[cite: 4]. Do NOT use MVC Controllers[cite: 4]. Frontend must use Next.js App Router (no Pages Router)[cite: 4].
* **CONS-004 (Authentication & Security)**: Use stateless JWT (15-min Access Token, 7-day Refresh Token with Rotation & Reuse Detection)[cite: 4]. Passwords must be hashed via PBKDF2[cite: 4].
* **CONS-005 (API & Error Format)**: Follow RESTful conventions under `/api/v1/`[cite: 4]. Errors must strictly conform to RFC 7807 (`application/problem+json`)[cite: 4].
* **CONS-006 (Database Rules)**: PostgreSQL with EF Core Code-First[cite: 4]. Never write raw unparameterized SQL queries[cite: 4].
* **CONS-007 (File Upload Security)**: Max file size 5 MB[cite: 4]. Validate MIME type and Magic Bytes (`image/jpeg`, `image/png`, `image/webp`, `image/avif`)[cite: 4]. Store on MinIO with GUID filenames[cite: 4].
* **CONS-008 (Validation)**: Input validation MUST be handled in Application Layer via `FluentValidation` + MediatR Pipeline Behavior[cite: 4]. Do NOT put validation code inside Minimal API handlers[cite: 4].
* **CONS-010 (Structured Logging)**: Use Serilog[cite: 4]. Every log must capture `CorrelationId`, `RequestPath`, and `UserId` (when authenticated)[cite: 4].

---

## 4. Key Domain Entities & Business Rules
1. **Recipe (Aggregate Root)**:
   - Child Entities: `RecipeStep`, `RecipeIngredient`, `RecipeImage`[cite: 4].
   - Owned Entity: `RecipeNutrition` (embedded directly in `Recipes` table)[cite: 4].
   - Concurrency: Controlled via `RowVersion` timestamp to prevent lost updates[cite: 4].
   - Publishing Gate: A recipe CANNOT be published (`Draft` -> `Published`) unless `Steps.Count > 0`[cite: 4].
2. **Category**:
   - Slug generated automatically from name[cite: 4].
   - Cannot be deleted if it contains recipes (`RecipeCount > 0`)[cite: 4].
3. **Authorization Policies**:
   - `Role-Based`: `Admin`, `Author`, `Guest`[cite: 4].
   - `Resource-Based`: Authors can ONLY edit/delete/publish their own recipes (`AuthorId == currentUserId`)[cite: 4]. Admin bypasses ownership checks[cite: 4].

---

## 5. Directory Structure Guidelines

### Backend (.NET 10)
```text
src/
├── CulinaryBlog.Domain/           # Entities, Value Objects, Domain Events, Enums
├── CulinaryBlog.Application/      # Commands, Queries, DTOs, FluentValidators, Behaviors
├── CulinaryBlog.Infrastructure/   # DbContext, Repositories, MinIO, Redis, Hangfire, Identity
└── CulinaryBlog.Api/     # Minimal API Endpoints, Middlewares, Extensions