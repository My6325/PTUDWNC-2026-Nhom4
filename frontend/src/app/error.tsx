"use client";

import React, { useEffect } from "react";
import Link from "next/link";
import { AlertCircle, RotateCcw, Home } from "lucide-react";
import { Button } from "@/components/ui/Button";

/**
 * Global Error Boundary của Next.js App Router (Tiêu chí 5 Lab 6).
 * Bắt tất cả lỗi runtime cấp ứng dụng và cho phép người dùng khôi phục trạng thái.
 */
export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    // Ghi nhận lỗi runtime vào console
    console.error("Next.js Global Runtime Error:", error);
  }, [error]);

  return (
    <div className="min-h-[70vh] flex items-center justify-center px-4 py-16 bg-surface-warm">
      <div className="max-w-md w-full bg-white dark:bg-stone-900 rounded-3xl border border-surface-border p-8 sm:p-10 text-center shadow-lg">
        {/* Icon cảnh báo */}
        <div className="w-16 h-16 rounded-2xl bg-brand-50 text-brand-600 flex items-center justify-center mx-auto mb-6 shadow-sm">
          <AlertCircle className="w-8 h-8" />
        </div>

        <h1 className="text-2xl font-extrabold text-stone-900 dark:text-stone-100 tracking-tight mb-3">
          Gian bếp gặp sự cố nhỏ!
        </h1>

        <p className="text-sm text-stone-600 dark:text-stone-400 leading-relaxed mb-6">
          Hệ thống không thể hiển thị trang này vào lúc này. Có thể kết nối máy chủ bị ngắt quãng hoặc dữ liệu tạm thời chưa sẵn sàng.
        </p>

        {error.digest && (
          <p className="text-xs font-mono bg-stone-100 dark:bg-stone-800 text-stone-500 py-1 px-2 rounded mb-6">
            Mã lỗi: {error.digest}
          </p>
        )}

        <div className="flex flex-col sm:flex-row items-center justify-center gap-3">
          <Button
            onClick={() => reset()}
            variant="default"
            size="md"
            className="w-full sm:w-auto"
            leftIcon={<RotateCcw className="w-4 h-4" />}
          >
            Thử lại
          </Button>
          <Button
            href="/"
            variant="outline"
            size="md"
            className="w-full sm:w-auto"
            leftIcon={<Home className="w-4 h-4" />}
          >
            Về Trang chủ
          </Button>
        </div>
      </div>
    </div>
  );
}
