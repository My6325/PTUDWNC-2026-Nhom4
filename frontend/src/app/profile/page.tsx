"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import {
  User,
  Mail,
  AtSign,
  ShieldCheck,
  Calendar,
  LogOut,
  ChevronRight,
  Save,
  CheckCircle2,
  AlertCircle,
  FileText,
  Link as LinkIcon,
  Sparkles,
  Camera,
} from "lucide-react";
import { authService } from "@/services/authService";
import { useAuthStore } from "@/stores/auth-store";
import { UserDto } from "@/types/auth";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";
import { Alert } from "@/components/ui/Alert";
import { Skeleton } from "@/components/ui/Skeleton";
import { extractApiErrorMessage } from "@/utils/apiError";
import { cn } from "@/utils/cn";

// Zod schema thẩm định form cập nhật hồ sơ
const profileSchema = z.object({
  displayName: z
    .string()
    .min(1, "Tên hiển thị không được để trống.")
    .max(100, "Tên hiển thị không được vượt quá 100 ký tự."),
  avatarUrl: z
    .string()
    .max(500, "Đường dẫn ảnh đại diện tối đa 500 ký tự.")
    .refine(
      (val) => {
        if (!val || val.trim() === "") return true;
        try {
          const url = new URL(val);
          return url.protocol === "http:" || url.protocol === "https:";
        } catch {
          return false;
        }
      },
      { message: "Ảnh đại diện phải là đường dẫn URL hợp lệ (bắt đầu bằng http:// hoặc https://)." }
    )
    .optional()
    .or(z.literal("")),
  bio: z
    .string()
    .max(500, "Tiểu sử không được vượt quá 500 ký tự.")
    .optional()
    .or(z.literal("")),
});

type ProfileFormData = z.infer<typeof profileSchema>;

