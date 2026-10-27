"use client";

import React, { Suspense, useEffect, useState, useCallback, useRef } from "react";
import { useSearchParams } from "next/navigation";
import {
  Search,
  Filter,
  SlidersHorizontal,
  X,
  RotateCcw,
  ChevronLeft,
  ChevronRight,
  CookingPot,
  Sparkles,
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Alert } from "@/components/ui/Alert";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { RecipeGrid } from "@/components/recipes/RecipeGrid";
import { recipeService } from "@/services/recipeService";
import { useRecipeCategories } from "@/hooks/useRecipeCategories";
import { RecipeListDto, GetRecipesParams } from "@/types/recipe";
import { PaginatedResult } from "@/types/common";
import { getErrorMessage } from "@/utils/apiError";
import { normalizeRecipePagination } from "@/utils/recipeQuery";

function RecipesContent() {
  const searchParams = useSearchParams();

  // Đọc params từ URL
  const currentSearchTerm = searchParams.get("SearchTerm") || "";
  const currentCategoryId = searchParams.get("CategoryId") || "";
  const currentDifficulty = searchParams.get("Difficulty") || "";
  const currentSortBy = searchParams.get("SortBy") || "newest";
  const currentPageIndex = normalizeRecipePagination(searchParams.get("PageIndex"), 12).pageIndex;
  const currentMinCookTime = searchParams.get("MinCookTimeMinutes") || "";
  const currentMaxCookTime = searchParams.get("MaxCookTimeMinutes") || "";
  const currentMinServings = searchParams.get("MinServings") || "";
  const currentMaxServings = searchParams.get("MaxServings") || "";

  // State cục bộ cho input tìm kiếm
  const [searchInput, setSearchInput] = useState(currentSearchTerm);
  const [isMobileFilterOpen, setIsMobileFilterOpen] = useState(false);

  // Data states
  const { categories, isLoading: categoriesLoading, error: categoriesError, retry: retryCategories } = useRecipeCategories();
  const [paginatedData, setPaginatedData] = useState<PaginatedResult<RecipeListDto>>({
    items: [],
    totalCount: 0,
    pageIndex: 1,
    pageSize: 12,
    totalPages: 0,
  });
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const activeRequest = useRef<AbortController | null>(null);

  // Cập nhật URL search params
  const updateFilters = useCallback(
    (newParams: Record<string, string | number | undefined | null>) => {
      // Read the latest URL so rapid filter changes do not overwrite each other.
      const params = new URLSearchParams(window.location.search);

      Object.entries(newParams).forEach(([key, value]) => {
        if (value === undefined || value === null || value === "") {
          params.delete(key);
        } else {
          params.set(key, String(value));
        }
      });

      // Nếu thay đổi bất kỳ bộ lọc nào khác PageIndex, reset về trang 1
      if (!("PageIndex" in newParams)) {
        params.delete("PageIndex");
      }

      window.history.pushState(null, "", `/recipes?${params.toString()}`);
    },
    []
  );

  // Đồng bộ searchInput khi URL thay đổi
  useEffect(() => {
    setSearchInput(currentSearchTerm);
  }, [currentSearchTerm]);

  // Nạp công thức khi searchParams thay đổi
  const fetchRecipes = useCallback(async () => {
    activeRequest.current?.abort();
    const controller = new AbortController();
    activeRequest.current = controller;
    setIsLoading(true);
    setError(null);

    const query: GetRecipesParams = {
      searchTerm: currentSearchTerm || undefined,
      categoryId: currentCategoryId || undefined,
      difficulty: currentDifficulty || undefined,
      sortBy: currentSortBy || "newest",
      pageIndex: currentPageIndex > 0 ? currentPageIndex : 1,
      pageSize: 12,
    };

    if (currentMinCookTime) query.minCookTimeMinutes = parseInt(currentMinCookTime, 10);
    if (currentMaxCookTime) query.maxCookTimeMinutes = parseInt(currentMaxCookTime, 10);
    if (currentMinServings) query.minServings = parseInt(currentMinServings, 10);
    if (currentMaxServings) query.maxServings = parseInt(currentMaxServings, 10);

    try {
      const result = await recipeService.getRecipes(query, controller.signal);
      if (!controller.signal.aborted) setPaginatedData(result);
    } catch (err) {
      if (controller.signal.aborted) return;
      console.error("Lỗi khi tìm kiếm công thức:", err);
      setError(getErrorMessage(err, "Không thể tải danh sách công thức. Vui lòng thử lại."));
    } finally {
      if (!controller.signal.aborted) setIsLoading(false);
    }
  }, [
    currentSearchTerm,
    currentCategoryId,
    currentDifficulty,
    currentSortBy,
    currentPageIndex,
    currentMinCookTime,
    currentMaxCookTime,
    currentMinServings,
    currentMaxServings,
  ]);

  useEffect(() => {
    void fetchRecipes();
    return () => activeRequest.current?.abort();
  }, [fetchRecipes]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateFilters({ SearchTerm: searchInput.trim() });
  };

  const handleResetFilters = () => {
    setSearchInput("");
    window.history.pushState(null, "", "/recipes");
  };

  const hasActiveFilters = Boolean(
    currentSearchTerm ||
    currentCategoryId ||
    currentDifficulty ||
    currentMinCookTime ||
    currentMaxCookTime ||
    currentMinServings ||
    currentMaxServings ||
    (currentSortBy && currentSortBy !== "newest")
  );

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 md:py-12 space-y-8">
      {/* 1. Header Trang & Thanh tìm kiếm chính */}
      <div className="space-y-4">
        <div className="flex flex-col md:flex-row md:items-end justify-between gap-4">
          <div>
            <h1 className="text-3xl sm:text-4xl font-extrabold text-stone-900 dark:text-white tracking-tight">
              Khám phá Công thức
            </h1>
            <p className="text-sm sm:text-base text-stone-500 dark:text-stone-400 mt-1">
              Tìm kiếm theo món ăn, danh mục hoặc lọc theo độ khó và thời gian nấu
            </p>
          </div>

          {/* Nút bật/tắt bộ lọc trên mobile */}
          <div className="flex items-center gap-2 lg:hidden">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsMobileFilterOpen(!isMobileFilterOpen)}
              leftIcon={<SlidersHorizontal className="w-4 h-4" />}
            >
              Bộ lọc {hasActiveFilters && "•"}
            </Button>
            {hasActiveFilters && (
              <Button variant="ghost" size="sm" onClick={handleResetFilters} className="text-xs text-stone-500">
                Xóa lọc
              </Button>
            )}
          </div>
        </div>

        {/* Ô tìm kiếm dạng Bar */}
        <form onSubmit={handleSearchSubmit} className="relative w-full" role="search">
          <input
            type="search"
            placeholder="Tìm theo tên món ăn hoặc từ khóa..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            className="w-full pl-11 pr-28 py-3.5 text-sm md:text-base bg-white dark:bg-stone-900 border border-stone-300 dark:border-stone-700 rounded-2xl focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none shadow-sm text-stone-900 dark:text-stone-100 placeholder:text-stone-400 transition-all"
            aria-label="Tìm kiếm theo tên món ăn"
          />
          <Search className="absolute left-4 top-4 w-5 h-5 text-stone-400 pointer-events-none" aria-hidden="true" />
          <div className="absolute right-2 top-2 flex items-center gap-1.5">
            {searchInput && (
              <button
                type="button"
                onClick={() => {
                  setSearchInput("");
                  updateFilters({ SearchTerm: "" });
                }}
                className="p-1.5 text-stone-400 hover:text-stone-600 rounded-lg"
                title="Xóa từ khóa"
              >
                <X className="w-4 h-4" />
              </button>
            )}
            <Button type="submit" variant="primary" size="sm" className="rounded-xl px-4">
              Tìm
            </Button>
          </div>
        </form>
      </div>

      {/* 2. Khung Bố cục: Cột Bộ lọc bên trái + Lưới công thức bên phải */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 items-start">
        {/* Bộ lọc bên trái (Desktop Sidebar & Mobile Drawer) */}
        <aside
          className={`lg:block ${
            isMobileFilterOpen
              ? "fixed inset-0 z-50 p-6 bg-white dark:bg-stone-900 overflow-y-auto"
              : "hidden"
          } lg:relative lg:p-6 lg:rounded-3xl lg:border lg:border-stone-200/80 dark:lg:border-stone-800 lg:bg-white dark:lg:bg-stone-900/60 lg:shadow-sm space-y-6`}
        >
          <div className="flex items-center justify-between pb-3 border-b border-stone-200 dark:border-stone-800">
            <h3 className="font-bold text-base text-stone-900 dark:text-white flex items-center gap-2">
              <Filter className="w-4 h-4 text-brand-600" /> Bộ lọc tìm kiếm
            </h3>
            <div className="flex items-center gap-2">
              {hasActiveFilters && (
                <button
                  type="button"
                  onClick={handleResetFilters}
                  className="text-xs text-brand-600 dark:text-brand-400 hover:underline flex items-center gap-1"
                >
                  <RotateCcw className="w-3 h-3" /> Đặt lại
                </button>
              )}
              {isMobileFilterOpen && (
                <button
                  type="button"
                  onClick={() => setIsMobileFilterOpen(false)}
                  className="p-1.5 text-stone-400 hover:text-stone-600 rounded-lg lg:hidden"
                  aria-label="Đóng bộ lọc"
                >
                  <X className="w-5 h-5" />
                </button>
              )}
            </div>
          </div>

          {/* Sắp xếp (SortBy) */}
          <div className="space-y-2">
            <label htmlFor="sort-select" className="text-xs font-bold uppercase tracking-wider text-stone-500 dark:text-stone-400">
              Sắp xếp theo
            </label>
            <select
              id="sort-select"
              value={currentSortBy}
              onChange={(e) => updateFilters({ SortBy: e.target.value })}
              className="w-full p-2.5 text-sm bg-stone-50 dark:bg-stone-800 border border-stone-300 dark:border-stone-700 rounded-xl focus:border-brand-500 focus:outline-none text-stone-900 dark:text-stone-100"
            >
              <option value="newest">Mới nhất</option>
              <option value="cookTime">Thời gian nấu ngắn nhất</option>
              {currentSearchTerm && <option value="relevance">Độ liên quan nhất</option>}
            </select>
          </div>

          {/* Danh mục (CategoryId - Gửi GUID) */}
          <div className="space-y-2">
            <label htmlFor="cat-select" className="text-xs font-bold uppercase tracking-wider text-stone-500 dark:text-stone-400">
              Danh mục món ăn
            </label>
            <select
              id="cat-select"
              disabled={categoriesLoading || !!categoriesError}
              value={currentCategoryId}
              onChange={(e) => updateFilters({ CategoryId: e.target.value })}
              className="w-full p-2.5 text-sm bg-stone-50 dark:bg-stone-800 border border-stone-300 dark:border-stone-700 rounded-xl focus:border-brand-500 focus:outline-none text-stone-900 dark:text-stone-100"
            >
              <option value="">{categoriesLoading ? "Đang tải danh mục..." : categoriesError ? "Không tải được danh mục" : "Tất cả danh mục"}</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
            {categoriesError && (
              <div role="alert" className="space-y-2 text-sm text-red-700 dark:text-red-300">
                <p>{categoriesError}</p>
                <Button variant="outline" size="sm" onClick={retryCategories}>Thử lại danh mục</Button>
              </div>
            )}
          </div>

          {/* Độ khó (Difficulty) */}
          <div className="space-y-2">
            <span className="text-xs font-bold uppercase tracking-wider text-stone-500 dark:text-stone-400 block">
              Mức độ khó
            </span>
            <div className="flex flex-wrap gap-1.5">
              {[
                { label: "Tất cả", value: "" },
                { label: "Dễ", value: "Easy" },
                { label: "Trung bình", value: "Medium" },
                { label: "Khó", value: "Hard" },
              ].map((diff) => (
                <button
                  key={diff.value}
                  type="button"
                  onClick={() => updateFilters({ Difficulty: diff.value })}
                  className={`px-3 py-1.5 text-xs font-medium rounded-lg border transition-all ${
                    currentDifficulty === diff.value
                      ? "bg-brand-600 text-white border-brand-600 shadow-sm"
                      : "bg-white dark:bg-stone-800 text-stone-700 dark:text-stone-300 border-stone-200 dark:border-stone-700 hover:border-stone-300"
                  }`}
                >
                  {diff.label}
                </button>
              ))}
            </div>
          </div>

          {/* Thời gian nấu (Cook Time) */}
          <div className="space-y-2">
            <span className="text-xs font-bold uppercase tracking-wider text-stone-500 dark:text-stone-400 block">
              Thời gian nấu (phút)
            </span>
            <div className="grid grid-cols-3 gap-1.5 text-xs">
              <button
                type="button"
                onClick={() => updateFilters({ MinCookTimeMinutes: "", MaxCookTimeMinutes: 30 })}
                className={`p-2 rounded-lg border text-center font-medium transition-all ${
                  currentMaxCookTime === "30" && !currentMinCookTime
                    ? "bg-brand-600 text-white border-brand-600"
                    : "bg-white dark:bg-stone-800 text-stone-700 dark:text-stone-300 border-stone-200 dark:border-stone-700"
                }`}
              >
                &lt; 30p
              </button>
              <button
                type="button"
                onClick={() => updateFilters({ MinCookTimeMinutes: 30, MaxCookTimeMinutes: 60 })}
                className={`p-2 rounded-lg border text-center font-medium transition-all ${
                  currentMinCookTime === "30" && currentMaxCookTime === "60"
                    ? "bg-brand-600 text-white border-brand-600"
                    : "bg-white dark:bg-stone-800 text-stone-700 dark:text-stone-300 border-stone-200 dark:border-stone-700"
                }`}
              >
                30-60p
              </button>
              <button
                type="button"
                onClick={() => updateFilters({ MinCookTimeMinutes: 60, MaxCookTimeMinutes: "" })}
                className={`p-2 rounded-lg border text-center font-medium transition-all ${
                  currentMinCookTime === "60" && !currentMaxCookTime
                    ? "bg-brand-600 text-white border-brand-600"
                    : "bg-white dark:bg-stone-800 text-stone-700 dark:text-stone-300 border-stone-200 dark:border-stone-700"
                }`}
              >
                &gt; 60p
              </button>
            </div>
          </div>

          {/* Khẩu phần (Servings) */}
          <div className="space-y-2">
            <span className="text-xs font-bold uppercase tracking-wider text-stone-500 dark:text-stone-400 block">
              Khẩu phần ăn
            </span>
            <div className="grid grid-cols-3 gap-1.5 text-xs">
              <button
                type="button"
                onClick={() => updateFilters({ MinServings: 1, MaxServings: 2 })}
                className={`p-2 rounded-lg border text-center font-medium transition-all ${
                  currentMinServings === "1" && currentMaxServings === "2"
                    ? "bg-brand-600 text-white border-brand-600"
                    : "bg-white dark:bg-stone-800 text-stone-700 dark:text-stone-300 border-stone-200 dark:border-stone-700"
                }`}
              >
                1-2 người
              </button>
              <button
                type="button"
                onClick={() => updateFilters({ MinServings: 3, MaxServings: 4 })}
                className={`p-2 rounded-lg border text-center font-medium transition-all ${
                  currentMinServings === "3" && currentMaxServings === "4"
                    ? "bg-brand-600 text-white border-brand-600"
                    : "bg-white dark:bg-stone-800 text-stone-700 dark:text-stone-300 border-stone-200 dark:border-stone-700"
                }`}
              >
                3-4 người
              </button>
              <button
                type="button"
                onClick={() => updateFilters({ MinServings: 5, MaxServings: "" })}
                className={`p-2 rounded-lg border text-center font-medium transition-all ${
                  currentMinServings === "5" && !currentMaxServings
                    ? "bg-brand-600 text-white border-brand-600"
                    : "bg-white dark:bg-stone-800 text-stone-700 dark:text-stone-300 border-stone-200 dark:border-stone-700"
                }`}
              >
                &gt; 4 người
              </button>
            </div>
          </div>

          {/* Nút đóng drawer trên mobile */}
          {isMobileFilterOpen && (
            <div className="pt-4 lg:hidden">
              <Button
                variant="primary"
                size="md"
                onClick={() => setIsMobileFilterOpen(false)}
                className="w-full"
              >
                Áp dụng bộ lọc
              </Button>
            </div>
          )}
        </aside>

        {/* Nội dung chính: Lưới công thức + Phân trang */}
        <main className="lg:col-span-3 space-y-6">
          {/* Thanh hiển thị kết quả và bộ lọc đang kích hoạt */}
          <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-stone-600 dark:text-stone-400 pb-2 border-b border-stone-200/80 dark:border-stone-800">
            <div>
              {isLoading ? (
                <span>Đang tìm kiếm công thức...</span>
              ) : error ? (
                <span>Chưa tải được kết quả</span>
              ) : (
                <span>
                  Tìm thấy <strong className="text-stone-900 dark:text-white">{paginatedData.totalCount}</strong> công thức
                  {currentSearchTerm && <span> cho từ khóa &quot;{currentSearchTerm}&quot;</span>}
                </span>
              )}
            </div>

            {hasActiveFilters && (
              <Button variant="ghost" size="sm" onClick={handleResetFilters} className="text-xs text-brand-600">
                Xóa tất cả bộ lọc
              </Button>
            )}
          </div>

          {/* Báo lỗi nếu có */}
          {error && (
            <Alert variant="error" title="Không thể tải danh sách công thức">
              <div className="flex items-center justify-between gap-3">
                <span>{error}</span>
                <Button size="sm" variant="outline" onClick={fetchRecipes}>
                  Thử lại
                </Button>
              </div>
            </Alert>
          )}

          {/* Lưới công thức */}
          {!error && <RecipeGrid
            recipes={paginatedData.items}
            isLoading={isLoading}
            skeletonCount={12}
            className="grid-cols-1 sm:grid-cols-2 lg:grid-cols-3"
            emptyState={
              <EmptyState
                icon={<CookingPot className="w-8 h-8 text-brand-600" />}
                title="Không tìm thấy món ăn phù hợp"
                description="Hãy thử thay đổi từ khóa tìm kiếm, giảm bớt điều kiện lọc hoặc khám phá các danh mục khác."
                action={
                  <Button variant="outline" size="sm" onClick={handleResetFilters}>
                    Xóa tất cả bộ lọc
                  </Button>
                }
              />
            }
          />}

          {/* Phân trang dùng chung (Pagination Component - Tiêu chí 5 Lab 6) */}
          {!error && <Pagination
            currentPage={paginatedData.pageIndex}
            totalPages={paginatedData.totalPages}
            onPageChange={(page) => updateFilters({ PageIndex: page })}
            isLoading={isLoading}
          />}
        </main>
      </div>
    </div>
  );
}

export default function RecipesPage() {
  return (
    <Suspense
      fallback={
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 text-center text-stone-500">
          Đang nạp dữ liệu công thức...
        </div>
      }
    >
      <RecipesContent />
    </Suspense>
  );
}
