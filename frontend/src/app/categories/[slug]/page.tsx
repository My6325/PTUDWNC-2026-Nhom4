"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import {
  ChevronRight,
  ArrowLeft,
  Clock,
  Users,
  Utensils,
  BookOpen,
  FolderOpen,
  RefreshCw,
  Sparkles,
} from "lucide-react";
import { categoryService } from "@/services/categoryService";
import { CategoryDetailDto, CategoryRecipeSummaryDto } from "@/types/category";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Skeleton } from "@/components/ui/Skeleton";
import { Alert } from "@/components/ui/Alert";
import { EmptyState } from "@/components/ui/EmptyState";
import { extractApiErrorMessage } from "@/utils/apiError";
import { cn } from "@/utils/cn";

/**
 * Card hiển thị món ăn tóm tắt trong danh mục (CategoryRecipeSummaryDto)
 */
const CategoryRecipeCard: React.FC<{ recipe: CategoryRecipeSummaryDto }> = ({
  recipe,
}) => {
  const totalMinutes = (recipe.prepTimeMinutes || 0) + (recipe.cookTimeMinutes || 0);

  return (
    <article className="group flex flex-col overflow-hidden rounded-2xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 shadow-xs hover:shadow-md hover:border-brand-300 dark:hover:border-brand-700/60 transition-all duration-300 hover:-translate-y-1">
      {/* Khung ảnh món ăn */}
      <Link
        href={`/recipes/${recipe.slug}`}
        className="relative aspect-[4/3] w-full overflow-hidden bg-stone-100 dark:bg-stone-800 block"
        tabIndex={-1}
      >
        {recipe.primaryImageUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={recipe.primaryImageUrl}
            alt={recipe.title}
            className="h-full w-full object-cover transition-transform duration-500 ease-out group-hover:scale-105"
            loading="lazy"
          />
        ) : (
          <div className="flex h-full w-full flex-col items-center justify-center bg-stone-100 dark:bg-stone-800 text-stone-400">
            <Utensils className="h-10 w-10 stroke-[1.4] mb-1" />
            <span className="text-xs font-semibold">Culinary Blog</span>
          </div>
        )}

        {/* Badge Độ khó ở góc trên ảnh */}
        <div className="absolute top-3 right-3">
          <Badge
            difficulty={recipe.difficulty as "Easy" | "Medium" | "Hard"}
            size="sm"
            className="backdrop-blur-md bg-white/90 dark:bg-stone-900/90 shadow-xs"
          />
        </div>
      </Link>

      {/* Thông tin bài viết */}
      <div className="flex flex-1 flex-col p-5">
        <h3 className="text-base sm:text-lg font-bold text-stone-900 dark:text-stone-100 line-clamp-2 leading-snug group-hover:text-brand-600 dark:group-hover:text-brand-400 transition-colors">
          <Link
            href={`/recipes/${recipe.slug}`}
            className="focus-visible:outline-none focus-visible:underline"
          >
            {recipe.title}
          </Link>
        </h3>

        {recipe.description && (
          <p className="mt-2 text-xs sm:text-sm text-stone-600 dark:text-stone-400 line-clamp-2 leading-relaxed">
            {recipe.description}
          </p>
        )}

        {/* Meta thông số (Thời gian nấu & Khẩu phần) */}
        <div className="mt-auto pt-4 flex items-center justify-between text-xs text-stone-500 dark:text-stone-400 border-t border-stone-100 dark:border-stone-800/80">
          <span className="inline-flex items-center gap-1 font-medium">
            <Clock className="h-3.5 w-3.5 text-stone-400" />
            <span>
              {totalMinutes > 0 ? `${totalMinutes} phút` : `${recipe.cookTimeMinutes} phút`}
            </span>
          </span>

          {recipe.servings > 0 && (
            <span className="inline-flex items-center gap-1 font-medium">
              <Users className="h-3.5 w-3.5 text-stone-400" />
              <span>{recipe.servings} phần</span>
            </span>
          )}
        </div>
      </div>
    </article>
  );
};

