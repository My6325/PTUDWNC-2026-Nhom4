import React from "react";
import { ChefHat } from "lucide-react";
import { cn } from "@/lib/utils";

export interface LoadingSpinnerProps {
  size?: "sm" | "md" | "lg";
  text?: string;
  className?: string;
}

/**
 * Component LoadingSpinner dùng chung phong cách Budget Bytes
 */
export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  size = "md",
  text = "Đang chuẩn bị công thức ngon...",
  className,
}) => {
  const sizeMap = {
    sm: "w-6 h-6",
    md: "w-10 h-10",
    lg: "w-16 h-16",
  };

  const textMap = {
    sm: "text-xs",
    md: "text-sm",
    lg: "text-base",
  };

  return (
    <div
      role="status"
      aria-live="polite"
      className={cn("flex flex-col items-center justify-center p-8 gap-3 text-stone-600 dark:text-stone-300", className)}
    >
      <div className="relative flex items-center justify-center">
        {/* Vòng quay ngoài */}
        <div
          className={cn(
            "rounded-full border-2 border-stone-200 border-t-brand-600 animate-spin dark:border-stone-800 dark:border-t-brand-500",
            sizeMap[size]
          )}
        />
        {/* Biểu tượng đầu bếp ở giữa */}
        <div className="absolute inset-0 flex items-center justify-center text-brand-600 animate-pulse">
          <ChefHat className={size === "lg" ? "w-7 h-7" : size === "md" ? "w-5 h-5" : "w-3 h-3"} />
        </div>
      </div>
      {text && (
        <p className={cn("font-medium text-stone-600 dark:text-stone-400 tracking-wide", textMap[size])}>
          {text}
        </p>
      )}
      <span className="sr-only">Đang tải...</span>
    </div>
  );
};