export default function ProfilePage() {
  const router = useRouter();

  const [user, setUser] = useState<UserDto | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [fetchError, setFetchError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState<boolean>(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      displayName: "",
      avatarUrl: "",
      bio: "",
    },
  });

  const watchedAvatarUrl = watch("avatarUrl");
  const watchedBio = watch("bio") || "";

  // 1. Kiểm tra phiên đăng nhập và tải thông tin người dùng từ GET /auth/me
  useEffect(() => {
    let isMounted = true;

    const loadProfile = async () => {
      // Bảo vệ trang: Nếu chưa có access token, chuyển hướng về trang login
      if (!authService.isAuthenticated()) {
        router.push("/login?redirect=/profile");
        return;
      }

      setIsLoading(true);
      setFetchError(null);
      try {
        const userData = await authService.getCurrentUser();
        if (isMounted) {
          setUser(userData);
          useAuthStore.getState().setUser(userData);
          reset({
            displayName: userData.displayName || "",
            avatarUrl: userData.avatarUrl || "",
            bio: userData.bio || "",
          });
        }
      } catch (err: unknown) {
        if (!isMounted) return;
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 401) {
          // Phiên hết hạn hoặc không hợp lệ -> Hướng về login
          await authService.logout();
          useAuthStore.getState().logout();
          router.push("/login?redirect=/profile");
        } else {
          setFetchError(
            extractApiErrorMessage(
              err,
              "Không thể tải thông tin hồ sơ cá nhân. Vui lòng thử lại sau."
            )
          );
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    };

    loadProfile();

    return () => {
      isMounted = false;
    };
  }, [router, reset]);

  // 2. Cập nhật hồ sơ bằng PATCH /auth/profile
  const onSubmit = async (data: ProfileFormData) => {
    setSaveSuccess(false);
    setSaveError(null);

    try {
      const updatedUser = await authService.updateProfile({
        displayName: data.displayName.trim(),
        avatarUrl: data.avatarUrl?.trim() || null,
        bio: data.bio?.trim() || null,
      });

      setUser(updatedUser);
      useAuthStore.getState().setUser(updatedUser);
      reset({
        displayName: updatedUser.displayName || "",
        avatarUrl: updatedUser.avatarUrl || "",
        bio: updatedUser.bio || "",
      });

      // Chỉ hiển thị thành công SAU KHI API trả về kết quả
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 5000);
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 401) {
        await authService.logout();
        useAuthStore.getState().logout();
        router.push("/login?redirect=/profile");
      } else {
        setSaveError(
          extractApiErrorMessage(
            err,
            "Cập nhật hồ sơ không thành công. Vui lòng kiểm tra lại thông tin."
          )
        );
      }
    }
  };

  // 3. Đăng xuất
  const handleLogout = async () => {
    try {
      await authService.logout();
    } catch {
      // Bỏ qua lỗi mạng
    } finally {
      useAuthStore.getState().logout();
      router.push("/");
    }
  };

  // Format ngày tạo an toàn chống mismatch
  const formattedJoinDate = user?.createdAt
    ? (() => {
        try {
          return new Date(user.createdAt).toLocaleDateString("vi-VN", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
          });
        } catch {
          return "";
        }
      })()
    : "";

  // Render trạng thái Loading Skeleton
  if (isLoading) {
    return (
      <div className="min-h-screen bg-surface-warm dark:bg-stone-950/50 py-10">
        <div className="container-custom max-w-4xl mx-auto px-4 space-y-6">
          <Skeleton className="h-6 w-48 rounded-md" />
          <div className="rounded-3xl bg-white dark:bg-stone-900 border border-stone-200 dark:border-stone-800 p-8 space-y-6">
            <div className="flex items-center gap-4">
              <Skeleton className="h-20 w-20 rounded-full" />
              <div className="space-y-2 flex-1">
                <Skeleton className="h-7 w-48 rounded-md" />
                <Skeleton className="h-4 w-32 rounded-md" />
              </div>
            </div>
            <Skeleton className="h-10 w-full rounded-xl" />
            <Skeleton className="h-10 w-full rounded-xl" />
            <Skeleton className="h-24 w-full rounded-xl" />
          </div>
        </div>
      </div>
    );
  }

  // Render trạng thái Lỗi tải ban đầu
  if (fetchError && !user) {
    return (
      <div className="min-h-[70vh] flex items-center justify-center py-16 px-4">
        <div className="max-w-md w-full text-center space-y-5">
          <div className="w-16 h-16 mx-auto rounded-full bg-red-100 dark:bg-red-950/40 text-red-600 dark:text-red-400 flex items-center justify-center">
            <AlertCircle className="w-8 h-8" />
          </div>
          <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-100">
            Không thể tải hồ sơ
          </h1>
          <p className="text-sm text-stone-600 dark:text-stone-400">{fetchError}</p>
          <div className="flex justify-center gap-3 pt-2">
            <Button variant="primary" onClick={() => window.location.reload()}>
              Thử tải lại
            </Button>
            <Button variant="outline" href="/">
              Về trang chủ
            </Button>
          </div>
        </div>
      </div>
    );
  }

  // Preview ảnh đại diện người dùng
  const displayAvatar =
    watchedAvatarUrl && watchedAvatarUrl.trim() !== ""
      ? watchedAvatarUrl
      : user?.avatarUrl;

  return (
    <div className="min-h-screen bg-surface-warm dark:bg-stone-950/50 pb-20">
      {/* Breadcrumb Navigation */}
      <div className="border-b border-surface-border dark:border-stone-800 bg-white dark:bg-stone-900/80">
        <div className="container-custom max-w-4xl mx-auto px-4 py-3">
          <nav className="flex items-center gap-1.5 text-xs text-stone-500 dark:text-stone-400">
            <Link
              href="/"
              className="hover:text-stone-900 dark:hover:text-stone-100 transition-colors"
            >
              Trang chủ
            </Link>
            <ChevronRight className="h-3 w-3 shrink-0" />
            <span className="text-stone-800 dark:text-stone-200 font-medium">
              Hồ sơ cá nhân
            </span>
          </nav>
        </div>
      </div>

      <div className="container-custom max-w-4xl mx-auto px-4 pt-10">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {/* CỘT TRÁI: THẺ TỔNG QUAN TÀI KHOẢN */}
          <div className="md:col-span-1 space-y-6">
            <div className="rounded-3xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 shadow-sm p-6 text-center">
              {/* Ảnh đại diện Avatar */}
              <div className="relative mx-auto w-24 h-24 mb-4">
                {displayAvatar ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img
                    src={displayAvatar}
                    alt={user?.displayName || "Ảnh đại diện"}
                    className="w-24 h-24 rounded-full object-cover border-2 border-brand-500 shadow-md"
                  />
                ) : (
                  <div className="w-24 h-24 rounded-full bg-brand-100 dark:bg-brand-950/60 text-brand-700 dark:text-brand-300 flex items-center justify-center font-extrabold text-2xl border-2 border-brand-400">
                    {user?.displayName ? user.displayName.charAt(0).toUpperCase() : <User className="w-10 h-10" />}
                  </div>
                )}
                <div className="absolute bottom-0 right-0 p-1.5 bg-brand-600 text-white rounded-full shadow-xs" title="Ảnh đại diện">
                  <Camera className="w-3.5 h-3.5" />
                </div>
              </div>

              {/* Tên & Username */}
              <h2 className="text-xl font-bold text-stone-900 dark:text-stone-100 leading-tight">
                {user?.displayName || "Thành viên"}
              </h2>
              <p className="text-xs text-stone-500 dark:text-stone-400 font-mono mt-1">
                @{user?.userName}
              </p>

              {/* Danh sách vai trò (Roles) */}
              <div className="flex flex-wrap items-center justify-center gap-1.5 mt-3">
                {user?.roles && user.roles.length > 0 ? (
                  user.roles.map((role) => (
                    <Badge
                      key={role}
                      variant={role.toLowerCase() === "admin" ? "danger" : role.toLowerCase() === "author" ? "brand" : "default"}
                      size="sm"
                    >
                      <ShieldCheck className="w-3 h-3 mr-1" />
                      {role}
                    </Badge>
                  ))
                ) : (
                  <Badge variant="default" size="sm">
                    Thành viên
                  </Badge>
                )}
              </div>

              {/* Ngày tham gia */}
              {formattedJoinDate && (
                <div className="mt-5 pt-5 border-t border-stone-100 dark:border-stone-800 text-xs text-stone-500 dark:text-stone-400 flex items-center justify-center gap-1.5">
                  <Calendar className="w-3.5 h-3.5" />
                  <span>Tham gia từ <strong suppressHydrationWarning>{formattedJoinDate}</strong></span>
                </div>
              )}

              {/* Nút Đăng xuất */}
              <div className="mt-6 pt-2">
                <Button
                  variant="outline"
                  size="sm"
                  className="w-full text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950/30 border-red-200 dark:border-red-900/60"
                  onClick={handleLogout}
                  leftIcon={<LogOut className="w-4 h-4" />}
                >
                  Đăng xuất tài khoản
                </Button>
              </div>
            </div>

            {/* Quyền hạn dựa trên Role (Thông tin phân hệ) */}
            <div className="rounded-2xl bg-brand-50/50 dark:bg-brand-950/30 border border-brand-200/60 dark:border-brand-900/40 p-4 text-xs space-y-2 text-stone-700 dark:text-stone-300">
              <span className="font-bold flex items-center gap-1 text-brand-700 dark:text-brand-300">
                <Sparkles className="w-3.5 h-3.5" /> Quyền tài khoản
              </span>
              <p className="leading-relaxed">
                {user?.roles?.includes("Admin")
                  ? "Bạn có toàn quyền Quản trị hệ thống (FR-ADM, quản lý danh mục và duyệt bài viết)."
                  : user?.roles?.includes("Author")
                  ? "Bạn là Tác giả ẩm thực (FR-RCP, có quyền sáng tạo và xuất bản công thức nấu ăn)."
                  : "Bạn là Thành viên độc giả (FR-AUTH, có thể khám phá công thức và bình luận)."}
              </p>
            </div>
          </div>

          {/* CỘT PHẢI: FORM CHỈNH SỬA HỒ SƠ */}
          <div className="md:col-span-2">
            <div className="rounded-3xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 shadow-sm p-6 sm:p-8">
              <div className="mb-6 pb-4 border-b border-stone-100 dark:border-stone-800">
                <h1 className="text-xl sm:text-2xl font-bold text-stone-900 dark:text-stone-100">
                  Thông tin cá nhân
                </h1>
                <p className="text-xs sm:text-sm text-stone-500 dark:text-stone-400 mt-1">
                  Quản lý thông tin hiển thị của bạn trên các bài viết và bình luận
                </p>
              </div>

              {/* Thông báo cập nhật thành công */}
              {saveSuccess && (
                <Alert
                  variant="success"
                  title="Cập nhật thành công!"
                  className="mb-6"
                  dismissible
                  onDismiss={() => setSaveSuccess(false)}
                >
                  Thông tin hồ sơ cá nhân của bạn đã được lưu lại thành công vào hệ thống.
                </Alert>
              )}

              {/* Thông báo lỗi cập nhật */}
              {saveError && (
                <Alert
                  variant="error"
                  title="Không thể lưu thay đổi"
                  className="mb-6"
                  dismissible
                  onDismiss={() => setSaveError(null)}
                >
                  {saveError}
                </Alert>
              )}

              <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
                {/* 1. Email (Read-only / Không cho sửa) */}
                <div className="space-y-1">
                  <Input
                    label="Địa chỉ Email (Cố định)"
                    value={user?.email || ""}
                    disabled
                    readOnly
                    leftIcon={<Mail className="w-4 h-4 text-stone-400" />}
                    className="bg-stone-100/70 dark:bg-stone-800/70 text-stone-500 cursor-not-allowed"
                  />
                  <span className="text-[11px] text-stone-400 block px-1">
                    Email đăng nhập được bảo mật và không thể tự thay đổi qua trang này.
                  </span>
                </div>

                {/* 2. UserName (Read-only / Không cho sửa) */}
                <div className="space-y-1">
                  <Input
                    label="Tên tài khoản / Username (Cố định)"
                    value={user?.userName || ""}
                    disabled
                    readOnly
                    leftIcon={<AtSign className="w-4 h-4 text-stone-400" />}
                    className="bg-stone-100/70 dark:bg-stone-800/70 text-stone-500 cursor-not-allowed"
                  />
                  <span className="text-[11px] text-stone-400 block px-1">
                    Tên người dùng là mã định danh duy nhất của tài khoản.
                  </span>
                </div>

                {/* 3. Tên hiển thị (DisplayName - Cho phép sửa) */}
                <Input
                  label="Tên hiển thị công khai *"
                  placeholder="Ví dụ: Bếp trưởng Minh Hưng"
                  leftIcon={<User className="w-4 h-4 text-stone-400" />}
                  error={errors.displayName?.message}
                  {...register("displayName")}
                />

                {/* 4. Avatar URL (Cho phép sửa) */}
                <div className="space-y-1">
                  <Input
                    label="Đường dẫn ảnh đại diện (Avatar URL)"
                    placeholder="https://example.com/avatar.jpg"
                    leftIcon={<LinkIcon className="w-4 h-4 text-stone-400" />}
                    error={errors.avatarUrl?.message}
                    {...register("avatarUrl")}
                  />
                  <span className="text-[11px] text-stone-400 block px-1">
                    Nhập đường dẫn URL ảnh công khai (để trống nếu muốn sử dụng ảnh mặc định).
                  </span>
                </div>

                {/* 5. Giới thiệu / Bio (Cho phép sửa) */}
                <div className="space-y-1.5">
                  <label className="block text-sm font-semibold text-stone-700 dark:text-stone-300">
                    Tiểu sử ngắn gọn (Bio)
                  </label>
                  <div className="relative">
                    <textarea
                      rows={4}
                      placeholder="Chia sẻ đôi nét về niềm đam mê ẩm thực của bạn..."
                      maxLength={500}
                      className={cn(
                        "w-full px-4 py-3 text-sm rounded-xl border bg-white dark:bg-stone-900 border-stone-300 dark:border-stone-700 text-stone-900 dark:text-stone-100 placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500 transition-all",
                        errors.bio && "border-red-500 focus:border-red-500 focus:ring-red-500/20"
                      )}
                      {...register("bio")}
                    />
                  </div>
                  <div className="flex items-center justify-between text-xs text-stone-400 px-1">
                    {errors.bio?.message ? (
                      <span className="text-red-500">{errors.bio.message}</span>
                    ) : (
                      <span>Tối đa 500 ký tự</span>
                    )}
                    <span>{watchedBio.length}/500</span>
                  </div>
                </div>

                {/* Nút Lưu thay đổi */}
                <div className="pt-4 flex items-center justify-end gap-3 border-t border-stone-100 dark:border-stone-800">
                  <Button
                    type="submit"
                    variant="primary"
                    size="md"
                    isLoading={isSubmitting}
                    disabled={isSubmitting || !isDirty}
                    leftIcon={<Save className="w-4 h-4" />}
                  >
                    {isSubmitting ? "Đang lưu..." : "Lưu thay đổi"}
                  </Button>
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
