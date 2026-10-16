/**
 * Định nghĩa các kiểu dữ liệu TypeScript cho Phân hệ Danh mục Ẩm thực (FR-CAT).
 * Khớp 100% với C# DTOs: CategoryDto, CategoryDetailDto, CategoryRecipeSummaryDto.
 */

export interface CategoryDto {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  imageUrl?: string | null;
  orderIndex: number;
}

export interface CategoryRecipeSummaryDto {
  id: string;
  title: string;
  slug: string;
  description?: string | null;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: string;
  primaryImageUrl?: string | null;
}

export interface CategoryDetailDto {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  imageUrl?: string | null;
  orderIndex: number;
  recipes: CategoryRecipeSummaryDto[];
}

export interface CreateCategoryRequest {
  name: string;
  description?: string;
  imageUrl?: string;
}

export interface UpdateCategoryRequest {
  name: string;
  description?: string;
  imageUrl?: string;
  orderIndex: number;
}
