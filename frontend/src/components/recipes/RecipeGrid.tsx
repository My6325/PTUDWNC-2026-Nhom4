import React from "react";
import { RecipeCard, RecipeCardData, RecipeCardSkeleton } from "./RecipeCard";
import { EmptyState } from "@/components/ui/EmptyState";
import { cn } from "@/utils/cn";

export interface RecipeGridProps {
  recipes?: RecipeCardData[];
  isLoading?: boolean;
  skeletonCount?: number;
  emptyState?: React.ReactNode;
  className?: string;
}

export const RecipeGrid: React.FC<RecipeGridProps> = ({
  recipes = [],
  isLoading = false,
  skeletonCount = 6,
  emptyState,
  className,
}) => {
  if (isLoading) {
    return (
      <div
        className={cn(
          "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6",
          className
        )}
      >
        {Array.from({ length: skeletonCount }).map((_, index) => (
          <RecipeCardSkeleton key={`skeleton-${index}`} />
        ))}
      </div>
    );
  }

  if (recipes.length === 0) {
    return (
      <div className={cn("w-full py-6", className)}>
        {emptyState || (
          <EmptyState
            title="Chưa có công thức nào"
            description="Hiện chưa tìm thấy công thức nấu ăn nào phù hợp với bộ lọc đã chọn."
          />
        )}
      </div>
    );
  }

  return (
    <div
      className={cn(
        "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6",
        className
      )}
    >
      {recipes.map((recipe) => (
        <RecipeCard key={recipe.id} recipe={recipe} />
      ))}
    </div>
  );
};
