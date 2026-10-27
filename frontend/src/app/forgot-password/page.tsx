"use client";

import React, { useState } from "react";
import Link from "next/link";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { ChefHat, Mail, ArrowLeft, Send, CheckCircle2, ArrowRight, KeyRound } from "lucide-react";
import { authService } from "@/services/authService";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Alert } from "@/components/ui/Alert";
import { extractApiErrorMessage } from "@/utils/apiError";

const forgotPasswordSchema = z.object({
  email: z
    .string()
    .min(1, "Vui lòng nhập địa chỉ email.")
    .email("Địa chỉ email không hợp lệ."),
});

type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;

export default function ForgotPasswordPage() {
  const [isSuccess, setIsSuccess] = useState(false);
  const [successEmail, setSuccessEmail] = useState("");
  const [devToken, setDevToken] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: {
      email: "",
    },
  });

  const onSubmit = async (data: ForgotPasswordFormData) => {
    setErrorMessage(null);
    try {
      const response = await authService.forgotPassword({
        email: data.email.trim(),
      });

      setSuccessEmail(data.email.trim());
      setIsSuccess(true);
      if (response.resetToken) {
        setDevToken(response.resetToken);
      }
    } catch (err: unknown) {
      setErrorMessage(
        extractApiErrorMessage(
          err,
          "Không thể gửi yêu cầu đặt lại mật khẩu. Vui lòng thử lại sau."
        )
      );
    }
  };

  return (
    <div className="min-h-[85vh] flex items-center justify-center py-12 px-4 bg-surface-warm dark:bg-stone-950/50">
      <div className="w-full max-w-md bg-white dark:bg-stone-900 rounded-3xl border border-surface-border dark:border-stone-800 shadow-xl p-8 sm:p-10">
        {/* Header Logo */}
        <div className="text-center mb-8">
          <Link
            href="/"
            className="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-brand-600 text-white shadow-lg shadow-brand-600/25 mb-4 hover:scale-105 transition-transform"
          >
            <ChefHat className="w-8 h-8" />
          </Link>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-stone-900 dark:text-stone-100 tracking-tight">
            Quên mật khẩu?
          </h1>
          <p className="mt-2 text-sm text-stone-500 dark:text-stone-400">
            Nhập email đã đăng ký để nhận liên kết khôi phục mật khẩu tài khoản
          </p>
        </div>

        {isSuccess ? (
          <div className="space-y-6 text-center">
            <div className="w-16 h-16 mx-auto rounded-full bg-emerald-100 dark:bg-emerald-950/50 text-emerald-600 dark:text-emerald-400 flex items-center justify-center">
              <CheckCircle2 className="w-9 h-9" />
            </div>

            <div className="space-y-2">
              <h2 className="text-lg font-bold text-stone-900 dark:text-stone-100">
                Kiểm tra hòm thư của bạn
              </h2>
              <p className="text-sm text-stone-600 dark:text-stone-300 leading-relaxed">
                Chúng tôi đã gửi hướng dẫn và liên kết đặt lại mật khẩu tới địa chỉ{" "}
                <strong className="text-brand-700 dark:text-brand-300">{successEmail}</strong>.
              </p>
              <p className="text-xs text-stone-500 dark:text-stone-400 pt-1">
                Vui lòng kiểm tra cả thư mục Spam/Rác nếu bạn không thấy email trong hộp thư đến.
              </p>
            </div>

            {/* Hỗ trợ nhanh cho môi trường phát triển / kiểm thử */}
            {devToken && (
              <div className="p-4 bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900/50 rounded-2xl text-left space-y-2">
                <div className="flex items-center gap-1.5 text-xs font-bold text-amber-800 dark:text-amber-300">
                  <KeyRound className="w-4 h-4" />
                  <span>Mã Token khôi phục (Môi trường Dev):</span>
                </div>
                <div className="p-2 bg-white dark:bg-stone-900 rounded-lg text-xs font-mono text-stone-700 dark:text-stone-300 break-all select-all border border-amber-200/60 dark:border-amber-900/40">
                  {devToken}
                </div>
                <Link
                  href={`/reset-password?email=${encodeURIComponent(successEmail)}&token=${encodeURIComponent(devToken)}`}
                  className="inline-flex items-center gap-1 text-xs font-bold text-brand-600 dark:text-brand-400 hover:underline pt-1"
                >
                  Chuyển ngay tới trang Đặt lại mật khẩu <ArrowRight className="w-3.5 h-3.5" />
                </Link>
              </div>
            )}

            <div className="pt-2 space-y-3">
              <Button
                variant="outline"
                className="w-full"
                href={`/reset-password?email=${encodeURIComponent(successEmail)}`}
                leftIcon={<KeyRound className="w-4 h-4" />}
              >
                Nhập mã đặt lại mật khẩu thủ công
              </Button>
              <Button
                variant="ghost"
                className="w-full text-stone-600 dark:text-stone-400"
                href="/login"
                leftIcon={<ArrowLeft className="w-4 h-4" />}
              >
                Quay lại trang Đăng nhập
              </Button>
            </div>
          </div>
        ) : (
          <>
            {errorMessage && (
              <Alert variant="error" className="mb-6" dismissible onDismiss={() => setErrorMessage(null)}>
                {errorMessage}
              </Alert>
            )}

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
              <Input
                label="Địa chỉ Email của bạn"
                type="email"
                placeholder="ban@example.com"
                autoComplete="email"
                leftIcon={<Mail className="w-4 h-4 text-stone-400" />}
                error={errors.email?.message}
                {...register("email")}
              />

              <Button
                type="submit"
                variant="primary"
                size="lg"
                className="w-full font-bold shadow-md shadow-brand-500/20"
                isLoading={isSubmitting}
                disabled={isSubmitting}
                leftIcon={<Send className="w-4 h-4" />}
              >
                {isSubmitting ? "Đang gửi yêu cầu..." : "Gửi liên kết khôi phục"}
              </Button>
            </form>

            <div className="mt-8 pt-6 border-t border-stone-100 dark:border-stone-800 text-center text-sm text-stone-600 dark:text-stone-400">
              Nhớ mật khẩu của bạn?{" "}
              <Link
                href="/login"
                className="font-bold text-brand-600 dark:text-brand-400 hover:text-brand-700 dark:hover:text-brand-300 inline-flex items-center gap-1 transition-colors"
              >
                Đăng nhập ngay <ArrowRight className="w-3.5 h-3.5" />
              </Link>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
