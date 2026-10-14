import React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

/**
 * Badge Component chuẩn shadcn/ui & phong cách Budget Bytes
 */
export const badgeVariants = cva(
  "inline-flex items-center font-medium rounded-full select-none transition-colors",
  {
    variants: {
      variant: {
        default: "bg-stone-100 dark:bg-stone-800 text-stone-700 dark:text-stone-300",
        brand:
          "bg-brand-50 dark:bg-brand-950/60 text-brand-700 dark:text-brand-300 border border-brand-200/60 dark:border-brand-800/60",
        herb:
          "bg-herb-50 dark:bg-herb-950/60 text-herb-700 dark:text-herb-300 border border-herb-200/60 dark:border-herb-800/60",
        warning:
          "bg-amber-50 dark:bg-amber-950/60 text-amber-700 dark:text-amber-300 border border-amber-200/60 dark:border-amber-800/60",
        danger:
          "bg-red-50 dark:bg-red-950/60 text-red-700 dark:text-red-300 border border-red-200/60 dark:border-red-800/60",
        outline:
          "border border-surface-border text-stone-700 dark:text-stone-300 bg-white dark:bg-stone-900",
      },
      size: {
        sm: "px-2.5 py-0.5 text-xs gap-1",
        md: "px-3 py-1 text-sm gap-1.5",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "sm",
    },
  }
);

export interface BadgeProps
  extends React.HTMLAttributes<HTMLSpanElement>,
    VariantProps<typeof badgeVariants> {
  difficulty?: "Easy" | "Medium" | "Hard" | "easy" | "medium" | "hard" | 1 | 2 | 3;
}

export const Badge: React.FC<BadgeProps> = ({
  className,
  variant = "default",
  difficulty,
  size = "sm",
  children,
  ...props
}) => {
  let resolvedVariant = variant;
  let label = children;

  // Xử lý tự động map Enum độ khó của Backend
  if (difficulty !== undefined) {
    const normalized = typeof difficulty === "string" ? difficulty.toLowerCase() : difficulty;
    if (normalized === "easy" || normalized === 1) {
      resolvedVariant = "herb";
      label = label || "Dễ";
    } else if (normalized === "medium" || normalized === 2) {
      resolvedVariant = "warning";
      label = label || "Trung bình";
    } else if (normalized === "hard" || normalized === 3) {
      resolvedVariant = "danger";
      label = label || "Khó";
    }
  }

  return (
    <span
      className={cn(badgeVariants({ variant: resolvedVariant, size }), className)}
      {...props}
    >
      {label}
    </span>
  );
};
