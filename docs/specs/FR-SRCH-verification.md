# Nghiem thu tim kiem va phan trang TV4

## Pham vi

FR-RCP-001, FR-SRCH-001 den FR-SRCH-004 va FR-JOB-003. Frontend Lab 6 tai
`frontend/src/app/recipes/page.tsx` tai su dung UI, layout va auth store chung.
Bao cao ca nhan duoc xuat rieng, khong luu trong Git.

## Dieu kien Supabase

Khong tu chay migration tren Supabase dung chung. Theo `docs/tasks_breakdown.md`,
truong nhom dieu phoi migration va seed sau khi review/merge. PR nay khong thay doi
schema cloud, khong seed va khong sua lich su migration.

Truoc khi nghiem thu FTS, nguoi quan tri chay cac truy van chi doc:

```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
SELECT column_name, data_type, is_generated, generation_expression
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'Recipes'
  AND column_name IN ('SearchVector', 'PublishedAt');
SELECT indexname, indexdef FROM pg_indexes
WHERE schemaname = 'public' AND tablename = 'Recipes';
SELECT extname, extnamespace::regnamespace FROM pg_extension
WHERE extname IN ('unaccent', 'pg_trgm');
SELECT to_regprocedure('public.recipe_unaccent(text)');
```

Yeu cau: migration `20261006120000_AddRecipeSearchAndPublication` da duoc ap dung,
`SearchVector` la generated tsvector co trong so A/B, index
`IX_Recipes_SearchVector_GIN`, helper `public.recipe_unaccent(text)` va cot
`PublishedAt`. Migration hien co tham chieu extension `unaccent` trong schema public;
can kiem tra schema extension truoc khi nguoi quan tri ap dung migration.
Neu lich su va schema khong khop, doi chieu truoc; khong chen gia vao lich su.

Khong fallback sang ILIKE khi thieu cot: fallback thay doi ngu nghia tim kiem,
mat ts_rank va bo qua sort. Loi schema phai duoc xu ly bang deployment,
khong tra 200 voi ket qua khac hop dong. API hien tra ProblemDetails 500 chung
va ghi log loi database, khong lo thong tin noi bo cho client.

## Kiem thu

```powershell
dotnet test tests/CulinaryBlog.Infrastructure.IntegrationTests/CulinaryBlog.Infrastructure.IntegrationTests.csproj --filter "FullyQualifiedName~RecipeSearchSqlTests|FullyQualifiedName~SitemapGenerationJobTests|FullyQualifiedName~RecipeMigrationTests|FullyQualifiedName~RecipeQueryPipelineTests|FullyQualifiedName~RecipesApiTests|FullyQualifiedName~PublishRecipeCommandTests|FullyQualifiedName~Lab5RecipeSecurityTests"
dotnet test tests/CulinaryBlog.Infrastructure.IntegrationTests/CulinaryBlog.Infrastructure.IntegrationTests.csproj --filter FullyQualifiedName~RecipeRepositorySupabaseTests
cd frontend
npm ci
npm run test:recipes
npm run typecheck
npm run build
```

Bo test SQL dung Npgsql translator thuc, chan ket noi va gia lap ket qua COUNT;
kiem tra FTS, facet, sort, LIMIT/OFFSET va projection khong tai Nutrition/Steps/Ingredients.
Khong thay the test tren PostgreSQL thuc. Test sitemap dung EF InMemory va thu muc
tam, khong goi Google. Test cloud chi doc va can du lieu seed it nhat 100 cong thuc,
co mon Bún Bò da Published.

Ket qua ngay 20/10/2026: 35 test backend cuc bo dat. Test cloud: 1 ca danh sach dat,
5 ca FTS bi chan boi PostgreSQL 42703 (thieu SearchVector). Frontend: 5 test
tham so dat, typecheck va production build dat. Chua xac nhan Hangfire chay luc
02:00 UTC hoac Google Search Console tren hosting.

Kiem tra trinh duyet voi API gia lap: desktop 1440px va mobile 390px dat;
chuyen trang, reset trang khi tim kiem, huy request cu, tach error/empty state,
bo loc mobile va khong tran ngang. Khong co page error. Ket qua nay xac minh
giao dien Lab 6, khong thay the nghiem thu FTS Supabase.

## Phan trang va giao dien Lab 6

API tu choi offset lon hon Int32.MaxValue truoc truy van, PageSize toi da 50.
Frontend chuan hoa trang sai ve 1 va gioi han page size 50. Tu khoa, filter va
trang duoc quan ly bang URL; auth store Zustand la ha tang chung, khong nhan
phan Auth la dong gop rieng cua TV4.

Khi doi bo loc, trang quay ve 1; request cu bi huy khi doi query/roi trang.
Response cu khong ghi de ket qua moi. Khi API loi, an ket qua va phan trang cu,
hien thong bao va nut thu lai; empty state chi hien khi request thanh cong rong.
Sitemap giu nguyen duong dan nen (vi du /blog/) khi ghep URL cong thuc.
