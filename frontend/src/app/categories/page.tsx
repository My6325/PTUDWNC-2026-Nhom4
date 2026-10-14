"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import {
  FolderHeart,
  ChevronRight,
  ArrowRight,
  Utensils,
  RefreshCw,
  Sparkles,
} from "lucide-react";
import { categoryService } from "@/services/categoryService";
import { CategoryDto } from "@/types/category";
import { Button } from "@/components/ui/Button";
import { Skeleton } from "@/components/ui/Skeleton";
import { Alert } from "@/components/ui/Alert";
import { EmptyState } from "@/components/ui/EmptyState";
import { extractApiErrorMessage } from "@/utils/apiError";
import { cn } from "@/utils/cn";

export default function CategoriesPage() {
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchCategories = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await categoryService.getCategories();
      // Sắp xếp theo orderIndex
      const sorted = [...data].sort((a, b) => a.orderIndex - b.orderIndex);
      setCategories(sorted);
    } catch (err: unknown) {
      setError(
        extractApiErrorMessage(
          err,
          "Không thể tải danh sách danh mục ẩm thực. Vui lòng thử lại sau."
        )
      );
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchCategories();
  }, []);

  return (
    <div className="min-h-screen bg-surface-warm dark:bg-stone-950/50 pb-20">
      {/* Breadcrumb Navigation */}
      <div className="border-b border-surface-border dark:border-stone-800 bg-white dark:bg-stone-900/80">
        <div className="container-custom mx-auto px-4 py-3">
          <nav className="flex items-center gap-1.5 text-xs text-stone-500 dark:text-stone-400">
            <Link
              href="/"
              className="hover:text-stone-900 dark:hover:text-stone-100 transition-colors"
            >
              Trang chủ
            </Link>
            <ChevronRight className="h-3 w-3 shrink-0" />
            <span className="text-stone-800 dark:text-stone-200 font-medium">
              Danh mục ẩm thực
            </span>
          </nav>
        </div>
      </div>

      <div className="container-custom mx-auto px-4 pt-10">
        {/* Header giới thiệu */}
        <div className="max-w-2xl mb-10 space-y-3">
          <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-brand-50 dark:bg-brand-950/60 text-brand-700 dark:text-brand-300 text-xs font-bold border border-brand-200 dark:border-brand-800/60">
            <Sparkles className="h-3.5 w-3.5" />
            <span>Bộ sưu tập món ngon</span>
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold text-stone-900 dark:text-stone-50 tracking-tight">
            Danh mục ẩm thực
          </h1>
          <p className="text-stone-600 dark:text-stone-400 text-base leading-relaxed">
            Khám phá các công thức nấu ăn phong phú được phân loại khoa học theo
            từng chủ đề, nguyên liệu và phong cách bữa ăn hàng ngày.
          </p>
        </div>

        {/* Trạng thái Lỗi */}
        {error && (
          <div className="max-w-xl mb-8">
            <Alert
              variant="error"
              title="Đã có lỗi xảy ra"
              className="mb-4"
            >
              {error}
            </Alert>
            <Button
              variant="outline"
              size="sm"
              onClick={fetchCategories}
              leftIcon={<RefreshCw className="h-4 w-4" />}
            >
              Thử tải lại
            </Button>
          </div>
        )}

        {/* Trạng thái Đang tải (Skeleton) */}
        {isLoading && (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
            {Array.from({ length: 8 }).map((_, idx) => (
              <div
                key={idx}
                className="rounded-2xl border border-stone-200/80 dark:border-stone-800 bg-white dark:bg-stone-900 overflow-hidden shadow-xs space-y-4 p-4"
              >
                <Skeleton className="aspect-[4/3] w-full rounded-xl" />
                <Skeleton className="h-6 w-3/4 rounded-md" />
                <Skeleton className="h-4 w-full rounded-md" />
                <Skeleton className="h-4 w-2/3 rounded-md" />
              </div>
            ))}
          </div>
        )}

        {/* Trạng thái Không có danh mục */}
        {!isLoading && !error && categories.length === 0 && (
          <div className="py-12">
            <EmptyState
              title="Chưa có danh mục nào"
              description="Hệ thống đang cập nhật các danh mục món ăn mới. Vui lòng quay lại sau!"
              icon={<FolderHeart className="h-10 w-10 text-stone-400" />}
              action={
                <Button variant="primary" href="/recipes">
                  Khám phá tất cả công thức
                </Button>
              }
            />
          </div>
        )}

        {/* Danh sách danh mục ẩm thực */}
        {!isLoading && !error && categories.length > 0 && (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
            {categories.map((category) => (
              <Link
                key={category.id}
                href={`/categories/${category.slug}`}
                className="group flex flex-col rounded-2xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 shadow-xs hover:shadow-md hover:border-brand-300 dark:hover:border-brand-700/60 transition-all duration-300 overflow-hidden hover:-translate-y-1"
              >
                {/* Khung ảnh danh mục */}
                <div className="relative aspect-[4/3] w-full overflow-hidden bg-stone-100 dark:bg-stone-800">
                  {category.imageUrl ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img
                      src={category.imageUrl}
                      alt={category.name}
                      className="h-full w-full object-cover transition-transform duration-500 ease-out group-hover:scale-105"
                      loading="lazy"
                    />
                  ) : (
                    <div className="flex h-full w-full flex-col items-center justify-center text-stone-400 bg-stone-100 dark:bg-stone-800">
                      <Utensils className="h-10 w-10 stroke-[1.4] mb-1 group-hover:text-brand-500 transition-colors" />
                      <span className="text-xs font-semibold">Culinary Blog</span>
                    </div>
                  )}

                  <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity flex items-end p-4">
                    <span className="text-white text-xs font-bold inline-flex items-center gap-1">
                      Xem danh mục <ArrowRight className="h-3.5 w-3.5" />
                    </span>
                  </div>
                </div>

                {/* Thông tin mô tả danh mục */}
                <div className="flex flex-1 flex-col p-5">
                  <h2 className="text-lg font-bold text-stone-900 dark:text-stone-100 group-hover:text-brand-600 dark:group-hover:text-brand-400 transition-colors">
                    {category.name}
                  </h2>

                  {category.description ? (
                    <p className="mt-2 text-sm text-stone-600 dark:text-stone-400 line-clamp-2 leading-relaxed">
                      {category.description}
                    </p>
                  ) : (
                    <p className="mt-2 text-xs text-stone-400 dark:text-stone-500 italic">
                      Bộ sưu tập các món ăn đặc sắc thuộc chủ đề {category.name}.
                    </p>
                  )}

                  <div className="mt-auto pt-4 flex items-center justify-between text-xs font-semibold text-brand-600 dark:text-brand-400">
                    <span>Khám phá món ăn</span>
                    <ArrowRight className="h-4 w-4 transform group-hover:translate-x-1 transition-transform" />
                  </div>
                </div>
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
