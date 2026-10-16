/**
 * Định nghĩa các kiểu dữ liệu TypeScript cho Phân hệ Quản lý & Khám phá Công thức (FR-RCP & FR-SRCH).
 * Tích hợp toàn diện DTOs của TV1, TV2, TV3, và TV4 khớp 100% với C# Backend .NET 10 DTOs.
 */

export enum RecipeDifficulty {
  Easy = 1,
  Medium = 2,
  Hard = 3,
}

export type RecipeDifficultyValue = "Easy" | "Medium" | "Hard" | "easy" | "medium" | "hard" | 1 | 2 | 3;

export enum RecipeStatus {
  Draft = 1,
  Published = 2,
  Archived = 3,
}

export type RecipeStatusValue = "Draft" | "Published" | "Archived" | 1 | 2 | 3;

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
}

/**
 * DTO danh sách món ăn từ GET /api/v1/recipes
 */
export interface RecipeListDto {
  id: string;
  title: string;
  slug: string;
  coverImageUrl?: string | null;
  categoryName?: string | null;
  authorName: string;
  difficulty: RecipeDifficulty | RecipeDifficultyValue;
  createdAt: string;
}

export type RecipeListItem = RecipeListDto;

/**
 * Tham số truy vấn tìm kiếm, lọc và phân trang công thức
 * Khớp với GetRecipesQuery trong C# Backend
 */
export interface RecipeSearchParams {
  searchTerm?: string;
  SearchTerm?: string;
  categoryId?: string;
  CategoryId?: string;
  difficulty?: "Easy" | "Medium" | "Hard" | string;
  Difficulty?: "Easy" | "Medium" | "Hard" | string;
  minCookTimeMinutes?: number;
  MinCookTimeMinutes?: number;
  maxCookTimeMinutes?: number;
  MaxCookTimeMinutes?: number;
  minServings?: number;
  MinServings?: number;
  maxServings?: number;
  MaxServings?: number;
  sortBy?: "newest" | "cookTime" | "relevance" | string;
  SortBy?: "newest" | "cookTime" | "relevance" | string;
  pageIndex?: number;
  PageIndex?: number;
  pageSize?: number;
  PageSize?: number;
}

export type GetRecipesParams = RecipeSearchParams;

export interface RecipeAuthorDto {
  name: string;
  avatarUrl?: string | null;
}

export type RecipeAuthor = RecipeAuthorDto;

export interface RecipeCategoryDto {
  id: string;
  name: string;
  slug: string;
}

export type RecipeCategory = RecipeCategoryDto;

export interface RecipeStepDto {
  id?: string;
  recipeId?: string;
  stepNumber: number;
  title: string;
  description: string;
  timerMinutes?: number | null;
  imageUrl?: string | null;
}

export type RecipeStep = RecipeStepDto;

export interface RecipeIngredientDto {
  id?: string;
  recipeId?: string;
  name: string;
  quantity?: number | null;
  unit?: string | null;
  notes?: string | null;
  orderIndex: number;
}

export type RecipeIngredient = RecipeIngredientDto;

export interface RecipeImageDto {
  id?: string;
  recipeId?: string;
  originalUrl: string;
  mediumUrl?: string | null;
  thumbnailUrl?: string | null;
  isPrimary: boolean;
  orderIndex: number;
}

export type RecipeImage = RecipeImageDto;

export interface RecipeNutritionDto {
  calories?: number | null;
  protein?: number | null;
  carbohydrates?: number | null;
  fat?: number | null;
  fiber?: number | null;
  sodium?: number | null;
}

export type RecipeNutrition = RecipeNutritionDto;
export type RecipeNutritionRequest = RecipeNutritionDto;

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
  difficulty: RecipeDifficulty | RecipeDifficultyValue;
  status: RecipeStatus | RecipeStatusValue;
  datePublished: string;
  authorId: string;
  author: RecipeAuthorDto;
  category?: RecipeCategoryDto | null;
  steps: RecipeStepDto[];
  ingredients: RecipeIngredientDto[];
  images: RecipeImageDto[];
  nutrition: RecipeNutritionDto;
}

export type RecipeDetail = RecipeDetailDto;

export interface CreateRecipeStepRequest {
  title: string;
  description: string;
  timerMinutes?: number | null;
  imageUrl?: string | null;
}

export interface CreateRecipeIngredientRequest {
  name: string;
  quantity?: number | null;
  unit?: string | null;
  notes?: string | null;
}

export interface CreateRecipeDraftRequest {
  title: string;
  description?: string | null;
  categoryId?: string | null;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: RecipeDifficulty | RecipeDifficultyValue;
  instructions?: string | null;
  nutrition?: RecipeNutritionDto | null;
  steps?: CreateRecipeStepRequest[] | null;
  ingredients?: CreateRecipeIngredientRequest[] | null;
}

export interface UpdateRecipeRequest {
  title: string;
  description?: string | null;
  instructions?: string | null;
  categoryId?: string | null;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: RecipeDifficulty | RecipeDifficultyValue;
  nutrition?: RecipeNutritionDto | null;
  /** Base64 received from RecipeDto; send unchanged to detect concurrent updates. */
  rowVersion: string;
}

export interface RecipeDto {
  id: string;
  title: string;
  slug: string;
  description?: string | null;
  instructions?: string | null;
  categoryId?: string | null;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: RecipeDifficulty | RecipeDifficultyValue;
  status: RecipeStatus | RecipeStatusValue;
  authorId: string;
  rowVersion: string;
}

export type AddRecipeStepRequest = CreateRecipeStepRequest;
export type UpdateRecipeStepRequest = CreateRecipeStepRequest;
export type AddIngredientRequest = CreateRecipeIngredientRequest;
export type UpdateIngredientRequest = CreateRecipeIngredientRequest;

export interface RecipeStepResponseDto extends CreateRecipeStepRequest {
  id: string;
  recipeId: string;
  stepNumber: number;
}

export interface RecipeIngredientResponseDto extends CreateRecipeIngredientRequest {
  id: string;
  recipeId: string;
  orderIndex: number;
}

export interface RecipeImageResponseDto {
  id: string;
  recipeId: string;
  originalUrl: string;
  mediumUrl?: string | null;
  thumbnailUrl?: string | null;
  isPrimary: boolean;
  orderIndex: number;
}
