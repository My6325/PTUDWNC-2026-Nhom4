import { apiClient } from "./apiClient";
import { CategoryDto, CategoryDetailDto, CreateCategoryRequest, UpdateCategoryRequest } from "@/types/category";

/**
 * Service giao tiếp với Backend API cho module Danh mục ẩm thực (FR-CAT).
 * Tuân thủ chuẩn RESTful, Clean Architecture và tài liệu đặc tả dự án.
 */
export const categoryService = {
  /**
   * Lấy danh sách danh mục ẩm thực đang hoạt động (FR-CAT-001).
   * Backend trả về danh mục đã được In-Memory Cache (TTL 60 phút).
   */
  async getCategories(signal?: AbortSignal): Promise<CategoryDto[]> {
    const response = await apiClient.get<CategoryDto[]>("/categories", { signal });
    return response.data;
  },

  /**
   * Lấy danh sách các danh mục ẩm thực trong Thùng rác (Yêu cầu quyền Admin - FR-CAT-005).
   */
  async getTrashCategories(signal?: AbortSignal): Promise<CategoryDto[]> {
    const response = await apiClient.get<CategoryDto[]>("/categories/trash", { signal });
    return response.data;
  },

  /**
   * Lấy thông tin chi tiết danh mục và danh sách công thức thuộc danh mục theo Slug (FR-CAT-002).
   */
  async getCategoryBySlug(slug: string, signal?: AbortSignal): Promise<CategoryDetailDto> {
    const response = await apiClient.get<CategoryDetailDto>(`/categories/${slug}`, { signal });
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

  /**
   * Xóa vĩnh viễn danh mục ẩm thực khỏi CSDL (Yêu cầu quyền Admin - FR-CAT-005 Hard Delete).
   * Xóa vật lý bản ghi và gỡ liên kết khỏi các món ăn. Không thể khôi phục.
   */
  async hardDeleteCategory(id: string): Promise<void> {
    await apiClient.delete(`/categories/${id}/permanent`);
  },

  /**
   * Kích hoạt quét dọn dẹp vĩnh viễn các danh mục trong Thùng rác đã quá hạn 30 ngày (Admin).
   */
  async triggerCleanup(): Promise<{ message: string }> {
    const response = await apiClient.post<{ message: string }>("/categories/cleanup");
    return response.data;
  },
};
