import { apiClient } from "./apiClient";
import type {
  PaginatedResult,
  RecipeDetail,
  RecipeListItem,
  RecipeSearchParams,
} from "@/types/recipe";

export async function getRecipes(
  params: RecipeSearchParams = {},
): Promise<PaginatedResult<RecipeListItem>> {
  const response = await apiClient.get<PaginatedResult<RecipeListItem>>("/recipes", {
    params,
  });
  return response.data;
}

export async function getRecipeBySlug(slug: string): Promise<RecipeDetail> {
  const response = await apiClient.get<RecipeDetail>(`/recipes/${encodeURIComponent(slug)}`);
  return response.data;
}
