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
  Lock,
  KeyRound,
  Eye,
  EyeOff,
  ChefHat,
  Send,
  Clock,
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

// Zod schema thẩm định form đổi mật khẩu
const changePasswordSchema = z
  .object({
    currentPassword: z
      .string()
      .min(1, "Vui lòng nhập mật khẩu hiện tại."),
    newPassword: z
      .string()
      .min(1, "Vui lòng nhập mật khẩu mới.")
      .min(8, "Mật khẩu mới phải có tối thiểu 8 ký tự.")
      .regex(
        /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/,
        "Mật khẩu mới phải bao gồm ít nhất một chữ hoa, một chữ thường và một chữ số."
      ),
    confirmNewPassword: z
      .string()
      .min(1, "Vui lòng xác nhận mật khẩu mới."),
  })
  .refine((data) => data.newPassword !== data.currentPassword, {
    message: "Mật khẩu mới không được trùng với mật khẩu hiện tại.",
    path: ["newPassword"],
  })
  .refine((data) => data.newPassword === data.confirmNewPassword, {
    message: "Xác nhận mật khẩu mới không trùng khớp.",
    path: ["confirmNewPassword"],
  });

type ChangePasswordFormData = z.infer<typeof changePasswordSchema>;

export default function ProfilePage() {
  const router = useRouter();

  const [user, setUser] = useState<UserDto | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [fetchError, setFetchError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState<boolean>(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  // Trạng thái yêu cầu nâng cấp lên Đầu bếp (Chính sách 1 Mục V)
  const [isRequestModalOpen, setIsRequestModalOpen] = useState(false);
  const [requestNote, setRequestNote] = useState("");
  const [isSubmittingRequest, setIsSubmittingRequest] = useState(false);
  const [requestSuccessMessage, setRequestSuccessMessage] = useState<string | null>(null);
  const [requestErrorMessage, setRequestErrorMessage] = useState<string | null>(null);

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

  const isReaderUser = Boolean(
    user &&
    !user.roles.some((r) => r.toLowerCase() === "admin" || r.toLowerCase() === "author")
  );

  const handleSendAuthorRequest = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmittingRequest(true);
    setRequestErrorMessage(null);
    setRequestSuccessMessage(null);
    try {
      const res = await authService.requestAuthorUpgrade(requestNote);
      setRequestSuccessMessage(res.message);
      setIsRequestModalOpen(false);
      if (user) {
        setUser({
          ...user,
          authorRequestStatus: "Pending",
          authorRequestNote: requestNote,
        });
      }
    } catch (err: unknown) {
      setRequestErrorMessage(
        extractApiErrorMessage(err, "Không thể gửi yêu cầu nâng cấp. Vui lòng thử lại sau.")
      );
    } finally {
      setIsSubmittingRequest(false);
    }
  };

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

  const [activeTab, setActiveTab] = useState<"profile" | "password">("profile");

  // State và Form quản lý Đổi mật khẩu
  const [passwordSuccess, setPasswordSuccess] = useState<boolean>(false);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  const {
    register: registerPassword,
    handleSubmit: handleSubmitPassword,
    reset: resetPasswordForm,
    formState: { errors: passwordErrors, isSubmitting: isSubmittingPassword },
  } = useForm<ChangePasswordFormData>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: {
      currentPassword: "",
      newPassword: "",
      confirmNewPassword: "",
    },
  });

  const onPasswordSubmit = async (data: ChangePasswordFormData) => {
    setPasswordSuccess(false);
    setPasswordError(null);
    try {
      await authService.changePassword({
        currentPassword: data.currentPassword,
        newPassword: data.newPassword,
        confirmNewPassword: data.confirmNewPassword,
      });

      setPasswordSuccess(true);
      resetPasswordForm();
      setTimeout(() => setPasswordSuccess(false), 5000);
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 401) {
        await authService.logout();
        useAuthStore.getState().logout();
        router.push("/login?redirect=/profile");
      } else {
        setPasswordError(
          extractApiErrorMessage(
            err,
            "Đổi mật khẩu không thành công. Vui lòng kiểm tra lại mật khẩu hiện tại."
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
                  user.roles.map((role) => {
                    const rLower = role.toLowerCase();
                    const isAdm = rLower === "admin";
                    const isAuth = rLower === "author";
                    const isUsr = rLower === "user";
                    const roleLabel = isAdm ? "Quản trị viên" : isAuth ? "Đầu bếp" : isUsr ? "Độc giả" : role;
                    return (
                      <Badge
                        key={role}
                        variant={isAdm ? "danger" : isAuth ? "brand" : "default"}
                        size="sm"
                      >
                        {isAdm ? (
                          <ShieldCheck className="w-3 h-3 mr-1" />
                        ) : isAuth ? (
                          <ChefHat className="w-3 h-3 mr-1" />
                        ) : (
                          <User className="w-3 h-3 mr-1" />
                        )}
                        {roleLabel}
                      </Badge>
                    );
                  })
                ) : (
                  <Badge variant="default" size="sm">
                    <User className="w-3 h-3 mr-1" />
                    Độc giả
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

            {/* THẺ ĐĂNG KÝ TRỞ THÀNH ĐẦU BẾP (CHÍNH SÁCH 1 MỤC V) */}
            {isReaderUser && (
              <div className="rounded-2xl bg-gradient-to-br from-amber-50 to-orange-50 dark:from-stone-900 dark:to-amber-950/30 border border-amber-200/80 dark:border-amber-900/50 p-5 space-y-3 shadow-xs">
                <div className="flex items-center gap-2 text-amber-700 dark:text-amber-400 font-bold text-sm">
                  <ChefHat className="w-4 h-4" />
                  <span>Trở thành Tác giả ẩm thực</span>
                </div>
                <p className="text-xs text-stone-600 dark:text-stone-300 leading-relaxed">
                  Bạn có công thức nấu ăn độc đáo muốn chia sẻ cùng cộng đồng Bếp Việt? Hãy gửi yêu cầu để được cấp quyền Đầu bếp!
                </p>

                {requestSuccessMessage && (
                  <Alert variant="success" className="text-xs p-2.5">
                    {requestSuccessMessage}
                  </Alert>
                )}
                {requestErrorMessage && (
                  <Alert variant="error" className="text-xs p-2.5">
                    {requestErrorMessage}
                  </Alert>
                )}

                {user?.authorRequestStatus === "Pending" ? (
                  <div className="flex items-center gap-2 p-2.5 rounded-xl bg-amber-100/70 dark:bg-amber-900/40 text-amber-800 dark:text-amber-300 text-xs font-medium">
                    <Clock className="w-4 h-4 shrink-0 text-amber-600 dark:text-amber-400" />
                    <span>Yêu cầu đang chờ Ban quản trị xét duyệt...</span>
                  </div>
                ) : user?.authorRequestStatus === "Approved" ? (
                  <div className="flex items-center gap-2 p-2.5 rounded-xl bg-emerald-100/70 dark:bg-emerald-900/40 text-emerald-800 dark:text-emerald-300 text-xs font-medium">
                    <CheckCircle2 className="w-4 h-4 shrink-0 text-emerald-600" />
                    <span>Yêu cầu đã được phê duyệt! Hãy tải lại trang.</span>
                  </div>
                ) : isRequestModalOpen ? (
                  <form onSubmit={handleSendAuthorRequest} className="space-y-3 pt-1">
                    <textarea
                      value={requestNote}
                      onChange={(e) => setRequestNote(e.target.value)}
                      placeholder="Giới thiệu đôi nét về đam mê, kinh nghiệm nấu ăn của bạn (tùy chọn)..."
                      className="w-full text-xs p-2.5 rounded-xl border border-amber-300 dark:border-amber-800 bg-white dark:bg-stone-900 text-stone-800 dark:text-stone-200 focus:outline-none focus:ring-2 focus:ring-amber-500 resize-none h-20"
                    />
                    <div className="flex items-center gap-2">
                      <Button
                        type="submit"
                        size="sm"
                        variant="default"
                        className="bg-amber-600 hover:bg-amber-700 text-white text-xs py-1.5 flex-1"
                        isLoading={isSubmittingRequest}
                        leftIcon={<Send className="w-3.5 h-3.5" />}
                      >
                        Gửi yêu cầu
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="ghost"
                        className="text-xs py-1.5"
                        onClick={() => setIsRequestModalOpen(false)}
                      >
                        Hủy
                      </Button>
                    </div>
                  </form>
                ) : (
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    className="w-full border-amber-300 hover:bg-amber-100/60 dark:border-amber-800 dark:hover:bg-amber-950/50 text-amber-800 dark:text-amber-300 text-xs font-semibold"
                    onClick={() => setIsRequestModalOpen(true)}
                    leftIcon={<ChefHat className="w-3.5 h-3.5" />}
                  >
                    Gửi yêu cầu nâng cấp Đầu bếp
                  </Button>
                )}
              </div>
            )}
            {/* Thanh điều hướng chức năng */}
            <div className="rounded-2xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 p-2 space-y-1 shadow-xs">
              <button
                type="button"
                onClick={() => setActiveTab("profile")}
                className={cn(
                  "w-full flex items-center gap-2.5 px-3.5 py-2.5 rounded-xl text-xs font-semibold transition-all text-left",
                  activeTab === "profile"
                    ? "bg-brand-500 text-white shadow-xs shadow-brand-500/20"
                    : "text-stone-600 dark:text-stone-400 hover:bg-stone-100 dark:hover:bg-stone-800/60"
                )}
              >
                <User className="w-4 h-4 shrink-0" />
                <span>Hồ sơ cá nhân</span>
              </button>
              <button
                type="button"
                onClick={() => setActiveTab("password")}
                className={cn(
                  "w-full flex items-center gap-2.5 px-3.5 py-2.5 rounded-xl text-xs font-semibold transition-all text-left",
                  activeTab === "password"
                    ? "bg-brand-500 text-white shadow-xs shadow-brand-500/20"
                    : "text-stone-600 dark:text-stone-400 hover:bg-stone-100 dark:hover:bg-stone-800/60"
                )}
              >
                <Lock className="w-4 h-4 shrink-0" />
                <span>Bảo mật & Đổi mật khẩu</span>
              </button>
            </div>
          </div>

          {/* CỘT PHẢI: FORM CHỈNH SỬA HỒ SƠ HOẶC ĐỔI MẬT KHẨU */}
          <div className="md:col-span-2">
            <div className="rounded-3xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 shadow-sm p-6 sm:p-8">
              {/* Tab Header Switcher */}
              <div className="flex items-center gap-3 border-b border-stone-200 dark:border-stone-800 mb-6 pb-2">
                <button
                  type="button"
                  onClick={() => setActiveTab("profile")}
                  className={cn(
                    "flex items-center gap-2 pb-3 px-3 text-sm font-bold border-b-2 transition-all -mb-[10px]",
                    activeTab === "profile"
                      ? "border-brand-600 text-brand-600 dark:border-brand-400 dark:text-brand-400"
                      : "border-transparent text-stone-500 hover:text-stone-800 dark:hover:text-stone-200"
                  )}
                >
                  <User className="w-4 h-4" />
                  Hồ sơ cá nhân
                </button>
                <button
                  type="button"
                  onClick={() => setActiveTab("password")}
                  className={cn(
                    "flex items-center gap-2 pb-3 px-3 text-sm font-bold border-b-2 transition-all -mb-[10px]",
                    activeTab === "password"
                      ? "border-brand-600 text-brand-600 dark:border-brand-400 dark:text-brand-400"
                      : "border-transparent text-stone-500 hover:text-stone-800 dark:hover:text-stone-200"
                  )}
                >
                  <Lock className="w-4 h-4" />
                  Bảo mật & Đổi mật khẩu
                </button>
              </div>

              {activeTab === "profile" ? (
                <div>
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
              ) : (
                /* TAB ĐỔI MẬT KHẨU */
                <div>
                  <div className="mb-6 pb-4 border-b border-stone-100 dark:border-stone-800">
                    <h1 className="text-xl sm:text-2xl font-bold text-stone-900 dark:text-stone-100">
                      Đổi mật khẩu tài khoản
                    </h1>
                    <p className="text-xs sm:text-sm text-stone-500 dark:text-stone-400 mt-1">
                      Cập nhật mật khẩu định kỳ giúp bảo vệ tài khoản Culinary Blog của bạn an toàn hơn
                    </p>
                  </div>

                  {/* Thông báo đổi mật khẩu thành công */}
                  {passwordSuccess && (
                    <Alert
                      variant="success"
                      title="Đổi mật khẩu thành công!"
                      className="mb-6"
                      dismissible
                      onDismiss={() => setPasswordSuccess(false)}
                    >
                      Mật khẩu của bạn đã được cập nhật thành công. Hãy sử dụng mật khẩu mới trong các lần đăng nhập tiếp theo.
                    </Alert>
                  )}

                  {/* Thông báo lỗi đổi mật khẩu */}
                  {passwordError && (
                    <Alert
                      variant="error"
                      title="Không thể đổi mật khẩu"
                      className="mb-6"
                      dismissible
                      onDismiss={() => setPasswordError(null)}
                    >
                      {passwordError}
                    </Alert>
                  )}

                  <form onSubmit={handleSubmitPassword(onPasswordSubmit)} className="space-y-5" noValidate>
                    {/* Mật khẩu hiện tại */}
                    <div className="space-y-1.5">
                      <Input
                        label="Mật khẩu hiện tại *"
                        type={showCurrentPassword ? "text" : "password"}
                        placeholder="Nhập mật khẩu bạn đang sử dụng"
                        autoComplete="current-password"
                        leftIcon={<Lock className="w-4 h-4 text-stone-400" />}
                        rightIcon={
                          <button
                            type="button"
                            onClick={() => setShowCurrentPassword(!showCurrentPassword)}
                            className="text-stone-400 hover:text-stone-600 dark:hover:text-stone-300 transition-colors focus:outline-none"
                            tabIndex={-1}
                            aria-label={showCurrentPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                          >
                            {showCurrentPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                          </button>
                        }
                        error={passwordErrors.currentPassword?.message}
                        {...registerPassword("currentPassword")}
                      />
                    </div>

                    {/* Mật khẩu mới */}
                    <div className="space-y-1.5">
                      <Input
                        label="Mật khẩu mới *"
                        type={showNewPassword ? "text" : "password"}
                        placeholder="Tối thiểu 8 ký tự (hoa, thường, số)"
                        autoComplete="new-password"
                        leftIcon={<KeyRound className="w-4 h-4 text-stone-400" />}
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
                        error={passwordErrors.newPassword?.message}
                        {...registerPassword("newPassword")}
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
                        error={passwordErrors.confirmNewPassword?.message}
                        {...registerPassword("confirmNewPassword")}
                      />
                    </div>

                    {/* Tiêu chí an toàn mật khẩu */}
                    <div className="p-4 bg-stone-50 dark:bg-stone-800/50 rounded-2xl border border-stone-200/60 dark:border-stone-700/60 text-xs text-stone-600 dark:text-stone-400 space-y-1.5">
                      <p className="font-bold text-stone-700 dark:text-stone-300">Quy tắc bảo mật mật khẩu:</p>
                      <ul className="list-disc list-inside space-y-1 pl-1">
                        <li>Độ dài tối thiểu 8 ký tự.</li>
                        <li>Bao gồm ít nhất một chữ in hoa (A-Z) và một chữ thường (a-z).</li>
                        <li>Bao gồm ít nhất một chữ số (0-9).</li>
                        <li>Không được trùng với mật khẩu hiện tại.</li>
                      </ul>
                    </div>

                    {/* Nút Đổi mật khẩu */}
                    <div className="pt-4 flex items-center justify-end gap-3 border-t border-stone-100 dark:border-stone-800">
                      <Button
                        type="submit"
                        variant="primary"
                        size="md"
                        isLoading={isSubmittingPassword}
                        disabled={isSubmittingPassword}
                        leftIcon={<Lock className="w-4 h-4" />}
                      >
                        {isSubmittingPassword ? "Đang xử lý..." : "Cập nhật mật khẩu"}
                      </Button>
                    </div>
                  </form>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
