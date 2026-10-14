import React from "react";
import { ChevronLeft, ChevronRight, MoreHorizontal } from "lucide-react";
import { cn } from "@/lib/utils";

export interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  isLoading?: boolean;
  className?: string;
}

/**
 * Component Phân trang Dùng chung (Tiêu chí 5 Lab 6)
 * Thiết kế theo chuẩn Budget Bytes: bo góc rounded-xl, touch target >= 44px, nút cam đất nung.
 */
export const Pagination: React.FC<PaginationProps> = ({
  currentPage,
  totalPages,
  onPageChange,
  isLoading = false,
  className,
}) => {
  if (totalPages <= 1) return null;

  // Thuật toán rút gọn danh sách trang
  const getPageNumbers = () => {
    const pages: (number | string)[] = [];
    const maxVisiblePages = 7;

    if (totalPages <= maxVisiblePages) {
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i);
      }
    } else {
      if (currentPage <= 4) {
        for (let i = 1; i <= 5; i++) {
          pages.push(i);
        }
        pages.push("ellipsis-end");
        pages.push(totalPages);
      } else if (currentPage >= totalPages - 3) {
        pages.push(1);
        pages.push("ellipsis-start");
        for (let i = totalPages - 4; i <= totalPages; i++) {
          pages.push(i);
        }
      } else {
        pages.push(1);
        pages.push("ellipsis-start");
        pages.push(currentPage - 1);
        pages.push(currentPage);
        pages.push(currentPage + 1);
        pages.push("ellipsis-end");
        pages.push(totalPages);
      }
    }

    return pages;
  };

  const pages = getPageNumbers();

  return (
    <nav
      role="navigation"
      aria-label="Phân trang danh sách bài viết"
      className={cn("flex flex-wrap items-center justify-center gap-1.5 sm:gap-2 pt-8", className)}
    >
      {/* Nút Trước */}
      <button
        type="button"
        onClick={() => onPageChange(currentPage - 1)}
        disabled={currentPage <= 1 || isLoading}
        aria-label="Về trang trước"
        className={cn(
          "inline-flex items-center justify-center gap-1 h-11 px-3.5 sm:px-4 rounded-xl text-sm font-medium transition-all min-h-[44px]",
          "border border-surface-border bg-white text-stone-700 hover:bg-stone-50 hover:text-stone-900 shadow-sm",
          "disabled:opacity-40 disabled:pointer-events-none active:scale-95",
          "dark:bg-stone-900 dark:border-stone-800 dark:text-stone-300"
        )}
      >
        <ChevronLeft className="w-4 h-4 shrink-0" aria-hidden="true" />
        <span className="hidden sm:inline">Trước</span>
      </button>

      {/* Danh sách các số trang */}
      <div className="flex items-center gap-1 sm:gap-1.5">
        {pages.map((page, index) => {
          if (typeof page === "string") {
            return (
              <span
                key={`${page}-${index}`}
                className="w-10 h-11 flex items-center justify-center text-stone-400 select-none"
                aria-hidden="true"
              >
                <MoreHorizontal className="w-4 h-4" />
              </span>
            );
          }

          const isCurrent = page === currentPage;

          return (
            <button
              key={page}
              type="button"
              onClick={() => onPageChange(page)}
              disabled={isLoading}
              aria-current={isCurrent ? "page" : undefined}
              aria-label={`Trang ${page}`}
              className={cn(
                "w-10 h-11 sm:w-11 sm:h-11 flex items-center justify-center rounded-xl text-sm font-semibold transition-all min-h-[44px] min-w-[40px] select-none",
                isCurrent
                  ? "bg-brand-600 text-white shadow-sm shadow-brand-600/30 font-bold scale-105"
                  : "bg-white border border-surface-border text-stone-700 hover:bg-stone-50 hover:border-brand-300 active:scale-95 dark:bg-stone-900 dark:border-stone-800 dark:text-stone-300 dark:hover:bg-stone-800"
              )}
            >
              {page}
            </button>
          );
        })}
      </div>

      {/* Nút Sau */}
      <button
        type="button"
        onClick={() => onPageChange(currentPage + 1)}
        disabled={currentPage >= totalPages || isLoading}
        aria-label="Sang trang tiếp theo"
        className={cn(
          "inline-flex items-center justify-center gap-1 h-11 px-3.5 sm:px-4 rounded-xl text-sm font-medium transition-all min-h-[44px]",
          "border border-surface-border bg-white text-stone-700 hover:bg-stone-50 hover:text-stone-900 shadow-sm",
          "disabled:opacity-40 disabled:pointer-events-none active:scale-95",
          "dark:bg-stone-900 dark:border-stone-800 dark:text-stone-300"
        )}
      >
        <span className="hidden sm:inline">Sau</span>
        <ChevronRight className="w-4 h-4 shrink-0" aria-hidden="true" />
      </button>
    </nav>
  );
};
