import React from "react";
import Link from "next/link";
import { ChefHat, Heart } from "lucide-react";

export const SiteFooter: React.FC = () => {
  return (
    <footer className="mt-auto border-t border-stone-200 dark:border-stone-800 bg-white dark:bg-stone-900 text-stone-600 dark:text-stone-400 transition-colors">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 md:py-16">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8 lg:gap-12">
          {/* Cột 1: Thông tin thương hiệu */}
          <div className="md:col-span-2 space-y-4">
            <Link href="/" className="inline-flex items-center gap-2.5">
              <div className="w-9 h-9 rounded-xl bg-brand-500 text-white flex items-center justify-center shadow-sm">
                <ChefHat className="w-5 h-5" />
              </div>
              <span className="font-extrabold text-xl tracking-tight text-stone-900 dark:text-white">
                Culinary<span className="text-brand-600 dark:text-brand-400">Blog</span>
              </span>
            </Link>
            <p className="text-sm text-stone-500 dark:text-stone-400 max-w-sm leading-relaxed">
              Nền tảng chia sẻ và học hỏi công thức nấu ăn phong phú, dễ làm và đầy đủ dinh dưỡng cho mọi gia đình Việt.
            </p>
            <div className="text-xs text-stone-400 dark:text-stone-500">
              Phát triển với .NET 10 Minimal APIs & Next.js 15 App Router
            </div>
          </div>

          {/* Cột 2: Điều hướng nhanh */}
          <div className="space-y-3">
            <h4 className="text-sm font-semibold uppercase tracking-wider text-stone-900 dark:text-stone-200">
              Khám phá
            </h4>
            <ul className="space-y-2 text-sm">
              <li>
                <Link href="/" className="hover:text-brand-600 dark:hover:text-brand-400 transition-colors">
                  Trang chủ
                </Link>
              </li>
              <li>
                <Link href="/recipes" className="hover:text-brand-600 dark:hover:text-brand-400 transition-colors">
                  Tất cả công thức
                </Link>
              </li>
              <li>
                <Link href="/categories" className="hover:text-brand-600 dark:hover:text-brand-400 transition-colors">
                  Danh mục món ăn
                </Link>
              </li>
            </ul>
          </div>

          {/* Cột 3: Dự án & Đội ngũ */}
          <div className="space-y-3">
            <h4 className="text-sm font-semibold uppercase tracking-wider text-stone-900 dark:text-stone-200">
              Đồ án môn học
            </h4>
            <p className="text-sm text-stone-500 dark:text-stone-400 leading-relaxed">
              Phát triển Ứng dụng Web Nâng cao (PTUDWNC) — Nhóm 4 (Học kỳ 1 / 2026).
            </p>
            <div className="pt-1">
              <span className="inline-flex items-center gap-1.5 text-xs text-stone-400">
                Làm với <Heart className="w-3.5 h-3.5 text-red-500 fill-current" /> đam mê ẩm thực
              </span>
            </div>
          </div>
        </div>

        {/* Thanh bản quyền dưới đáy */}
        <div className="mt-12 pt-8 border-t border-stone-100 dark:border-stone-800/80 flex flex-col sm:flex-row items-center justify-between gap-4 text-xs text-stone-400">
          <p>© {new Date().getFullYear()} Culinary Blog. Bản quyền thuộc về Nhóm 4.</p>
          <div className="flex items-center gap-4">
            <Link href="/recipes" className="hover:underline">
              Tìm kiếm công thức
            </Link>
            <Link href="/categories" className="hover:underline">
              Danh mục
            </Link>
          </div>
        </div>
      </div>
    </footer>
  );
};
