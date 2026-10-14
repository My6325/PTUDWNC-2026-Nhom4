import React, { forwardRef } from "react";
import Link from "next/link";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";
import { Spinner } from "./Spinner";

/**
 * Button Component chuẩn shadcn/ui với CVA & thẩm mỹ Budget Bytes.
 */
export const buttonVariants = cva(
  "inline-flex items-center justify-center font-medium rounded-xl transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2 disabled:opacity-50 disabled:pointer-events-none select-none active:scale-[0.98]",
  {
    variants: {
      variant: {
        default:
          "bg-brand-600 text-white hover:bg-brand-700 active:bg-brand-800 shadow-sm shadow-brand-600/20",
        primary:
          "bg-brand-600 text-white hover:bg-brand-700 active:bg-brand-800 shadow-sm shadow-brand-600/20",
        secondary:
          "bg-stone-100 text-stone-800 hover:bg-stone-200 active:bg-stone-300 dark:bg-stone-800 dark:text-stone-100",
        herb:
          "bg-herb-600 text-white hover:bg-herb-700 active:bg-herb-800 shadow-sm shadow-herb-600/20",
        outline:
          "border border-surface-border bg-white text-stone-700 hover:bg-stone-50 hover:text-stone-900 dark:bg-stone-900 dark:text-stone-200 dark:hover:bg-stone-800",
        ghost:
          "bg-transparent text-stone-700 hover:bg-stone-100 hover:text-stone-900 dark:text-stone-300 dark:hover:bg-stone-800",
        destructive:
          "bg-red-600 text-white hover:bg-red-700 active:bg-red-800 shadow-sm shadow-red-600/20",
        danger:
          "bg-red-600 text-white hover:bg-red-700 active:bg-red-800 shadow-sm shadow-red-600/20",
        link:
          "text-brand-600 underline-offset-4 hover:underline p-0 h-auto",
      },
      size: {
        default: "h-11 px-5 py-2.5 text-sm gap-2 min-h-[44px]",
        md: "h-11 px-5 py-2.5 text-sm gap-2 min-h-[44px]",
        sm: "h-9 px-3.5 py-1.5 text-xs gap-1.5 rounded-lg",
        lg: "h-12 px-7 py-3 text-base gap-2.5 rounded-xl min-h-[48px]",
        icon: "h-11 w-11 p-0 shrink-0 min-h-[44px] min-w-[44px]",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
);

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  isLoading?: boolean;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
  href?: string;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      className,
      variant = "default",
      size = "default",
      type = "button",
      isLoading = false,
      leftIcon,
      rightIcon,
      disabled,
      children,
      href,
      ...props
    },
    ref
  ) => {
    const combinedClasses = cn(buttonVariants({ variant, size }), className);

    const content = (
      <>
        {isLoading && <Spinner size={size === "lg" ? "md" : "sm"} className="text-current" />}
        {!isLoading && leftIcon && <span className="inline-flex shrink-0">{leftIcon}</span>}
        {children}
        {!isLoading && rightIcon && <span className="inline-flex shrink-0">{rightIcon}</span>}
      </>
    );

    if (href) {
      return (
        <Link href={href} className={combinedClasses} aria-disabled={disabled || isLoading}>
          {content}
        </Link>
      );
    }

    return (
      <button
        ref={ref}
        type={type}
        className={combinedClasses}
        disabled={disabled || isLoading}
        {...props}
      >
        {content}
      </button>
    );
  }
);

Button.displayName = "Button";
