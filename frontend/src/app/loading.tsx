import React from "react";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";

/**
 * Trang Loading toàn cục của Next.js App Router (Tiêu chí 5 Lab 6).
 * Hiển thị hiệu ứng Skeleton Shimmer chuẩn Budget Bytes để triệt tiêu hiện tượng giật khung hình (CLS = 0).
 */
export default function GlobalLoading() {
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8 animate-pulse">
      {/* Spinner trung tâm */}
      <div className="flex justify-center py-4">
        <LoadingSpinner size="md" text="Đang tải dữ liệu từ Bếp Việt..." />
      </div>

      {/* Hero skeleton */}
      <div className="h-64 sm:h-80 w-full rounded-3xl bg-stone-200/70 dark:bg-stone-800/60" />

      {/* Grid skeletons mô phỏng thẻ công thức */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        {[1, 2, 3, 4, 5, 6, 7, 8].map((i) => (
          <div
            key={i}
            className="rounded-2xl border border-surface-border bg-white dark:bg-stone-900 p-4 space-y-4 shadow-sm"
          >
            {/* Ảnh món ăn skeleton */}
            <div className="w-full aspect-[4/3] rounded-xl bg-stone-200 dark:bg-stone-800" />
            {/* Tiêu đề skeleton */}
            <div className="space-y-2">
              <div className="h-4 bg-stone-200 dark:bg-stone-800 rounded w-5/6" />
              <div className="h-4 bg-stone-200 dark:bg-stone-800 rounded w-3/5" />
            </div>
            {/* Meta chips skeleton */}
            <div className="flex gap-2 pt-2">
              <div className="h-6 w-16 bg-stone-200 dark:bg-stone-800 rounded-full" />
              <div className="h-6 w-20 bg-stone-200 dark:bg-stone-800 rounded-full" />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
