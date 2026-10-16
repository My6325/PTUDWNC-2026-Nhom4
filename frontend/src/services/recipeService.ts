import { apiClient } from "./apiClient";
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
): Promise<PaginatedResult<RecipeListDto>> {
  const queryParams: Record<string, unknown> = {};

  if (params) {
    const term = params.searchTerm ?? params.SearchTerm;
    if (term?.trim()) {
      queryParams.SearchTerm = term.trim();
    }

    const catId = params.categoryId ?? params.CategoryId;
    if (catId) {
      queryParams.CategoryId = catId;
    }

    const diff = params.difficulty ?? params.Difficulty;
    if (diff) {
      queryParams.Difficulty = diff;
    }

    const minCook = params.minCookTimeMinutes ?? params.MinCookTimeMinutes;
    if (typeof minCook === "number") {
      queryParams.MinCookTimeMinutes = minCook;
    }

    const maxCook = params.maxCookTimeMinutes ?? params.MaxCookTimeMinutes;
    if (typeof maxCook === "number") {
      queryParams.MaxCookTimeMinutes = maxCook;
    }

    const minServ = params.minServings ?? params.MinServings;
    if (typeof minServ === "number") {
      queryParams.MinServings = minServ;
    }

    const maxServ = params.maxServings ?? params.MaxServings;
    if (typeof maxServ === "number") {
      queryParams.MaxServings = maxServ;
    }

    const sort = params.sortBy ?? params.SortBy;
    if (sort === "relevance" && !queryParams.SearchTerm) {
      queryParams.SortBy = "newest";
    } else if (sort) {
      queryParams.SortBy = sort;
    } else {
      queryParams.SortBy = "newest";
    }

    const pageIdx = params.pageIndex ?? params.PageIndex;
    if (typeof pageIdx === "number" && pageIdx > 0) {
      queryParams.PageIndex = pageIdx;
    }

    const pageSize = params.pageSize ?? params.PageSize;
    if (typeof pageSize === "number" && pageSize > 0) {
      queryParams.PageSize = pageSize;
    }
  }

  const response = await apiClient.get<PaginatedResult<RecipeListDto>>("/recipes", {
    params: queryParams,
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
