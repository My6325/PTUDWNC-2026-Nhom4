import type { RecipeSearchParams } from "../types/recipe";

const MAX_OFFSET = 2_147_483_647;

export function normalizeRecipePagination(pageIndex: unknown, pageSize: unknown = 12) {
  const size = Number(pageSize);
  const safeSize = Number.isSafeInteger(size) && size > 0 ? Math.min(size, 50) : 12;
  const page = Number(pageIndex);
  const maxPage = Math.floor(MAX_OFFSET / safeSize) + 1;
  return {
    pageIndex: Number.isSafeInteger(page) && page >= 1 && page <= maxPage ? page : 1,
    pageSize: safeSize,
  };
}

export function buildRecipeQuery(params: RecipeSearchParams = {}) {
  const term = (params.searchTerm ?? params.SearchTerm)?.trim();
  const requestedSort = params.sortBy ?? params.SortBy;
  const sort = requestedSort === "cookTime" || (requestedSort === "relevance" && term)
    ? requestedSort : "newest";
  const pagination = normalizeRecipePagination(
    params.pageIndex ?? params.PageIndex, params.pageSize ?? params.PageSize ?? 10,
  );
  const query: Record<string, string | number> = {
    SortBy: sort, PageIndex: pagination.pageIndex, PageSize: pagination.pageSize,
  };
  if (term) query.SearchTerm = term;
  const category = params.categoryId ?? params.CategoryId;
  if (category) query.CategoryId = category;
  const difficulty = params.difficulty ?? params.Difficulty;
  if (difficulty) query.Difficulty = difficulty;

  for (const [key, value] of Object.entries({
    MinCookTimeMinutes: params.minCookTimeMinutes ?? params.MinCookTimeMinutes,
    MaxCookTimeMinutes: params.maxCookTimeMinutes ?? params.MaxCookTimeMinutes,
    MinServings: params.minServings ?? params.MinServings,
    MaxServings: params.maxServings ?? params.MaxServings,
  })) {
    if (typeof value === "number" && Number.isSafeInteger(value)) query[key] = value;
  }
  return query;
}
