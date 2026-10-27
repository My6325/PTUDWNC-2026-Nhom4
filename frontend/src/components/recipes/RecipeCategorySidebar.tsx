"use client";

import Link from "next/link";
import { BookOpen, ArrowRight } from "lucide-react";
import { useRecipeCategories } from "@/hooks/useRecipeCategories";
import { Button } from "@/components/ui/Button";
import { Skeleton } from "@/components/ui/Skeleton";

export function RecipeCategorySidebar() {
  const { categories, isLoading, error, retry } = useRecipeCategories();

  return (
    <aside aria-labelledby="home-categories-title" className="min-w-0 rounded-2xl border border-surface-border bg-white p-5 dark:bg-stone-900 dark:border-stone-800">
      <h2 id="home-categories-title" className="flex items-center gap-2 text-xl font-bold text-stone-900 dark:text-white">
        <BookOpen className="h-5 w-5 text-brand-600" aria-hidden="true" /> Danh mục món ăn
      </h2>
      <div className="mt-5" aria-busy={isLoading}>
        {isLoading ? (
          <div role="status" aria-label="Đang tải danh mục" className="space-y-3">
            {Array.from({ length: 6 }, (_, i) => <Skeleton key={i} className="h-10 w-full" />)}
          </div>
        ) : error ? (
          <div role="alert" className="space-y-3 text-sm text-red-700 dark:text-red-300">
            <p>Không thể tải danh mục. {error}</p>
            <Button variant="outline" size="sm" onClick={retry}>Thử lại danh mục</Button>
          </div>
        ) : categories.length === 0 ? (
          <p className="text-sm text-stone-500">Chưa có danh mục món ăn.</p>
        ) : (
          <nav aria-label="Danh mục công thức" className="max-h-96 overflow-y-auto">
            <ul className="space-y-1">
              {categories.map(category => (
                <li key={category.id}>
                  <Link href={`/recipes?CategoryId=${encodeURIComponent(category.id)}`} className="flex items-center justify-between gap-2 rounded-xl px-3 py-2.5 text-sm text-stone-700 hover:bg-brand-50 hover:text-brand-700 focus-visible:outline focus-visible:outline-brand-600 dark:text-stone-200 dark:hover:bg-stone-800">
                    <span className="break-words">{category.name}</span>
                    <ArrowRight className="h-4 w-4 shrink-0" aria-hidden="true" />
                  </Link>
                </li>
              ))}
            </ul>
          </nav>
        )}
      </div>
      <Link href="/categories" className="mt-5 block border-t border-surface-border pt-4 text-sm font-semibold text-brand-600 hover:underline">Xem tất cả danh mục</Link>
    </aside>
  );
}
