import { apiClient } from "./apiClient";
import {
  RecipeListDto,
  RecipeDetailDto,
  GetRecipesParams,
} from "@/types/recipe";
import { PaginatedResult } from "@/types/common";

/**
 * Service tích hợp dữ liệu Công thức nấu ăn (FR-RCP & FR-SRCH).
 * Tái sử dụng instance apiClient, kết nối trực tiếp đến .NET 10 Minimal APIs.
 */
export const recipeService = {
  /**
   * Lấy danh sách công thức phân trang, hỗ trợ tìm kiếm toàn văn và bộ lọc (FR-RCP-001 & FR-SRCH).
   */
  async getRecipes(params?: GetRecipesParams): Promise<PaginatedResult<RecipeListDto>> {
    // Chuẩn hóa và làm sạch query params
    const queryParams: Record<string, unknown> = {};

    if (params) {
      if (params.searchTerm?.trim()) {
        queryParams.SearchTerm = params.searchTerm.trim();
      }

      if (params.categoryId) {
        queryParams.CategoryId = params.categoryId;
      }

      if (params.difficulty) {
        queryParams.Difficulty = params.difficulty;
      }

      if (typeof params.minCookTimeMinutes === "number") {
        queryParams.MinCookTimeMinutes = params.minCookTimeMinutes;
      }

      if (typeof params.maxCookTimeMinutes === "number") {
        queryParams.MaxCookTimeMinutes = params.maxCookTimeMinutes;
      }

      if (typeof params.minServings === "number") {
        queryParams.MinServings = params.minServings;
      }

      if (typeof params.maxServings === "number") {
        queryParams.MaxServings = params.maxServings;
      }

      // Xử lý logic an toàn cho SortBy:
      // "relevance" bắt buộc phải có SearchTerm, nếu không có từ khóa thì fallback về "newest" để tránh lỗi 422 từ Backend.
      if (params.sortBy === "relevance" && !queryParams.SearchTerm) {
        queryParams.SortBy = "newest";
      } else if (params.sortBy) {
        queryParams.SortBy = params.sortBy;
      } else {
        queryParams.SortBy = "newest";
      }

      if (typeof params.pageIndex === "number" && params.pageIndex > 0) {
        queryParams.PageIndex = params.pageIndex;
      }

      if (typeof params.pageSize === "number" && params.pageSize > 0) {
        queryParams.PageSize = params.pageSize;
      }
    }

    const response = await apiClient.get<PaginatedResult<RecipeListDto>>("/recipes", {
      params: queryParams,
    });

    return response.data;
  },

  /**
   * Lấy chi tiết toàn bộ công thức món ăn theo slug (FR-RCP-002).
   */
  async getRecipeBySlug(slug: string): Promise<RecipeDetailDto> {
    const response = await apiClient.get<RecipeDetailDto>(`/recipes/${encodeURIComponent(slug)}`);
    return response.data;
  },

  /**
   * Xuất bản công thức nấu ăn (Yêu cầu Author sở hữu hoặc Admin - FR-RCP-005).
   */
  async publishRecipe(id: string): Promise<void> {
    await apiClient.put(`/recipes/${id}/publish`);
  },

  /**
   * Lưu trữ công thức nấu ăn (Ẩn khỏi trang chủ - FR-RCP-006).
   */
  async archiveRecipe(id: string): Promise<void> {
    await apiClient.post(`/recipes/${id}/archive`);
  },

  /**
   * Khôi phục công thức nấu ăn đã lưu trữ (FR-RCP-006).
   */
  async unarchiveRecipe(id: string): Promise<void> {
    await apiClient.post(`/recipes/${id}/unarchive`);
  },

  /**
   * Xóa mềm công thức nấu ăn (FR-RCP-007).
   */
  async deleteRecipe(id: string): Promise<void> {
    await apiClient.delete(`/recipes/${id}`);
  },

  /**
   * Tải ảnh công thức lên Supabase Storage (FR-RCP-008 & FR-FILE-001).
   */
  async uploadRecipeImage(recipeId: string, file: File): Promise<unknown> {
    const formData = new FormData();
    formData.append("file", file);

    const response = await apiClient.post(`/recipes/${recipeId}/images`, formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    });

    return response.data;
  },
};
