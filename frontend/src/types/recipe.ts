export type RecipeDifficulty = 1 | 2 | 3;
export type RecipeStatus = 1 | 2 | 3;

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
