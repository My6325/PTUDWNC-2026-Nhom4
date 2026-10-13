import { apiClient } from "./apiClient";
import type {
  AddIngredientRequest,
  AddRecipeStepRequest,
  CreateRecipeDraftRequest,
  RecipeDto,
  RecipeImageResponseDto,
  RecipeIngredientResponseDto,
  RecipeStepResponseDto,
  UpdateIngredientRequest,
  UpdateRecipeRequest,
  UpdateRecipeStepRequest,
} from "@/types/recipe";

const recipePath = (id: string) => `/recipes/${encodeURIComponent(id)}`;

/** Errors remain AxiosError so callers can inspect status and ProblemDetails. */
export const recipeService = {
  async createDraft(request: CreateRecipeDraftRequest): Promise<RecipeDto> {
    return (await apiClient.post<RecipeDto>("/recipes", request)).data;
  },
  async update(id: string, request: UpdateRecipeRequest): Promise<RecipeDto> {
    return (await apiClient.put<RecipeDto>(recipePath(id), request)).data;
  },
  async delete(id: string): Promise<void> {
    await apiClient.delete<void>(recipePath(id));
  },
  async addStep(id: string, request: AddRecipeStepRequest): Promise<RecipeStepResponseDto> {
    return (await apiClient.post<RecipeStepResponseDto>(`${recipePath(id)}/steps`, request)).data;
  },
  async updateStep(id: string, stepId: string, request: UpdateRecipeStepRequest): Promise<RecipeStepResponseDto> {
    return (await apiClient.put<RecipeStepResponseDto>(`${recipePath(id)}/steps/${encodeURIComponent(stepId)}`, request)).data;
  },
  async deleteStep(id: string, stepId: string): Promise<void> {
    await apiClient.delete<void>(`${recipePath(id)}/steps/${encodeURIComponent(stepId)}`);
  },
  async addIngredient(id: string, request: AddIngredientRequest): Promise<RecipeIngredientResponseDto> {
    return (await apiClient.post<RecipeIngredientResponseDto>(`${recipePath(id)}/ingredients`, request)).data;
  },
  async updateIngredient(id: string, ingredientId: string, request: UpdateIngredientRequest): Promise<RecipeIngredientResponseDto> {
    return (await apiClient.put<RecipeIngredientResponseDto>(`${recipePath(id)}/ingredients/${encodeURIComponent(ingredientId)}`, request)).data;
  },
  async deleteIngredient(id: string, ingredientId: string): Promise<void> {
    await apiClient.delete<void>(`${recipePath(id)}/ingredients/${encodeURIComponent(ingredientId)}`);
  },
  async uploadImage(id: string, file: File): Promise<RecipeImageResponseDto> {
    const data = new FormData();
    data.append("file", file);
    return (await apiClient.post<RecipeImageResponseDto>(`${recipePath(id)}/images`, data, {
      // Remove the client's JSON default; the browser supplies the multipart boundary.
      headers: { "Content-Type": undefined },
    })).data;
  },
  async deleteImage(id: string, imageId: string): Promise<void> {
    await apiClient.delete<void>(`${recipePath(id)}/images/${encodeURIComponent(imageId)}`);
  },
};
