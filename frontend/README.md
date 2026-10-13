# Front-end Culinary Blog

Yêu cầu: Node.js 20.9 trở lên và npm; dùng dependencies trong package-lock.json.

```powershell
cd frontend
npm ci
Copy-Item .env.example .env.local
npm run typecheck
npm run build
npm run dev -- --port 3000
```

Địa chỉ giao diện: http://localhost:3000. Backend dùng profile `http` tại cổng 5000;
`NEXT_PUBLIC_API_URL` mặc định là `http://localhost:5000/api/v1`. Chỉ biến công khai
được đặt tiền tố `NEXT_PUBLIC_`; không lưu token/mật khẩu vào biến này.

`src/types/recipe.ts` khớp contracts Recipe Core của backend. Enum gửi dạng số;
GUID và RowVersion Base64 gửi dạng chuỗi. Khi cập nhật, gửi lại nguyên RowVersion
backend trả về. Các trường nullable dùng `null`, không đổi thành chuỗi rỗng.

`src/services/recipeService.ts` cung cấp tạo/sửa/xóa Recipe, Steps, Ingredients và
upload/xóa Images. Service trả DTO hoặc void, giữ nguyên AxiosError để giao diện
xử lý mã HTTP/ProblemDetails. Upload dùng FormData với trường `file`, để trình
duyệt tự tạo multipart boundary. apiClient lấy accessToken từ localStorage trong
trình duyệt; các thao tác có xác thực cần được gọi từ phía client.

Khung components/recipes và hooks được giữ bằng .gitkeep; chưa có giao diện soạn
thảo hoàn chỉnh. Không thêm thư viện mới trong Lab 5.

Kiểm tra bảo mật và tài liệu API Recipe trên backend thử nghiệm local,
từ thư mục gốc repository:

```powershell
dotnet test tests/CulinaryBlog.Infrastructure.IntegrationTests --no-restore --filter "FullyQualifiedName~Lab5RecipeSecurityTests" -m:1 /nodeReuse:false
```

Các test dùng JWT thử nghiệm và repository trong bộ nhớ, không kết nối Supabase.
