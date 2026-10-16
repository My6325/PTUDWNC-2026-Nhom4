"use client";

import React, { useState, Suspense } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { ChefHat, Mail, Lock, Eye, EyeOff, LogIn, ArrowRight } from "lucide-react";
import { authService } from "@/services/authService";
import { useAuthStore } from "@/stores/auth-store";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Alert } from "@/components/ui/Alert";
import { extractApiErrorMessage } from "@/utils/apiError";

// Schema xác thực dữ liệu đầu vào bằng Zod
const loginSchema = z.object({
  email: z
    .string()
    .min(1, "Vui lòng nhập địa chỉ email.")
    .email("Địa chỉ email không hợp lệ."),
  password: z
    .string()
    .min(1, "Vui lòng nhập mật khẩu."),
});

type LoginFormData = z.infer<typeof loginSchema>;

function LoginFormContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const redirectUrl = searchParams?.get("redirect") || "/";

  const [showPassword, setShowPassword] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: "",
      password: "",
    },
  });

  const onSubmit = async (data: LoginFormData) => {
    setErrorMessage(null);
    try {
      const authRes = await authService.login({
        email: data.email.trim(),
        password: data.password,
      });

      // Cập nhật Zustand reactive store toàn cục
      useAuthStore.getState().login(authRes);

      // Điều hướng về trang trước đó hoặc trang chủ
      router.push(redirectUrl);
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 401) {
        setErrorMessage("Email hoặc mật khẩu không chính xác. Vui lòng kiểm tra lại.");
      } else if (status === 423) {
        setErrorMessage("Tài khoản của bạn đã bị tạm khóa do nhiều lần đăng nhập sai. Vui lòng thử lại sau.");
      } else if (status === 429) {
        setErrorMessage("Bạn đã gửi quá nhiều yêu cầu trong thời gian ngắn. Vui lòng chờ 1 phút rồi thử lại.");
      } else if (status === 422) {
        setErrorMessage("Dữ liệu gửi lên không đúng định dạng yêu cầu.");
      } else {
        setErrorMessage(extractApiErrorMessage(err, "Đăng nhập không thành công. Vui lòng thử lại sau."));
      }
    }
  };

  return (
    <div className="w-full max-w-md bg-white dark:bg-stone-900 rounded-3xl border border-surface-border dark:border-stone-800 shadow-xl p-8 sm:p-10">
      {/* Logo & Tiêu đề */}
      <div className="text-center mb-8">
        <Link
          href="/"
          className="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-brand-600 text-white shadow-lg shadow-brand-600/25 mb-4 hover:scale-105 transition-transform"
        >
          <ChefHat className="w-8 h-8" />
        </Link>
        <h1 className="text-2xl sm:text-3xl font-extrabold text-stone-900 dark:text-stone-100 tracking-tight">
          Chào mừng trở lại!
        </h1>
        <p className="mt-2 text-sm text-stone-500 dark:text-stone-400">
          Đăng nhập vào tài khoản Culinary Blog của bạn
        </p>
      </div>

      {/* Thông báo lỗi nếu có */}
      {errorMessage && (
        <Alert variant="error" className="mb-6" dismissible onDismiss={() => setErrorMessage(null)}>
          {errorMessage}
        </Alert>
      )}

      {/* Form đăng nhập */}
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

        {/* Mật khẩu */}
        <div className="space-y-1.5">
          <Input
            label="Mật khẩu"
            type={showPassword ? "text" : "password"}
            placeholder="Nhập mật khẩu của bạn"
            autoComplete="current-password"
            leftIcon={<Lock className="w-4 h-4 text-stone-400" />}
            rightIcon={
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="text-stone-400 hover:text-stone-600 dark:hover:text-stone-300 transition-colors focus:outline-none"
                tabIndex={-1}
                aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
              >
                {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
              </button>
            }
            error={errors.password?.message}
            {...register("password")}
          />
        </div>

        {/* Nút đăng nhập */}
        <Button
          type="submit"
          variant="primary"
          size="lg"
          className="w-full mt-2 font-bold shadow-md shadow-brand-500/20"
          isLoading={isSubmitting}
          disabled={isSubmitting}
          leftIcon={<LogIn className="w-5 h-5" />}
        >
          {isSubmitting ? "Đang xử lý..." : "Đăng nhập"}
        </Button>
      </form>

      {/* Footer chuyển sang trang Đăng ký */}
      <div className="mt-8 pt-6 border-t border-stone-100 dark:border-stone-800 text-center text-sm text-stone-600 dark:text-stone-400">
        Chưa có tài khoản?{" "}
        <Link
          href={`/register${redirectUrl !== "/" ? `?redirect=${encodeURIComponent(redirectUrl)}` : ""}`}
          className="font-bold text-brand-600 dark:text-brand-400 hover:text-brand-700 dark:hover:text-brand-300 inline-flex items-center gap-1 transition-colors"
        >
          Đăng ký ngay <ArrowRight className="w-3.5 h-3.5" />
        </Link>
      </div>
    </div>
  );
}

export default function LoginPage() {
  return (
    <div className="min-h-[85vh] flex items-center justify-center py-12 px-4 bg-surface-warm dark:bg-stone-950/50">
      <Suspense
        fallback={
          <div className="w-full max-w-md bg-white dark:bg-stone-900 rounded-3xl p-8 border border-stone-200 animate-pulse h-96" />
        }
      >
        <LoginFormContent />
      </Suspense>
    </div>
  );
}
