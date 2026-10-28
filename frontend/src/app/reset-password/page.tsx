"use client";

import React, { useState, useEffect, Suspense } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import {
  ChefHat,
  Lock,
  Eye,
  EyeOff,
  Mail,
  KeyRound,
  CheckCircle2,
  LogIn,
  ArrowRight,
  ShieldCheck,
} from "lucide-react";
import { authService } from "@/services/authService";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Alert } from "@/components/ui/Alert";
import { extractApiErrorMessage } from "@/utils/apiError";

const resetPasswordSchema = z
  .object({
    email: z
      .string()
      .min(1, "Vui lòng nhập địa chỉ email.")
      .email("Địa chỉ email không hợp lệ."),
    token: z
      .string()
      .min(1, "Vui lòng nhập mã token đặt lại mật khẩu."),
    newPassword: z
      .string()
      .min(1, "Vui lòng nhập mật khẩu mới.")
      .min(8, "Mật khẩu mới phải có tối thiểu 8 ký tự.")
      .regex(
        /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/,
        "Mật khẩu phải bao gồm ít nhất một chữ hoa, một chữ thường và một chữ số."
      ),
    confirmNewPassword: z
      .string()
      .min(1, "Vui lòng xác nhận mật khẩu mới."),
  })
  .refine((data) => data.newPassword === data.confirmNewPassword, {
    message: "Xác nhận mật khẩu mới không trùng khớp.",
    path: ["confirmNewPassword"],
  });

type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

function ResetPasswordFormContent() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      email: "",
      token: "",
      newPassword: "",
      confirmNewPassword: "",
    },
  });

  // Tự động đọc email và token từ URL query params
  useEffect(() => {
    if (searchParams) {
      const emailParam = searchParams.get("email");
      const tokenParam = searchParams.get("token");

      if (emailParam) {
        setValue("email", emailParam);
      }
      if (tokenParam) {
        setValue("token", tokenParam);
      }
    }
  }, [searchParams, setValue]);

  const onSubmit = async (data: ResetPasswordFormData) => {
    setErrorMessage(null);
    try {
      await authService.resetPassword({
        email: data.email.trim(),
        token: data.token.trim(),
        newPassword: data.newPassword,
        confirmNewPassword: data.confirmNewPassword,
      });

      setIsSuccess(true);
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 422) {
        setErrorMessage(
          extractApiErrorMessage(
            err,
            "Mã token không hợp lệ hoặc đã hết hạn. Vui lòng gửi lại yêu cầu quên mật khẩu."
          )
        );
      } else {
        setErrorMessage(
          extractApiErrorMessage(
            err,
            "Đặt lại mật khẩu không thành công. Vui lòng kiểm tra lại thông tin."
          )
        );
      }
    }
  };

  return (
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
          Đặt lại mật khẩu
        </h1>
        <p className="mt-2 text-sm text-stone-500 dark:text-stone-400">
          Thiết lập mật khẩu bảo mật mới cho tài khoản của bạn
        </p>
      </div>

      {isSuccess ? (
        <div className="space-y-6 text-center">
          <div className="w-16 h-16 mx-auto rounded-full bg-emerald-100 dark:bg-emerald-950/50 text-emerald-600 dark:text-emerald-400 flex items-center justify-center">
            <CheckCircle2 className="w-9 h-9" />
          </div>

          <div className="space-y-2">
            <h2 className="text-lg font-bold text-stone-900 dark:text-stone-100">
              Đổi mật khẩu thành công!
            </h2>
            <p className="text-sm text-stone-600 dark:text-stone-300 leading-relaxed">
              Mật khẩu mới của bạn đã được cập nhật thành công vào hệ thống. Bạn có thể sử dụng mật khẩu mới này để đăng nhập ngay bây giờ.
            </p>
          </div>

          <div className="pt-2">
            <Button
              variant="primary"
              size="lg"
              className="w-full font-bold shadow-md shadow-brand-500/20"
              onClick={() => router.push("/login")}
              leftIcon={<LogIn className="w-5 h-5" />}
            >
              Đăng nhập ngay
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
            {/* Email */}
            <Input
              label="Địa chỉ Email"
              type="email"
              placeholder="ban@example.com"
              autoComplete="email"
              leftIcon={<Mail className="w-4 h-4 text-stone-400" />}
              error={errors.email?.message}
              {...register("email")}
            />

            {/* Token */}
            <Input
              label="Mã Token xác thực"
              placeholder="Nhập mã token từ email"
              leftIcon={<KeyRound className="w-4 h-4 text-stone-400" />}
              error={errors.token?.message}
              {...register("token")}
            />

            {/* Mật khẩu mới */}
            <div className="space-y-1.5">
              <Input
                label="Mật khẩu mới *"
                type={showNewPassword ? "text" : "password"}
                placeholder="Tối thiểu 8 ký tự (hoa, thường, số)"
                autoComplete="new-password"
                leftIcon={<Lock className="w-4 h-4 text-stone-400" />}
                rightIcon={
                  <button
                    type="button"
                    onClick={() => setShowNewPassword(!showNewPassword)}
                    className="text-stone-400 hover:text-stone-600 dark:hover:text-stone-300 transition-colors focus:outline-none"
                    tabIndex={-1}
                    aria-label={showNewPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                  >
                    {showNewPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                }
                error={errors.newPassword?.message}
                {...register("newPassword")}
              />
            </div>

            {/* Xác nhận mật khẩu mới */}
            <div className="space-y-1.5">
              <Input
                label="Xác nhận mật khẩu mới *"
                type={showConfirmPassword ? "text" : "password"}
                placeholder="Nhập lại mật khẩu mới"
                autoComplete="new-password"
                leftIcon={<ShieldCheck className="w-4 h-4 text-stone-400" />}
                rightIcon={
                  <button
                    type="button"
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    className="text-stone-400 hover:text-stone-600 dark:hover:text-stone-300 transition-colors focus:outline-none"
                    tabIndex={-1}
                    aria-label={showConfirmPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                  >
                    {showConfirmPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                }
                error={errors.confirmNewPassword?.message}
                {...register("confirmNewPassword")}
              />
            </div>

            {/* Nút gửi */}
            <Button
              type="submit"
              variant="primary"
              size="lg"
              className="w-full mt-2 font-bold shadow-md shadow-brand-500/20"
              isLoading={isSubmitting}
              disabled={isSubmitting}
              leftIcon={<KeyRound className="w-5 h-5" />}
            >
              {isSubmitting ? "Đang cập nhật..." : "Cập nhật mật khẩu mới"}
            </Button>
          </form>

          {/* Quay về đăng nhập */}
          <div className="mt-8 pt-6 border-t border-stone-100 dark:border-stone-800 text-center text-sm text-stone-600 dark:text-stone-400">
            Đã nhớ lại mật khẩu?{" "}
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
  );
}

export default function ResetPasswordPage() {
  return (
    <div className="min-h-[85vh] flex items-center justify-center py-12 px-4 bg-surface-warm dark:bg-stone-950/50">
      <Suspense
        fallback={
          <div className="w-full max-w-md bg-white dark:bg-stone-900 rounded-3xl p-8 border border-stone-200 animate-pulse h-96" />
        }
      >
        <ResetPasswordFormContent />
      </Suspense>
    </div>
  );
}
