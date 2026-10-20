import { apiClient } from "./apiClient";
import { buildRecipeQuery } from "@/utils/recipeQuery";
import type {
  AddIngredientRequest,
  AddRecipeStepRequest,
  CreateRecipeDraftRequest,
  GetRecipesParams,
  PaginatedResult,
  RecipeDetailDto,
  RecipeDto,
  RecipeImageResponseDto,
  RecipeIngredientResponseDto,
  RecipeListDto,
  RecipeSearchParams,
  RecipeStepResponseDto,
  UpdateIngredientRequest,
  UpdateRecipeRequest,
  UpdateRecipeStepRequest,
} from "@/types/recipe";

const recipePath = (id: string) => `/recipes/${encodeURIComponent(id)}`;

/**
 * Lấy danh sách công thức đã xuất bản kèm tìm kiếm FTS tiếng Việt, lọc đa tiêu chí và phân trang.
 * Tự động làm sạch tham số và xử lý an toàn cho sắp xếp theo relevance.
 */
export async function getRecipes(
  params?: RecipeSearchParams,
  signal?: AbortSignal,
): Promise<PaginatedResult<RecipeListDto>> {
  const response = await apiClient.get<PaginatedResult<RecipeListDto>>("/recipes", {
    params: buildRecipeQuery(params),
    signal,
  });
  return response.data;
}

/**
 * Lấy chi tiết công thức ẩm thực theo Slug URL.
 */
export async function getRecipeBySlug(slug: string): Promise<RecipeDetailDto> {
  const response = await apiClient.get<RecipeDetailDto>(`/recipes/${encodeURIComponent(slug)}`);
  return response.data;
}

/**
 * Service quản trị & khám phá Recipe (kết hợp đầy đủ phương thức của cả nhóm).
 */
export const recipeService = {
  getRecipes,
  getRecipeBySlug,

  // TV3: CRUD & Steps & Ingredients & Media
  async createDraft(request: CreateRecipeDraftRequest): Promise<RecipeDto> {
    return (await apiClient.post<RecipeDto>("/recipes", request)).data;
  },

  async update(id: string, request: UpdateRecipeRequest): Promise<RecipeDto> {
    return (await apiClient.put<RecipeDto>(recipePath(id), request)).data;
  },

  async delete(id: string): Promise<void> {
    await apiClient.delete<void>(recipePath(id));
  },

  async deleteRecipe(id: string): Promise<void> {
    await apiClient.delete<void>(recipePath(id));
  },

  // TV4 & TV2: Publish / Archive / Unarchive
  async publishRecipe(id: string): Promise<void> {
    await apiClient.put<void>(`${recipePath(id)}/publish`);
  },

  async unpublishRecipe(id: string): Promise<void> {
    await apiClient.post<void>(`${recipePath(id)}/unpublish`);
  },

  async archiveRecipe(id: string): Promise<void> {
    await apiClient.post<void>(`${recipePath(id)}/archive`);
  },

  async unarchiveRecipe(id: string): Promise<void> {
    await apiClient.post<void>(`${recipePath(id)}/unarchive`);
  },

  // Steps
  async addStep(id: string, request: AddRecipeStepRequest): Promise<RecipeStepResponseDto> {
    return (await apiClient.post<RecipeStepResponseDto>(`${recipePath(id)}/steps`, request)).data;
  },

  async updateStep(
    id: string,
    stepId: string,
    request: UpdateRecipeStepRequest,
  ): Promise<RecipeStepResponseDto> {
    return (
      await apiClient.put<RecipeStepResponseDto>(
        `${recipePath(id)}/steps/${encodeURIComponent(stepId)}`,
        request,
      )
    ).data;
  },

  async deleteStep(id: string, stepId: string): Promise<void> {
    await apiClient.delete<void>(`${recipePath(id)}/steps/${encodeURIComponent(stepId)}`);
  },

  // Ingredients
  async addIngredient(
    id: string,
    request: AddIngredientRequest,
  ): Promise<RecipeIngredientResponseDto> {
    return (await apiClient.post<RecipeIngredientResponseDto>(`${recipePath(id)}/ingredients`, request)).data;
  },

  async updateIngredient(
    id: string,
    ingredientId: string,
    request: UpdateIngredientRequest,
  ): Promise<RecipeIngredientResponseDto> {
    return (
      await apiClient.put<RecipeIngredientResponseDto>(
        `${recipePath(id)}/ingredients/${encodeURIComponent(ingredientId)}`,
        request,
      )
    ).data;
  },

  async deleteIngredient(id: string, ingredientId: string): Promise<void> {
    await apiClient.delete<void>(`${recipePath(id)}/ingredients/${encodeURIComponent(ingredientId)}`);
  },

  // Images
  async uploadImage(id: string, file: File): Promise<RecipeImageResponseDto> {
    const data = new FormData();
    data.append("file", file);
    return (
      await apiClient.post<RecipeImageResponseDto>(`${recipePath(id)}/images`, data, {
        headers: { "Content-Type": undefined },
      })
    ).data;
  },

  async uploadRecipeImage(recipeId: string, file: File): Promise<RecipeImageResponseDto> {
    const data = new FormData();
    data.append("file", file);
    return (
      await apiClient.post<RecipeImageResponseDto>(`${recipePath(recipeId)}/images`, data, {
        headers: { "Content-Type": undefined },
      })
    ).data;
  },

  async deleteImage(id: string, imageId: string): Promise<void> {
    await apiClient.delete<void>(`${recipePath(id)}/images/${encodeURIComponent(imageId)}`);
  },
};

export default recipeService;
