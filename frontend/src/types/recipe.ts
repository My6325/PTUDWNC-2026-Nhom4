/**
 * Định nghĩa các kiểu dữ liệu TypeScript cho Phân hệ Quản lý & Khám phá Công thức (FR-RCP & FR-SRCH).
 * Khớp 100% với C# Backend .NET 10 DTOs.
 */

export type RecipeDifficulty = "Easy" | "Medium" | "Hard" | "easy" | "medium" | "hard" | 1 | 2 | 3;
export type RecipeStatus = "Draft" | "Published" | "Archived" | 1 | 2 | 3;

/**
 * DTO danh sách món ăn từ GET /api/v1/recipes
 * LƯU Ý: Không có Description, không có Rating, không có ViewCount.
 */
export interface RecipeListDto {
  id: string;
  title: string;
  slug: string;
  coverImageUrl?: string | null;
  categoryName?: string | null;
  authorName: string;
  difficulty: RecipeDifficulty;
  createdAt: string;
}

export interface RecipeAuthorDto {
  name: string;
  avatarUrl?: string | null;
}

export interface RecipeCategoryDto {
  id: string;
  name: string;
  slug: string;
}

export interface RecipeStepDto {
  stepNumber: number;
  title: string;
  description: string;
  timerMinutes?: number | null;
  imageUrl?: string | null;
}

export interface RecipeIngredientDto {
  name: string;
  quantity?: number | null;
  unit?: string | null;
  notes?: string | null;
  orderIndex: number;
}

export interface RecipeImageDto {
  originalUrl: string;
  mediumUrl?: string | null;
  thumbnailUrl?: string | null;
  isPrimary: boolean;
  orderIndex: number;
}

export interface RecipeNutritionDto {
  calories?: number | null;
  protein?: number | null;
  carbohydrates?: number | null;
  fat?: number | null;
  fiber?: number | null;
  sodium?: number | null;
}

/**
 * DTO chi tiết công thức nấu ăn từ GET /api/v1/recipes/{slug}
 */
export interface RecipeDetailDto {
  id: string;
  title: string;
  slug: string;
  description?: string | null;
  instructions?: string | null;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  totalTimeMinutes: number;
  servings: number;
  difficulty: RecipeDifficulty;
  status: RecipeStatus;
  datePublished: string;
  authorId: string;
  author: RecipeAuthorDto;
  category?: RecipeCategoryDto | null;
  steps: RecipeStepDto[];
  ingredients: RecipeIngredientDto[];
  images: RecipeImageDto[];
  nutrition: RecipeNutritionDto;
}

/**
 * Tham số truy vấn tìm kiếm, lọc và phân trang công thức
 * Khớp với GetRecipesQuery trong C#
 */
export interface GetRecipesParams {
  searchTerm?: string;
  categoryId?: string;
  difficulty?: "Easy" | "Medium" | "Hard" | string;
  minCookTimeMinutes?: number;
  maxCookTimeMinutes?: number;
  minServings?: number;
  maxServings?: number;
  sortBy?: "newest" | "cookTime" | "relevance" | string;
  pageIndex?: number;
  pageSize?: number;
}
