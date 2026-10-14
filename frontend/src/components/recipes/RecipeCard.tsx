import React from "react";
import Link from "next/link";
import { Utensils, User, Calendar } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { Skeleton } from "@/components/ui/Skeleton";
import { RecipeListDto } from "@/types/recipe";
import { cn } from "@/utils/cn";

export type RecipeCardData = RecipeListDto;

export interface RecipeCardProps {
  recipe: RecipeCardData;
  className?: string;
}

export const RecipeCard: React.FC<RecipeCardProps> = ({ recipe, className }) => {
  // Format ngày tạo an toàn, chống hydration mismatch giữa máy chủ và trình duyệt
  const formatDateSafe = (dateInput: string | Date): string => {
    try {
      const d = typeof dateInput === "string" ? new Date(dateInput) : dateInput;
      if (isNaN(d.getTime())) return "";
      return d.toLocaleDateString("vi-VN", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
      });
    } catch {
      return "";
    }
  };

  const formattedDate = formatDateSafe(recipe.createdAt);

  return (
    <article
      className={cn(
        "group flex flex-col overflow-hidden rounded-2xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 shadow-sm transition-all duration-300 hover:-translate-y-1 hover:shadow-md hover:border-brand-300/60 dark:hover:border-brand-700/60",
        className
      )}
    >
      {/* Khung ảnh món ăn */}
      <Link
        href={`/recipes/${recipe.slug}`}
        className="relative aspect-[4/3] w-full overflow-hidden bg-stone-100 dark:bg-stone-800 block"
        tabIndex={-1}
      >
        {recipe.coverImageUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={recipe.coverImageUrl}
            alt={recipe.title}
            className="h-full w-full object-cover transition-transform duration-500 ease-out group-hover:scale-105"
            loading="lazy"
          />
        ) : (
          <div className="flex h-full w-full flex-col items-center justify-center bg-stone-100 dark:bg-stone-800 text-stone-400">
            <Utensils className="h-10 w-10 stroke-[1.5] mb-1" />
            <span className="text-xs font-medium">Culinary Blog</span>
          </div>
        )}

        {/* Badges góc trên ảnh */}
        <div className="absolute top-3 left-3 flex flex-wrap gap-1.5">
          {recipe.categoryName && (
            <Badge variant="brand" size="sm" className="backdrop-blur-md bg-white/90 dark:bg-stone-900/90 shadow-sm">
              {recipe.categoryName}
            </Badge>
          )}
        </div>
        <div className="absolute top-3 right-3">
          <Badge difficulty={recipe.difficulty} size="sm" className="backdrop-blur-md bg-white/90 dark:bg-stone-900/90 shadow-sm" />
        </div>
      </Link>

      {/* Thông tin bài viết */}
      <div className="flex flex-1 flex-col p-5">
        <h3 className="text-base md:text-lg font-bold text-stone-900 dark:text-stone-100 line-clamp-2 leading-snug group-hover:text-brand-600 dark:group-hover:text-brand-400 transition-colors">
          <Link href={`/recipes/${recipe.slug}`} className="focus-visible:outline-none focus-visible:underline">
            {recipe.title}
          </Link>
        </h3>

        {/* Footer meta (Tác giả & Ngày đăng) */}
        <div className="mt-auto pt-4 flex items-center justify-between text-xs text-stone-500 dark:text-stone-400 border-t border-stone-100 dark:border-stone-800/80">
          <span className="inline-flex items-center gap-1.5 font-medium truncate max-w-[140px]">
            <User className="h-3.5 w-3.5 shrink-0 text-stone-400" />
            <span className="truncate">{recipe.authorName || "Đầu bếp"}</span>
          </span>
          <span className="inline-flex items-center gap-1 text-stone-400">
            <Calendar className="h-3.5 w-3.5 shrink-0" />
            <span suppressHydrationWarning>{formattedDate}</span>
          </span>
        </div>
      </div>
    </article>
  );
};

export const RecipeCardSkeleton: React.FC = () => {
  return (
    <div className="flex flex-col overflow-hidden rounded-2xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 p-0 shadow-sm">
      <Skeleton className="aspect-[4/3] w-full rounded-none" />
      <div className="p-5 space-y-3">
        <div className="flex gap-2">
          <Skeleton className="h-5 w-16 rounded-full" />
          <Skeleton className="h-5 w-12 rounded-full" />
        </div>
        <Skeleton className="h-6 w-5/6 rounded" />
        <Skeleton className="h-4 w-3/4 rounded" />
        <div className="pt-3 border-t border-stone-100 dark:border-stone-800 flex justify-between">
          <Skeleton className="h-4 w-20 rounded" />
          <Skeleton className="h-4 w-16 rounded" />
        </div>
      </div>
    </div>
  );
};
