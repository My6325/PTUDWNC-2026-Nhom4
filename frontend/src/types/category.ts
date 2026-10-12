/**
 * Định nghĩa các kiểu dữ liệu TypeScript (TS Types) cho Phân hệ Danh mục Ẩm thực (FR-CAT).
 * Khớp 100% với DTO của Backend .NET 10.
 */

export interface CategoryDto {
  id: string;
  name: string;
  slug: string;
  description?: string;
  imageUrl?: string;
  orderIndex: number;
}

export interface RecipeSummaryDto {
  id: string;
  title: string;
  slug: string;
  imageUrl?: string;
  cookingTimeMinutes: number;
}

export interface CategoryDetailDto {
  id: string;
  name: string;
  slug: string;
  description?: string;
  imageUrl?: string;
  orderIndex: number;
  recipes: RecipeSummaryDto[];
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