export default function CategoryDetailPage() {
  const params = useParams();
  const slug = typeof params?.slug === "string" ? params.slug : "";

  const [category, setCategory] = useState<CategoryDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [isNotFound, setIsNotFound] = useState<boolean>(false);

  useEffect(() => {
    if (!slug) return;

    let isMounted = true;
    const fetchCategory = async () => {
      setIsLoading(true);
      setError(null);
      setIsNotFound(false);
      try {
        const data = await categoryService.getCategoryBySlug(slug);
        if (isMounted) {
          setCategory(data);
        }
      } catch (err: unknown) {
        if (!isMounted) return;
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 404) {
          setIsNotFound(true);
        } else {
          setError(
            extractApiErrorMessage(
              err,
              "Không thể tải thông tin danh mục ẩm thực. Vui lòng thử lại."
            )
          );
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    };

    fetchCategory();

    return () => {
      isMounted = false;
    };
  }, [slug]);

  // Trạng thái Đang tải (Loading Skeleton)
  if (isLoading) {
    return (
      <div className="min-h-screen bg-surface-warm dark:bg-stone-950/50 py-8">
        <div className="container-custom mx-auto px-4 space-y-6">
          <Skeleton className="h-6 w-48 rounded-md" />
          <div className="rounded-3xl bg-white dark:bg-stone-900 p-8 space-y-4 border border-stone-200 dark:border-stone-800">
            <Skeleton className="h-10 w-1/3 rounded-lg" />
            <Skeleton className="h-5 w-2/3 rounded-md" />
            <Skeleton className="h-6 w-28 rounded-full" />
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6 pt-4">
            {Array.from({ length: 4 }).map((_, idx) => (
              <div
                key={idx}
                className="rounded-2xl border border-stone-200/80 dark:border-stone-800 bg-white dark:bg-stone-900 p-4 space-y-3"
              >
                <Skeleton className="aspect-[4/3] w-full rounded-xl" />
                <Skeleton className="h-6 w-3/4 rounded-md" />
                <Skeleton className="h-4 w-1/2 rounded-md" />
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  // Trạng thái Không tìm thấy (404)
  if (isNotFound) {
    return (
      <div className="min-h-[70vh] flex items-center justify-center py-16 px-4">
        <div className="max-w-md w-full text-center space-y-5">
          <div className="w-16 h-16 mx-auto rounded-full bg-amber-100 dark:bg-amber-900/40 flex items-center justify-center text-amber-600 dark:text-amber-400">
            <FolderOpen className="w-8 h-8" />
          </div>
          <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-100">
            Không tìm thấy danh mục
          </h1>
          <p className="text-sm text-stone-600 dark:text-stone-400">
            Danh mục ẩm thực bạn tìm kiếm không tồn tại hoặc đã thay đổi đường dẫn liên kết.
          </p>
          <div className="flex justify-center gap-3 pt-2">
            <Button
              variant="primary"
              href="/categories"
              leftIcon={<ArrowLeft className="w-4 h-4" />}
            >
              Xem tất cả danh mục
            </Button>
            <Button variant="outline" href="/">
              Về trang chủ
            </Button>
          </div>
        </div>
      </div>
    );
  }

  // Trạng thái Lỗi API
  if (error || !category) {
    return (
      <div className="container-custom max-w-2xl mx-auto py-16 px-4">
        <Alert
          variant="error"
          title="Không thể tải danh mục"
          className="mb-6"
        >
          {error || "Đã xảy ra lỗi không xác định."}
        </Alert>
        <div className="flex justify-center gap-3">
          <Button variant="primary" onClick={() => window.location.reload()}>
            Tải lại trang
          </Button>
          <Button variant="outline" href="/categories">
            Quay lại danh sách
          </Button>
        </div>
      </div>
    );
  }

  const recipeCount = category.recipes?.length || 0;

  return (
    <div className="min-h-screen bg-surface-warm dark:bg-stone-950/50 pb-20">
      {/* Breadcrumb Navigation */}
      <div className="border-b border-surface-border dark:border-stone-800 bg-white dark:bg-stone-900/80">
        <div className="container-custom mx-auto px-4 py-3">
          <nav className="flex items-center gap-1.5 text-xs text-stone-500 dark:text-stone-400 overflow-x-auto whitespace-nowrap">
            <Link
              href="/"
              className="hover:text-stone-900 dark:hover:text-stone-100 transition-colors"
            >
              Trang chủ
            </Link>
            <ChevronRight className="h-3 w-3 shrink-0" />
            <Link
              href="/categories"
              className="hover:text-stone-900 dark:hover:text-stone-100 transition-colors"
            >
              Danh mục
            </Link>
            <ChevronRight className="h-3 w-3 shrink-0" />
            <span className="text-stone-800 dark:text-stone-200 font-medium">
              {category.name}
            </span>
          </nav>
        </div>
      </div>

      <div className="container-custom mx-auto px-4 pt-8">
        {/* Banner Danh mục (Category Header Hero) */}
        <div className="relative overflow-hidden rounded-3xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 shadow-sm p-6 sm:p-10 mb-10">
          <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
            <div className="max-w-2xl space-y-3">
              <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-brand-50 dark:bg-brand-950/60 text-brand-700 dark:text-brand-300 text-xs font-bold border border-brand-200 dark:border-brand-800/60">
                <Sparkles className="h-3.5 w-3.5" />
                <span>Danh mục ẩm thực</span>
              </div>

              <h1 className="text-3xl sm:text-4xl lg:text-5xl font-extrabold text-stone-900 dark:text-stone-50 tracking-tight">
                {category.name}
              </h1>

              {category.description ? (
                <p className="text-stone-600 dark:text-stone-300 text-base sm:text-lg leading-relaxed">
                  {category.description}
                </p>
              ) : (
                <p className="text-stone-500 dark:text-stone-400 text-sm italic">
                  Tổng hợp các công thức nấu ăn đặc trưng và hấp dẫn nhất thuộc chủ đề {category.name}.
                </p>
              )}

              <div className="flex items-center gap-3 pt-2">
                <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-lg bg-stone-100 dark:bg-stone-800 text-stone-700 dark:text-stone-300 text-xs font-bold">
                  <BookOpen className="h-3.5 w-3.5 text-brand-600 dark:text-brand-400" />
                  {recipeCount} công thức
                </span>
              </div>
            </div>

            {/* Ảnh đại diện danh mục nếu có */}
            {category.imageUrl && (
              <div className="w-full md:w-64 h-44 shrink-0 rounded-2xl overflow-hidden bg-stone-100 dark:bg-stone-800 border border-stone-200 dark:border-stone-700">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={category.imageUrl}
                  alt={category.name}
                  className="w-full h-full object-cover"
                />
              </div>
            )}
          </div>
        </div>

        {/* Danh sách công thức trong danh mục */}
        <div className="space-y-6">
          <div className="flex items-center justify-between pb-3 border-b border-stone-200 dark:border-stone-800">
            <h2 className="text-xl sm:text-2xl font-bold text-stone-900 dark:text-stone-100 flex items-center gap-2">
              <Utensils className="h-5 w-5 text-brand-600 dark:text-brand-400" />
              Công thức thuộc danh mục
            </h2>
            <span className="text-xs text-stone-500 dark:text-stone-400 font-medium">
              Hiển thị {recipeCount} món ngon
            </span>
          </div>

          {recipeCount > 0 ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
              {category.recipes.map((recipeItem) => (
                <CategoryRecipeCard key={recipeItem.id} recipe={recipeItem} />
              ))}
            </div>
          ) : (
            <div className="py-12">
              <EmptyState
                title="Chưa có công thức trong danh mục này"
                description={`Các đầu bếp đang trong quá trình chuẩn bị các công thức tuyệt hảo cho chủ đề ${category.name}. Bạn hãy quay lại sau nhé!`}
                icon={<Utensils className="h-10 w-10 text-stone-400" />}
                action={
                  <Button variant="primary" href="/recipes">
                    Khám phá các công thức khác
                  </Button>
                }
              />
            </div>
          )}
        </div>

        {/* Thanh điều hướng chân trang */}
        <div className="mt-14 pt-8 border-t border-stone-200/80 dark:border-stone-800 flex flex-wrap items-center justify-between gap-4">
          <Button
            variant="outline"
            href="/categories"
            leftIcon={<ArrowLeft className="h-4 w-4" />}
          >
            Tất cả danh mục
          </Button>

          <Button variant="primary" href="/recipes">
            Tìm kiếm & Lọc công thức
          </Button>
        </div>
      </div>
    </div>
  );
}
