"use client";

import React, { useState, Suspense } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { ChefHat, Mail, Lock, User, AtSign, Eye, EyeOff, UserPlus, ArrowRight } from "lucide-react";
import { authService } from "@/services/authService";
import { useAuthStore } from "@/stores/auth-store";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Alert } from "@/components/ui/Alert";
import { extractApiErrorMessage } from "@/utils/apiError";

// Schema xác thực dữ liệu đăng ký khớp với backend FluentValidation
const registerSchema = z
  .object({
    displayName: z
      .string()
      .min(1, "Vui lòng nhập tên hiển thị của bạn.")
      .max(100, "Tên hiển thị tối đa 100 ký tự."),
    userName: z
      .string()
      .min(3, "Tên người dùng phải có ít nhất 3 ký tự.")
      .max(50, "Tên người dùng tối đa 50 ký tự.")
      .regex(
        /^[a-zA-Z0-9_.-]+$/,
        "Tên người dùng chỉ được chứa chữ cái, số, dấu gạch dưới, gạch ngang hoặc dấu chấm (không dấu, không khoảng trắng)."
      ),
    email: z
      .string()
      .min(1, "Vui lòng nhập địa chỉ email.")
      .email("Địa chỉ email không đúng định dạng."),
    password: z
      .string()
      .min(8, "Mật khẩu phải có tối thiểu 8 ký tự.")
      .regex(
        /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/,
        "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số."
      ),
    confirmPassword: z
      .string()
      .min(1, "Vui lòng xác nhận lại mật khẩu."),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Mật khẩu xác nhận không trùng khớp.",
    path: ["confirmPassword"],
  });

type RegisterFormData = z.infer<typeof registerSchema>;

function RegisterFormContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const redirectUrl = searchParams?.get("redirect") || "/";

  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      displayName: "",
      userName: "",
      email: "",
      password: "",
      confirmPassword: "",
    },
  });

  const onSubmit = async (data: RegisterFormData) => {
    setErrorMessage(null);
    try {
      const authRes = await authService.register({
        email: data.email.trim(),
        userName: data.userName.trim(),
        displayName: data.displayName.trim(),
        password: data.password,
      });

      // Cập nhật Zustand reactive store toàn cục
      useAuthStore.getState().login(authRes);

      // Điều hướng về trang trước đó hoặc trang chủ
      router.push(redirectUrl);
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 409) {
        setErrorMessage("Email hoặc tên người dùng này đã được sử dụng. Vui lòng chọn thông tin khác.");
      } else if (status === 422) {
        setErrorMessage(
          extractApiErrorMessage(
            err,
            "Dữ liệu đăng ký không đáp ứng quy chuẩn thẩm định. Vui lòng kiểm tra lại."
          )
        );
      } else if (status === 429) {
        setErrorMessage("Bạn gửi quá nhiều yêu cầu trong thời gian ngắn. Vui lòng đợi 1 phút rồi thử lại.");
      } else {
        setErrorMessage(
          extractApiErrorMessage(err, "Đăng ký tài khoản không thành công. Vui lòng thử lại sau.")
        );
      }
    }
  };

  return (
    <div className="w-full max-w-lg bg-white dark:bg-stone-900 rounded-3xl border border-surface-border dark:border-stone-800 shadow-xl p-8 sm:p-10">
      {/* Header */}
      <div className="text-center mb-8">
        <Link
          href="/"
          className="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-brand-600 text-white shadow-lg shadow-brand-600/25 mb-4 hover:scale-105 transition-transform"
        >
          <ChefHat className="w-8 h-8" />
        </Link>
        <h1 className="text-2xl sm:text-3xl font-extrabold text-stone-900 dark:text-stone-100 tracking-tight">
          Tạo tài khoản mới
        </h1>
        <p className="mt-2 text-sm text-stone-500 dark:text-stone-400">
          Gia nhập cộng đồng Culinary Blog để khám phá và chia sẻ công thức ẩm thực
        </p>
      </div>

      {/* Thông báo lỗi */}
      {errorMessage && (
        <Alert variant="error" className="mb-6" dismissible onDismiss={() => setErrorMessage(null)}>
          {errorMessage}
        </Alert>
      )}

      {/* Form đăng ký */}
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {/* Tên hiển thị */}
        <Input
          label="Tên hiển thị (Họ và Tên)"
          placeholder="Ví dụ: Nguyễn Văn An"
          autoComplete="name"
          leftIcon={<User className="w-4 h-4 text-stone-400" />}
          error={errors.displayName?.message}
          {...register("displayName")}
        />

        {/* Tên người dùng */}
        <Input
          label="Tên người dùng (Username)"
          placeholder="Ví dụ: vanan_cook"
          autoComplete="username"
          leftIcon={<AtSign className="w-4 h-4 text-stone-400" />}
          error={errors.userName?.message}
          {...register("userName")}
        />

        {/* Email */}
        <Input
          label="Địa chỉ Email"
          type="email"
          placeholder="vanan@example.com"
          autoComplete="email"
          leftIcon={<Mail className="w-4 h-4 text-stone-400" />}
          error={errors.email?.message}
          {...register("email")}
        />

        {/* Mật khẩu */}
        <Input
          label="Mật khẩu"
          type={showPassword ? "text" : "password"}
          placeholder="Tối thiểu 8 ký tự (chữ hoa, chữ thường, số)"
          autoComplete="new-password"
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

        {/* Xác nhận mật khẩu */}
        <Input
          label="Xác nhận mật khẩu"
          type={showConfirmPassword ? "text" : "password"}
          placeholder="Nhập lại mật khẩu phía trên"
          autoComplete="new-password"
          leftIcon={<Lock className="w-4 h-4 text-stone-400" />}
          rightIcon={
            <button
              type="button"
              onClick={() => setShowConfirmPassword(!showConfirmPassword)}
              className="text-stone-400 hover:text-stone-600 dark:hover:text-stone-300 transition-colors focus:outline-none"
              tabIndex={-1}
              aria-label={showConfirmPassword ? "Ẩn mật khẩu xác nhận" : "Hiện mật khẩu xác nhận"}
            >
              {showConfirmPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
            </button>
          }
          error={errors.confirmPassword?.message}
          {...register("confirmPassword")}
        />

        {/* Nút đăng ký */}
        <Button
          type="submit"
          variant="primary"
          size="lg"
          className="w-full mt-4 font-bold shadow-md shadow-brand-500/20"
          isLoading={isSubmitting}
          disabled={isSubmitting}
          leftIcon={<UserPlus className="w-5 h-5" />}
        >
          {isSubmitting ? "Đang tạo tài khoản..." : "Đăng ký tài khoản"}
        </Button>
      </form>

      {/* Footer chuyển sang trang Đăng nhập */}
      <div className="mt-8 pt-6 border-t border-stone-100 dark:border-stone-800 text-center text-sm text-stone-600 dark:text-stone-400">
        Đã có tài khoản?{" "}
        <Link
          href={`/login${redirectUrl !== "/" ? `?redirect=${encodeURIComponent(redirectUrl)}` : ""}`}
          className="font-bold text-brand-600 dark:text-brand-400 hover:text-brand-700 dark:hover:text-brand-300 inline-flex items-center gap-1 transition-colors"
        >
          Đăng nhập ngay <ArrowRight className="w-3.5 h-3.5" />
        </Link>
      </div>
    </div>
  );
}

export default function RegisterPage() {
  return (
    <div className="min-h-[85vh] flex items-center justify-center py-12 px-4 bg-surface-warm dark:bg-stone-950/50">
      <Suspense
        fallback={
          <div className="w-full max-w-lg bg-white dark:bg-stone-900 rounded-3xl p-8 border border-stone-200 animate-pulse h-[500px]" />
        }
      >
        <RegisterFormContent />
      </Suspense>
    </div>
  );
}
