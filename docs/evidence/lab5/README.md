# Minh chứng Lab 5 của Phan Khánh Vương

Ngày đối chiếu: 13/10/2026. Các kết quả dưới đây được chạy trong workspace này.

| Kiểm tra | Kết quả |
| --- | --- |
| `npm ci --no-audit --no-fund` trong frontend | Thành công, 424 packages; giữ lockfile |
| `npm run typecheck` trong frontend | Thành công |
| `npm run build` trong frontend | Thành công, Next.js 15.5.27, prerender 4/4 |
| GET http://127.0.0.1:3000 | 200, HTML chứa Culinary Blog; xem frontend-http.json |
| `node scripts/lab5/verify-recipe-service.cjs` | Đạt 11 thao tác service; JSON, Bearer giả, multipart, void và URL encoding |
| Bộ test backend local | 53 đạt, 0 lỗi; xem local-test-results.json |
| Lab5RecipeSecurityTests | 12 đạt: 3 handler, 4 HTTP ownership, 1 JWT lỗi, 2 CORS, 1 limiter, 1 OpenAPI/Scalar |

Lệnh chạy test từ thư mục gốc:

```powershell
dotnet test tests/CulinaryBlog.Infrastructure.IntegrationTests --no-restore --filter "FullyQualifiedName!~RecipeRepositorySupabaseTests&FullyQualifiedName!~RecipeMigrationTests" --logger "trx;LogFileName=lab5-local.trx" -m:1 /nodeReuse:false
```

Backend thử nghiệm dùng Kestrel local, JWT ký bằng khóa thử nghiệm, endpoint và
handler phân quyền thật, repository/cache giả trong bộ nhớ. CORS, Rate Limiting
và đăng ký OpenAPI dùng chung code với Program.cs. Kiểm tra HTTP cập nhật Recipe:
không token 401; người khác 403; chủ sở hữu hoặc Admin 200. Không có tài khoản
Identity thật, không kết nối Supabase, không chạy Hangfire, migration hoặc seed.

Giới hạn chung vẫn là 60 request/phút/IP với 2 chỗ trong hàng đợi; test gửi 60
request thành công, sau đó 3 request đồng thời để xác nhận ít nhất một request
trả 429, hủy các request đang đợi. Preflight được phép trả header cho origin
localhost:3000; origin khác không nhận Access-Control-Allow-Origin.

OpenAPI test kiểm tra schema endpoint cập nhật, response 200/401/403/404/409/422,
Bearer JWT, multipart upload và response 400. Scalar endpoint trả HTML 200;
chưa kiểm chứng tương tác JavaScript trong trình duyệt hoặc tải ảnh lên storage thật.

API versioning hiện có đường dẫn /api/v1, chưa có cơ chế quản lý phiên bản riêng.
Việc hoàn thiện versioning và nghiệm thu với tài khoản Supabase là đầu việc phối
hợp nhóm được ghi trong báo cáo; không có thông điệp gửi ngoài repository.

Các log lỗi trong lần chạy đầu không phải kết quả cuối. JSON lưu kết quả của lần
chạy đạt 53/53. Minh chứng không chứa token thật, mật khẩu hay chuỗi kết nối.

Báo cáo DOCX đã được chỉnh theo mẫu Lab 3 và Lab 4, kiểm tra trực quan cả ba trang A4 ngang. `render_docx.py` không
chạy được do thiếu LibreOffice trên Windows; bản sao PDF được xuất bằng Word ẩn,
sau đó raster bằng Poppler trong Codex runtime để kiểm tra. Bản PDF/PNG chỉ dùng
cho QA trong scratch, không phải tệp nộp. Front-end thử nghiệm đã dừng sau kiểm tra.

Mẫu gốc Lab 4 được giữ nguyên; script tạo báo cáo chỉ thay `word/document.xml`
trên bản sao, giữ nguyên các package part khác. Khoảng báo cáo Lab 5 dùng
06/10–13/10/2026, nối tiếp Lab 4. Tiến độ phần bảo mật 80% và tài liệu/versioning
50% là ước lượng theo đầu việc trong bảng; các phần môi trường, thư viện và types
đạt 100% trong phạm vi tối thiểu Lab 5. Liên kết dùng branch feature/vuong-recipe-frontend.
