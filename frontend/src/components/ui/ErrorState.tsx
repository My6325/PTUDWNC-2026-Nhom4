import React from "react";
import { AlertCircle, RotateCcw } from "lucide-react";
import { Button } from "./Button";
import { cn } from "@/lib/utils";

export interface ErrorStateProps {
  title?: string;
  message?: string;
  onRetry?: () => void;
  className?: string;
}

/**
 * Component ErrorState dùng chung (Tiêu chí 5 Lab 6)
 * Hiển thị thông điệp lỗi trực quan với nút Thử lại thân thiện.
 */
export const ErrorState: React.FC<ErrorStateProps> = ({
  title = "Đã xảy ra sự cố không mong muốn",
  message = "Không thể tải dữ liệu vào lúc này. Vui lòng kiểm tra kết nối mạng hoặc thử lại sau.",
  onRetry,
  className,
}) => {
  return (
    <div
      role="alert"
      className={cn(
        "flex flex-col items-center justify-center p-8 sm:p-12 text-center rounded-2xl border border-red-200/80 bg-red-50/50 dark:bg-red-950/20 dark:border-red-900/60 max-w-lg mx-auto my-8",
        className
      )}
    >
      <div className="w-14 h-14 rounded-2xl bg-red-100 dark:bg-red-900/40 text-red-600 dark:text-red-400 flex items-center justify-center mb-4 shadow-sm">
        <AlertCircle className="w-7 h-7" aria-hidden="true" />
      </div>

      <h3 className="font-bold text-lg text-stone-900 dark:text-stone-100 mb-2">
        {title}
      </h3>

      <p className="text-sm text-stone-600 dark:text-stone-400 max-w-sm mb-6 leading-relaxed">
        {message}
      </p>

      {onRetry && (
        <Button
          onClick={onRetry}
          variant="default"
          size="sm"
          leftIcon={<RotateCcw className="w-4 h-4" />}
        >
          Thử tải lại trang
        </Button>
      )}
    </div>
  );
};
