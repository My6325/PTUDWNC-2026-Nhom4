import { apiClient } from "./apiClient";
import type { PaginatedResult, RecipeDto, RecipeNutritionDto, RecipeStepResponseDto, RecipeIngredientResponseDto, RecipeImageResponseDto, UpdateRecipeRequest } from "@/types/recipe";

export type WorkflowAction = "submit" | "withdraw" | "publish" | "reject" | "archive" | "unarchive" | "unpublish" | "delete" | "restore";
export interface ManagedRecipeItem {
  id: string; title: string; slug: string; status: number | string; isDeleted: boolean;
  authorId: string; createdAt: string; reviewNote?: string | null; rowVersion: string;
}
export interface ManagedRecipe {
  recipe: RecipeDto; reviewNote?: string | null; nutrition: RecipeNutritionDto;
  steps: RecipeStepResponseDto[]; ingredients: RecipeIngredientResponseDto[]; images: RecipeImageResponseDto[];
}
export interface EditorStep { id?: string; title: string; description: string; timerMinutes?: number | null; imageUrl?: string | null; }
export interface EditorIngredient { id?: string; name: string; quantity?: number | null; unit?: string | null; notes?: string | null; }
export const recipeManagementService = {
  async list(status: string, pageIndex: number, reviewQueue: boolean, signal?: AbortSignal) {
    return (await apiClient.get<PaginatedResult<ManagedRecipeItem>>(`/recipes/${reviewQueue ? "review-queue" : "mine"}`, {
      params: { ...(status === "Trash" ? { trash: true } : { status }), pageIndex, pageSize: 12 }, signal,
    })).data;
  },
  async get(id: string, signal?: AbortSignal) {
    return (await apiClient.get<ManagedRecipe>(`/recipes/manage/${encodeURIComponent(id)}`, { signal })).data;
  },
  async save(id: string, recipe: UpdateRecipeRequest, steps: EditorStep[], ingredients: EditorIngredient[]) {
    return (await apiClient.put<RecipeDto>(`/recipes/manage/${encodeURIComponent(id)}`, { recipe, steps, ingredients })).data;
  },
  async act(item: { id: string; rowVersion: string }, action: WorkflowAction, reason?: string) {
    const url = `/recipes/${encodeURIComponent(item.id)}${action === "delete" ? "" : `/${action}`}`;
    await apiClient.request({ url, method: action === "delete" ? "DELETE" : "POST",
      headers: { "If-Match": `"${item.rowVersion}"` }, data: action === "reject" ? { reason } : undefined });
  },
};

export function recipeStatusName(value: number | string): string {
  return ({ 1: "Draft", 2: "Published", 3: "Archived", 4: "PendingApproval" } as Record<string, string>)[String(value)] ?? String(value);
}
