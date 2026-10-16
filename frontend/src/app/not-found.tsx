import React from "react";
import Link from "next/link";
import { UtensilsCrossed, Home, Search, BookOpen } from "lucide-react";
import { Button } from "@/components/ui/Button";

/**
 * Trang 404 Not Found toàn cục phong cách Budget Bytes (Tiêu chí 5 Lab 6)
 */
export default function NotFoundPage() {
  return (
    <div className="min-h-[75vh] flex items-center justify-center px-4 py-16 bg-surface-warm">
      <div className="max-w-lg w-full bg-white dark:bg-stone-900 rounded-3xl border border-surface-border p-8 sm:p-12 text-center shadow-lg">
        {/* Biểu tượng ẩm thực 404 */}
        <div className="w-20 h-20 rounded-3xl bg-brand-50 dark:bg-brand-950/60 text-brand-600 flex items-center justify-center mx-auto mb-6 shadow-sm border border-brand-200/50">
          <UtensilsCrossed className="w-10 h-10" />
        </div>

        <span className="text-xs font-bold tracking-widest uppercase text-brand-600 bg-brand-50 px-3 py-1 rounded-full border border-brand-200">
          Mã lỗi 404
        </span>

        <h1 className="text-2xl sm:text-3xl font-extrabold text-stone-900 dark:text-stone-100 tracking-tight mt-4 mb-3">
          Món ăn này chưa có trong thực đơn!
        </h1>

        <p className="text-sm text-stone-600 dark:text-stone-400 leading-relaxed mb-8 max-w-sm mx-auto">
          Công thức bạn đang tìm kiếm có thể đã được dọn khỏi bếp hoặc đường dẫn liên kết chưa chính xác.
        </p>

        {/* Các nút điều hướng nhanh */}
        <div className="flex flex-col sm:flex-row items-center justify-center gap-3">
          <Button
            href="/"
            variant="default"
            size="md"
            className="w-full sm:w-auto"
            leftIcon={<Home className="w-4 h-4" />}
          >
            Về Trang chủ
          </Button>
          <Button
            href="/recipes"
            variant="outline"
            size="md"
            className="w-full sm:w-auto"
            leftIcon={<Search className="w-4 h-4" />}
          >
            Tìm công thức khác
          </Button>
          <Button
            href="/categories"
            variant="ghost"
            size="md"
            className="w-full sm:w-auto"
            leftIcon={<BookOpen className="w-4 h-4" />}
          >
            Danh mục
          </Button>
        </div>
      </div>
    </div>
  );
}
