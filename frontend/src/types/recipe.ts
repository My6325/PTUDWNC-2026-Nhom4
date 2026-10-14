/** JSON contracts for Recipe Core and Search/Detail APIs. */
export enum RecipeDifficulty {
  Easy = 1,
  Medium = 2,
  Hard = 3,
}

export enum RecipeStatus {
  Draft = 1,
  Published = 2,
  Archived = 3,
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
}

export interface RecipeListItem {
  id: string;
  title: string;
  slug: string;
  coverImageUrl: string | null;
  categoryName: string | null;
  authorName: string;
  difficulty: RecipeDifficulty;
  createdAt: string;
}

export interface RecipeSearchParams {
  SearchTerm?: string;
  CategoryId?: string;
  Difficulty?: "Easy" | "Medium" | "Hard";
  MinCookTimeMinutes?: number;
  MaxCookTimeMinutes?: number;
  MinServings?: number;
  MaxServings?: number;
  SortBy?: "newest" | "cookTime" | "relevance";
  PageIndex?: number;
  PageSize?: number;
}

export interface RecipeAuthor {
  name: string;
  avatarUrl: string | null;
}

export interface RecipeCategory {
  id: string;
  name: string;
  slug: string;
}

export interface RecipeStep {
  stepNumber: number;
  title: string;
  description: string;
  timerMinutes: number | null;
  imageUrl: string | null;
}

export interface RecipeIngredient {
  name: string;
  quantity: number | null;
  unit: string | null;
  notes: string | null;
  orderIndex: number;
}

export interface RecipeImage {
  originalUrl: string;
  mediumUrl: string | null;
  thumbnailUrl: string | null;
  isPrimary: boolean;
  orderIndex: number;
}

export interface RecipeNutrition {
  calories: number | null;
  protein: number | null;
  carbohydrates: number | null;
  fat: number | null;
  fiber: number | null;
  sodium: number | null;
}

export interface RecipeNutritionRequest {
  calories: number | null;
  protein: number | null;
  carbohydrates: number | null;
  fat: number | null;
  fiber: number | null;
  sodium: number | null;
}

export interface RecipeDetail {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  instructions: string | null;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  totalTimeMinutes: number;
  servings: number;
  difficulty: RecipeDifficulty;
  status: RecipeStatus;
  datePublished: string;
  authorId: string;
  author: RecipeAuthor;
  category: RecipeCategory | null;
  steps: RecipeStep[];
  ingredients: RecipeIngredient[];
  images: RecipeImage[];
  nutrition: RecipeNutrition;
}

export interface CreateRecipeStepRequest {
  title: string;
  description: string;
  timerMinutes: number | null;
  imageUrl: string | null;
}

export interface CreateRecipeIngredientRequest {
  name: string;
  quantity: number | null;
  unit: string | null;
  notes: string | null;
}

export interface CreateRecipeDraftRequest {
  title: string;
  description: string | null;
  categoryId: string | null;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: RecipeDifficulty;
  instructions: string | null;
  nutrition: RecipeNutritionRequest | null;
  steps: CreateRecipeStepRequest[] | null;
  ingredients: CreateRecipeIngredientRequest[] | null;
}

export interface UpdateRecipeRequest {
  title: string;
  description: string;
  instructions: string | null;
  categoryId: string | null;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: RecipeDifficulty;
  nutrition: RecipeNutritionRequest | null;
  /** Base64 received from RecipeDto; send unchanged to detect concurrent updates. */
  rowVersion: string;
}

export interface RecipeDto {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  instructions: string | null;
  categoryId: string | null;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: RecipeDifficulty;
  status: RecipeStatus;
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
  mediumUrl: string | null;
  thumbnailUrl: string | null;
  isPrimary: boolean;
  orderIndex: number;
}
