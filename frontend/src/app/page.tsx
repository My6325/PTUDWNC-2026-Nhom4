"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  ChefHat,
  Search,
  Sparkles,
  ArrowRight,
  BookOpen,
  CookingPot,
  RefreshCw,
  UtensilsCrossed,
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Alert } from "@/components/ui/Alert";
import { Skeleton } from "@/components/ui/Skeleton";
import { RecipeGrid } from "@/components/recipes/RecipeGrid";
import { categoryService } from "@/services/categoryService";
import { recipeService } from "@/services/recipeService";
import { CategoryDto } from "@/types/category";
import { RecipeListDto } from "@/types/recipe";
import { getErrorMessage } from "@/utils/apiError";

export default function HomePage() {
  const router = useRouter();
  const [searchTerm, setSearchTerm] = useState("");

  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [isLoadingCategories, setIsLoadingCategories] = useState(true);

  const [recipes, setRecipes] = useState<RecipeListDto[]>([]);
  const [isLoadingRecipes, setIsLoadingRecipes] = useState(true);

  const [apiError, setApiError] = useState<string | null>(null);

  const fetchData = async () => {
    setApiError(null);

    // 1. Nạp danh sách danh mục (FR-CAT-001)
    setIsLoadingCategories(true);
    try {
      const catData = await categoryService.getCategories();
      setCategories(catData || []);
    } catch (err) {
      console.error("Lỗi khi tải danh mục:", err);
    } finally {
      setIsLoadingCategories(false);
    }

    // 2. Nạp công thức mới nhất (FR-RCP-001)
    setIsLoadingRecipes(true);
    try {
      const recipeData = await recipeService.getRecipes({
        sortBy: "newest",
        pageIndex: 1,
        pageSize: 8,
      });
      setRecipes(recipeData?.items || []);
    } catch (err) {
      console.error("Lỗi khi tải công thức:", err);
      setApiError(getErrorMessage(err, "Không thể tải danh sách công thức. Vui lòng kiểm tra kết nối API."));
    } finally {
      setIsLoadingRecipes(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleHeroSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchTerm.trim()) {
      router.push(`/recipes?SearchTerm=${encodeURIComponent(searchTerm.trim())}`);
    } else {
      router.push("/recipes");
    }
  };

  return (
    <div className="space-y-16 md:space-y-24 pb-20">
      {/* 1. Hero Section ấm cúng & Ô tìm kiếm nổi bật */}
      <section className="relative overflow-hidden bg-gradient-to-b from-brand-50/60 via-surface-warm to-background dark:from-stone-900/40 dark:via-background dark:to-background pt-12 md:pt-20 pb-16 px-4 sm:px-6 lg:px-8 border-b border-stone-200/60 dark:border-stone-800/60">
        <div className="max-w-4xl mx-auto text-center space-y-6">
          <div className="inline-flex items-center gap-2">
            <Badge variant="brand" size="md" className="shadow-sm">
              <Sparkles className="w-3.5 h-3.5 mr-1 text-brand-600" />
              Nền tảng Ẩm thực & Nấu ăn Gia đình
            </Badge>
          </div>

          <h1 className="text-4xl sm:text-5xl md:text-6xl font-black tracking-tight text-stone-900 dark:text-white leading-[1.15]">
            Khám phá hương vị mới, <br />
            <span className="text-brand-600 dark:text-brand-400">nấu ăn trọn niềm vui</span>
          </h1>

          <p className="text-base sm:text-lg text-stone-600 dark:text-stone-300 max-w-2xl mx-auto leading-relaxed">
            Hàng ngàn công thức nấu ăn chuẩn vị từ các đầu bếp và người yêu ẩm thực. Hướng dẫn chi tiết từng bước, định lượng nguyên liệu chuẩn xác và đầy đủ giá trị dinh dưỡng.
          </p>

          {/* Ô tìm kiếm trung tâm */}
          <form onSubmit={handleHeroSearch} className="max-w-2xl mx-auto pt-2" role="search">
            <div className="relative flex items-center shadow-lg shadow-brand-500/10 rounded-2xl overflow-hidden border border-stone-200 dark:border-stone-700 bg-white dark:bg-stone-900 focus-within:border-brand-500 focus-within:ring-2 focus-within:ring-brand-500/20 transition-all p-1.5">
              <div className="pl-3 text-stone-400">
                <Search className="w-5 h-5" aria-hidden="true" />
              </div>
              <input
                type="search"
                placeholder="Bạn muốn nấu món gì hôm nay? (Ví dụ: Phở bò, Sườn xào, Canh chua...)"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full px-3 py-3 text-sm md:text-base bg-transparent text-stone-900 dark:text-stone-100 placeholder:text-stone-400 focus:outline-none"
                aria-label="Tìm kiếm món ăn hoặc nguyên liệu"
              />
              <Button type="submit" variant="primary" size="md" className="shrink-0 rounded-xl px-5">
                Tìm kiếm
              </Button>
            </div>
          </form>

          {/* Gợi ý từ khóa phổ biến */}
          <div className="flex flex-wrap items-center justify-center gap-2 pt-2 text-xs text-stone-500 dark:text-stone-400">
            <span className="font-semibold text-stone-700 dark:text-stone-300">Gợi ý tìm kiếm:</span>
            {["Món xào", "Món kho", "Canh thanh mát", "Bữa sáng nhanh", "Ăn vặt"].map((tag) => (
              <button
                key={tag}
                type="button"
                onClick={() => router.push(`/recipes?SearchTerm=${encodeURIComponent(tag)}`)}
                className="px-2.5 py-1 rounded-full bg-white dark:bg-stone-800 border border-stone-200 dark:border-stone-700 hover:border-brand-400 hover:text-brand-600 dark:hover:text-brand-400 transition-colors"
              >
                {tag}
              </button>
            ))}
          </div>
        </div>
      </section>

      {/* Thông báo lỗi nếu không kết nối được Backend */}
      {apiError && (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <Alert
            variant="warning"
            title="Thông báo kết nối API"
            icon={<CookingPot className="w-5 h-5 text-amber-600" />}
          >
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
              <span>{apiError}</span>
              <Button size="sm" variant="outline" onClick={fetchData} leftIcon={<RefreshCw className="w-3.5 h-3.5" />}>
                Thử lại
              </Button>
            </div>
          </Alert>
        </div>
      )}

      {/* 2. Danh mục Ẩm thực (Categories) */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-8">
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 border-b border-stone-200/80 dark:border-stone-800 pb-4">
          <div>
            <div className="flex items-center gap-2 text-brand-600 dark:text-brand-400 font-semibold text-xs tracking-wider uppercase">
              <BookOpen className="w-4 h-4" />
              <span>Chủ đề ẩm thực</span>
            </div>
            <h2 className="text-2xl sm:text-3xl font-extrabold text-stone-900 dark:text-white tracking-tight mt-1">
              Danh mục món ăn
            </h2>
          </div>
          <Link
            href="/categories"
            className="inline-flex items-center gap-1.5 text-sm font-semibold text-brand-600 dark:text-brand-400 hover:text-brand-700 hover:underline"
          >
            Xem tất cả danh mục <ArrowRight className="w-4 h-4" />
          </Link>
        </div>

        {/* Danh sách thẻ danh mục */}
        {isLoadingCategories ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
            {Array.from({ length: 6 }).map((_, idx) => (
              <div key={idx} className="p-4 rounded-2xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 space-y-2">
                <Skeleton className="w-10 h-10 rounded-xl" />
                <Skeleton className="h-4 w-3/4 rounded" />
              </div>
            ))}
          </div>
        ) : categories.length === 0 ? (
          <div className="text-center py-8 text-stone-500 text-sm">
            Chưa có danh mục món ăn nào được tạo.
          </div>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
            {categories.slice(0, 12).map((cat) => (
              <Link
                key={cat.id}
                href={`/recipes?CategoryId=${cat.id}`}
                className="group flex flex-col items-center justify-center text-center p-5 rounded-2xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 shadow-sm hover:border-brand-400 hover:shadow-md hover:-translate-y-1 transition-all"
              >
                <div className="w-12 h-12 rounded-xl bg-brand-50 dark:bg-brand-950/50 text-brand-600 dark:text-brand-400 flex items-center justify-center mb-3 group-hover:bg-brand-600 group-hover:text-white transition-colors shadow-inner">
                  <UtensilsCrossed className="w-6 h-6" />
                </div>
                <span className="font-bold text-sm text-stone-900 dark:text-stone-100 group-hover:text-brand-600 dark:group-hover:text-brand-400 transition-colors line-clamp-1">
                  {cat.name}
                </span>
                {cat.description && (
                  <span className="text-[11px] text-stone-500 dark:text-stone-400 line-clamp-1 mt-0.5">
                    {cat.description}
                  </span>
                )}
              </Link>
            ))}
          </div>
        )}
      </section>

      {/* 3. Công thức Mới nhất (Newest Recipes) */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-8">
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 border-b border-stone-200/80 dark:border-stone-800 pb-4">
          <div>
            <div className="flex items-center gap-2 text-brand-600 dark:text-brand-400 font-semibold text-xs tracking-wider uppercase">
              <ChefHat className="w-4 h-4" />
              <span>Cập nhật hàng ngày</span>
            </div>
            <h2 className="text-2xl sm:text-3xl font-extrabold text-stone-900 dark:text-white tracking-tight mt-1">
              Công thức nấu ăn mới nhất
            </h2>
          </div>
          <Button href="/recipes" variant="outline" size="sm" rightIcon={<ArrowRight className="w-4 h-4" />}>
            Xem tất cả công thức
          </Button>
        </div>

        {/* Lưới công thức tái sử dụng RecipeGrid */}
        <RecipeGrid
          recipes={recipes}
          isLoading={isLoadingRecipes}
          skeletonCount={8}
          emptyState={
            <div className="text-center py-12 rounded-2xl bg-white dark:bg-stone-900 border border-stone-200 dark:border-stone-800 p-8">
              <CookingPot className="w-12 h-12 text-stone-400 mx-auto mb-3" />
              <h3 className="font-bold text-base text-stone-900 dark:text-stone-100 mb-1">
                Chưa có công thức nào được xuất bản
              </h3>
              <p className="text-sm text-stone-500 max-w-md mx-auto">
                Hãy quay lại sau hoặc dùng ô tìm kiếm phía trên để tra cứu công thức theo chủ đề.
              </p>
            </div>
          }
        />
      </section>
    </div>
  );
}
