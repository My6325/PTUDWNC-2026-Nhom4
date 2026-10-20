import { apiClient } from "./apiClient";
import { CategoryDto, CategoryDetailDto, CreateCategoryRequest, UpdateCategoryRequest } from "@/types/category";

/**
 * Service tích hợp dữ liệu Danh mục Ẩm thực (FR-CAT).
 * Sử dụng duy nhất instance apiClient, kết nối trực tiếp đến .NET 10 Minimal APIs.
 */
export const categoryService = {
  /**
   * Lấy danh sách toàn bộ các danh mục ẩm thực đã sắp xếp theo thứ tự (FR-CAT-001).
   * Backend có In-Memory Cache TTL 60 phút.
   */
  async getCategories(): Promise<CategoryDto[]> {
    const response = await apiClient.get<CategoryDto[]>("/categories");
    return response.data;
  },

  /**
   * Lấy danh sách danh mục trong Thùng rác (đã bị xóa mềm) dành cho Admin.
   */
  async getTrashCategories(): Promise<CategoryDto[]> {
    const response = await apiClient.get<CategoryDto[]>("/categories/trash");
    return response.data;
  },

  /**
   * Lấy chi tiết danh mục kèm danh sách công thức tóm tắt theo slug (FR-CAT-002).
   */
  async getCategoryBySlug(slug: string): Promise<CategoryDetailDto> {
    const response = await apiClient.get<CategoryDetailDto>(`/categories/${encodeURIComponent(slug)}`);
    return response.data;
  },

  /**
   * Tạo mới danh mục ẩm thực (Yêu cầu quyền Admin - FR-CAT-003).
   */
  async createCategory(request: CreateCategoryRequest): Promise<CategoryDto> {
    const response = await apiClient.post<CategoryDto>("/categories", request);
    return response.data;
  },

  /**
   * Cập nhật thông tin danh mục ẩm thực (Yêu cầu quyền Admin - FR-CAT-004).
   */
  async updateCategory(id: string, request: UpdateCategoryRequest): Promise<CategoryDto> {
    const response = await apiClient.put<CategoryDto>(`/categories/${id}`, request);
    return response.data;
  },

  /**
   * Xóa mềm danh mục ẩm thực (Yêu cầu quyền Admin - FR-CAT-005).
   * Chuyển danh mục vào Thùng rác lưu trữ 30 ngày, bảo toàn các công thức liên quan.
   */
  async deleteCategory(id: string): Promise<void> {
    await apiClient.delete(`/categories/${id}`);
  },

  /**
   * Khôi phục danh mục ẩm thực từ Thùng rác về trạng thái hoạt động (Admin).
   */
  async restoreCategory(id: string): Promise<CategoryDto> {
    const response = await apiClient.post<CategoryDto>(`/categories/${id}/restore`);
    return response.data;
  },
};
