"use client";

import React, { useEffect, useState, useMemo } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  Users,
  Shield,
  ShieldAlert,
  ChefHat,
  User as UserIcon,
  Search,
  RefreshCw,
  CheckCircle2,
  AlertCircle,
  Clock,
  Layers,
  ArrowRightLeft,
  ChevronRight,
  Filter,
  UserCheck,
  UserMinus,
  Sparkles,
} from "lucide-react";
import { authService } from "@/services/authService";
import { AdminUserItemDto } from "@/types/auth";
import { useAuthStore } from "@/stores/auth-store";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Alert } from "@/components/ui/Alert";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { extractApiErrorMessage } from "@/utils/apiError";
import { cn } from "@/utils/cn";

/**
 * Trang Quản trị Người dùng & Duyệt Vai trò (/admin/users)
 * Hiện thực chuẩn mực Đề xuất Nâng cao theo Chính sách 1 (RBAC):
 * - Xem danh sách toàn bộ người dùng và vai trò hiện tại
 * - Xem các yêu cầu nâng cấp lên Đầu bếp đang chờ duyệt (Pending)
 * - Thao tác 1-click chuyển đổi vai trò User <-> Author
 */
export default function AdminUsersPage() {
  const router = useRouter();
  const { user: currentUser, isAuthenticated, isLoading: isAuthLoading } = useAuthStore();

  const [users, setUsers] = useState<AdminUserItemDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState<string>("");
  const [roleFilter, setRoleFilter] = useState<string>("all");

  // State xác nhận đổi vai trò
  const [actionTarget, setActionTarget] = useState<{
    user: AdminUserItemDto;
    targetRole: "Author" | "User";
  } | null>(null);
  const [isProcessingAction, setIsProcessingAction] = useState<boolean>(false);

  const isAdmin = currentUser?.roles?.some((r) => r.toLowerCase() === "admin") ?? false;

  // 1. Tải danh sách người dùng
  const loadUsers = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await authService.getAdminUsers();
      setUsers(data);
    } catch (err: unknown) {
      setError(extractApiErrorMessage(err, "Không thể tải danh sách người dùng."));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (!isAuthLoading) {
      if (!isAuthenticated) {
        router.push("/login?redirect=/admin/users");
      } else if (!isAdmin) {
        // Không có quyền
      } else {
        loadUsers();
      }
    }
  }, [isAuthLoading, isAuthenticated, isAdmin, router]);

  // 2. Thao tác đổi vai trò
  const handleConfirmRoleChange = async () => {
    if (!actionTarget) return;

    setIsProcessingAction(true);
    setError(null);
    setSuccessMessage(null);

    try {
      const res = await authService.changeUserRole(actionTarget.user.id, actionTarget.targetRole);
      setSuccessMessage(res.message);
      setActionTarget(null);
      // Nạp lại danh sách
      await loadUsers();
    } catch (err: unknown) {
      setError(extractApiErrorMessage(err, "Lỗi khi cập nhật vai trò người dùng."));
    } finally {
      setIsProcessingAction(false);
    }
  };

  // 3. Thống kê nhanh
  const stats = useMemo(() => {
    const total = users.length;
    const readers = users.filter((u) => u.roles.some((r) => r.toLowerCase() === "user") && !u.roles.some((r) => r.toLowerCase() === "author" || r.toLowerCase() === "admin")).length;
    const authors = users.filter((u) => u.roles.some((r) => r.toLowerCase() === "author")).length;
    const pending = users.filter((u) => u.authorRequestStatus === "Pending").length;
    return { total, readers, authors, pending };
  }, [users]);

  // 4. Lọc dữ liệu hiển thị
  const filteredUsers = useMemo(() => {
    return users.filter((u) => {
      const matchesSearch =
        u.displayName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        u.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
        u.userName.toLowerCase().includes(searchTerm.toLowerCase());

      if (!matchesSearch) return false;

      if (roleFilter === "pending") {
        return u.authorRequestStatus === "Pending";
      }
      if (roleFilter === "author") {
        return u.roles.some((r) => r.toLowerCase() === "author");
      }
      if (roleFilter === "user") {
        return u.roles.some((r) => r.toLowerCase() === "user") && !u.roles.some((r) => r.toLowerCase() === "author" || r.toLowerCase() === "admin");
      }
      if (roleFilter === "admin") {
        return u.roles.some((r) => r.toLowerCase() === "admin");
      }

      return true;
    });
  }, [users, searchTerm, roleFilter]);

  // Màn hình loading phiên
  if (isAuthLoading) {
    return (
      <div className="min-h-screen bg-surface-warm dark:bg-stone-950/50 py-10">
        <div className="container-custom max-w-7xl mx-auto px-4 space-y-6">
          <Skeleton className="h-10 w-64 rounded-xl" />
          <Skeleton className="h-40 w-full rounded-3xl" />
          <Skeleton className="h-96 w-full rounded-3xl" />
        </div>
      </div>
    );
  }

  // Màn hình từ chối nếu không phải Admin
  if (!isAdmin) {
    return (
      <div className="min-h-[70vh] flex items-center justify-center py-16 px-4">
        <div className="max-w-md w-full text-center space-y-5 rounded-3xl bg-white dark:bg-stone-900 border border-stone-200 dark:border-stone-800 p-8 shadow-sm">
          <div className="w-16 h-16 mx-auto rounded-full bg-red-100 dark:bg-red-950/40 text-red-600 dark:text-red-400 flex items-center justify-center">
            <ShieldAlert className="w-8 h-8" />
          </div>
          <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-100">
            Truy cập bị từ chối
          </h1>
          <p className="text-sm text-stone-600 dark:text-stone-400">
            Khu vực này chỉ dành riêng cho Quản trị viên (Admin) của CulinaryBlog.
          </p>
          <div className="flex justify-center gap-3 pt-2">
            <Button variant="default" href="/">
              Về trang chủ
            </Button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-surface-warm dark:bg-stone-950/50 pb-20">
      {/* Breadcrumb Navigation */}
      <div className="border-b border-surface-border dark:border-stone-800 bg-white dark:bg-stone-900/80">
        <div className="container-custom max-w-7xl mx-auto px-4 py-3">
          <nav className="flex items-center gap-1.5 text-xs text-stone-500 dark:text-stone-400">
            <Link href="/" className="hover:text-stone-900 dark:hover:text-stone-100 transition-colors">
              Trang chủ
            </Link>
            <ChevronRight className="h-3 w-3 shrink-0" />
            <span className="text-stone-600 dark:text-stone-300">Quản trị hệ thống</span>
            <ChevronRight className="h-3 w-3 shrink-0" />
            <span className="text-stone-900 dark:text-stone-100 font-semibold">Người dùng & Phân quyền</span>
          </nav>
        </div>
      </div>

      <div className="container-custom max-w-7xl mx-auto px-4 pt-8 space-y-8">
        {/* Header & Sub-Navigation */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-3">
              <div className="w-12 h-12 rounded-2xl bg-brand-600 text-white flex items-center justify-center shadow-md shadow-brand-600/20">
                <Users className="w-6 h-6" />
              </div>
              <div>
                <h1 className="text-2xl sm:text-3xl font-extrabold text-stone-900 dark:text-stone-100 tracking-tight">
                  Quản lý Người dùng & Phân quyền
                </h1>
                <p className="text-xs sm:text-sm text-stone-500 dark:text-stone-400 mt-0.5">
                  Kiểm soát phân quyền RBAC, xét duyệt yêu cầu nâng cấp Đầu bếp và quản lý tài khoản.
                </p>
              </div>
            </div>
          </div>

          {/* Sub-Navigation Tabs */}
          <div className="flex items-center bg-stone-100 dark:bg-stone-900 p-1 rounded-2xl border border-stone-200 dark:border-stone-800 self-start md:self-auto">
            <Link
              href="/admin/categories"
              className="flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-semibold text-stone-600 dark:text-stone-400 hover:text-stone-900 dark:hover:text-stone-100 transition-colors"
            >
              <Layers className="w-3.5 h-3.5" />
              <span>Danh mục ẩm thực</span>
            </Link>
            <div className="flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-semibold bg-white dark:bg-stone-800 text-brand-600 dark:text-brand-400 shadow-xs">
              <Users className="w-3.5 h-3.5" />
              <span>Người dùng & RBAC</span>
            </div>
          </div>
        </div>

        {/* Thông báo thao tác */}
        {error && (
          <Alert variant="error" title="Thao tác thất bại" dismissible onDismiss={() => setError(null)}>
            {error}
          </Alert>
        )}
        {successMessage && (
          <Alert variant="success" title="Thao tác thành công" dismissible onDismiss={() => setSuccessMessage(null)}>
            {successMessage}
          </Alert>
        )}

        {/* 4 Thẻ thống kê nhanh */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="p-5 rounded-3xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 shadow-xs flex items-center gap-4">
            <div className="w-12 h-12 rounded-2xl bg-stone-100 dark:bg-stone-800 text-stone-700 dark:text-stone-300 flex items-center justify-center">
              <Users className="w-6 h-6" />
            </div>
            <div>
              <span className="block text-2xl font-black text-stone-900 dark:text-stone-100">{stats.total}</span>
              <span className="text-xs text-stone-500 font-medium">Tổng người dùng</span>
            </div>
          </div>

          <div className="p-5 rounded-3xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 shadow-xs flex items-center gap-4">
            <div className="w-12 h-12 rounded-2xl bg-amber-50 dark:bg-amber-950/50 text-amber-600 dark:text-amber-400 flex items-center justify-center">
              <Clock className="w-6 h-6" />
            </div>
            <div>
              <span className="block text-2xl font-black text-amber-600 dark:text-amber-400">{stats.pending}</span>
              <span className="text-xs text-stone-500 font-medium">Chờ duyệt Đầu bếp</span>
            </div>
          </div>

          <div className="p-5 rounded-3xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 shadow-xs flex items-center gap-4">
            <div className="w-12 h-12 rounded-2xl bg-orange-50 dark:bg-orange-950/50 text-orange-600 dark:text-orange-400 flex items-center justify-center">
              <ChefHat className="w-6 h-6" />
            </div>
            <div>
              <span className="block text-2xl font-black text-orange-600 dark:text-orange-400">{stats.authors}</span>
              <span className="text-xs text-stone-500 font-medium">Đầu bếp (Author)</span>
            </div>
          </div>

          <div className="p-5 rounded-3xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 shadow-xs flex items-center gap-4">
            <div className="w-12 h-12 rounded-2xl bg-brand-50 dark:bg-brand-950/50 text-brand-600 dark:text-brand-400 flex items-center justify-center">
              <UserIcon className="w-6 h-6" />
            </div>
            <div>
              <span className="block text-2xl font-black text-brand-600 dark:text-brand-400">{stats.readers}</span>
              <span className="text-xs text-stone-500 font-medium">Độc giả thường (User)</span>
            </div>
          </div>
        </div>

        {/* Thanh tìm kiếm & Bộ lọc */}
        <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
          <div className="relative flex-1 max-w-md">
            <Search className="w-4 h-4 text-stone-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Tìm theo tên hiển thị, email hoặc username..."
              className="w-full pl-10 pr-4 py-2.5 rounded-2xl border border-stone-200 dark:border-stone-800 bg-white dark:bg-stone-900 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 shadow-xs"
            />
          </div>

          <div className="flex items-center gap-2 overflow-x-auto pb-1 sm:pb-0">
            <Filter className="w-3.5 h-3.5 text-stone-400 shrink-0" />
            {[
              { id: "all", label: "Tất cả" },
              { id: "pending", label: `Chờ duyệt (${stats.pending})` },
              { id: "author", label: "Đầu bếp" },
              { id: "user", label: "Độc giả" },
              { id: "admin", label: "Admin" },
            ].map((f) => (
              <button
                key={f.id}
                type="button"
                onClick={() => setRoleFilter(f.id)}
                className={cn(
                  "px-3 py-1.5 rounded-xl text-xs font-semibold whitespace-nowrap transition-colors",
                  roleFilter === f.id
                    ? "bg-brand-600 text-white shadow-xs"
                    : "bg-white dark:bg-stone-900 text-stone-600 dark:text-stone-400 border border-stone-200 dark:border-stone-800 hover:bg-stone-50"
                )}
              >
                {f.label}
              </button>
            ))}
            <Button
              variant="outline"
              size="sm"
              onClick={loadUsers}
              isLoading={isLoading}
              leftIcon={<RefreshCw className="w-3.5 h-3.5" />}
              className="ml-auto"
            >
              Làm mới
            </Button>
          </div>
        </div>

        {/* Bảng danh sách người dùng */}
        <div className="rounded-3xl bg-white dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 shadow-sm overflow-hidden">
          {isLoading ? (
            <div className="p-8 space-y-4">
              {[1, 2, 3, 4, 5].map((i) => (
                <Skeleton key={i} className="h-16 w-full rounded-2xl" />
              ))}
            </div>
          ) : filteredUsers.length === 0 ? (
            <div className="py-16 text-center">
              <EmptyState
                icon={<Users className="w-8 h-8" />}
                title="Không tìm thấy người dùng"
                description={
                  searchTerm
                    ? `Không có kết quả nào khớp với từ khóa "${searchTerm}".`
                    : "Không có người dùng nào thuộc bộ lọc này."
                }
              />
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-stone-100 dark:border-stone-800 text-[11px] font-bold text-stone-400 uppercase tracking-wider bg-stone-50/50 dark:bg-stone-900/50">
                    <th className="py-4 px-6">Người dùng</th>
                    <th className="py-4 px-6">Email & Ngày tham gia</th>
                    <th className="py-4 px-6">Vai trò hiện tại</th>
                    <th className="py-4 px-6">Yêu cầu Đầu bếp</th>
                    <th className="py-4 px-6 text-right">Thao tác phân quyền</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-stone-100 dark:divide-stone-800 text-xs text-stone-700 dark:text-stone-300">
                  {filteredUsers.map((u) => {
                    const isAdm = u.roles.some((r) => r.toLowerCase() === "admin");
                    const isAuth = u.roles.some((r) => r.toLowerCase() === "author");
                    const hasPendingRequest = u.authorRequestStatus === "Pending";

                    return (
                      <tr
                        key={u.id}
                        className={cn(
                          "hover:bg-stone-50/60 dark:hover:bg-stone-800/40 transition-colors",
                          hasPendingRequest && "bg-amber-50/30 dark:bg-amber-950/10"
                        )}
                      >
                        {/* 1. Người dùng (Avatar & DisplayName) */}
                        <td className="py-4 px-6">
                          <div className="flex items-center gap-3">
                            {u.avatarUrl ? (
                              // eslint-disable-next-line @next/next/no-img-element
                              <img
                                src={u.avatarUrl}
                                alt={u.displayName}
                                className="w-9 h-9 rounded-full object-cover border border-stone-200"
                              />
                            ) : (
                              <div className="w-9 h-9 rounded-full bg-brand-100 dark:bg-brand-950/60 text-brand-700 dark:text-brand-300 font-bold flex items-center justify-center text-sm">
                                {u.displayName.charAt(0).toUpperCase()}
                              </div>
                            )}
                            <div>
                              <span className="block font-bold text-stone-900 dark:text-stone-100 text-sm">
                                {u.displayName}
                              </span>
                              <span className="block text-stone-400 font-mono text-[11px]">
                                @{u.userName}
                              </span>
                            </div>
                          </div>
                        </td>

                        {/* 2. Email & Ngày tham gia */}
                        <td className="py-4 px-6">
                          <span className="block font-medium text-stone-800 dark:text-stone-200">{u.email}</span>
                          <span className="block text-stone-400 text-[11px] mt-0.5">
                            {new Date(u.createdAt).toLocaleDateString("vi-VN")}
                          </span>
                        </td>

                        {/* 3. Vai trò hiện tại */}
                        <td className="py-4 px-6">
                          <div className="flex flex-wrap items-center gap-1">
                            {u.roles.map((r) => {
                              const rLower = r.toLowerCase();
                              return (
                                <Badge
                                  key={r}
                                  variant={rLower === "admin" ? "danger" : rLower === "author" ? "brand" : "default"}
                                  size="sm"
                                >
                                  {rLower === "admin" ? (
                                    <Shield className="w-3 h-3 mr-1" />
                                  ) : rLower === "author" ? (
                                    <ChefHat className="w-3 h-3 mr-1" />
                                  ) : (
                                    <UserIcon className="w-3 h-3 mr-1" />
                                  )}
                                  {rLower === "admin" ? "Admin" : rLower === "author" ? "Đầu bếp" : "Độc giả"}
                                </Badge>
                              );
                            })}
                          </div>
                        </td>

                        {/* 4. Trạng thái yêu cầu Đầu bếp */}
                        <td className="py-4 px-6">
                          {hasPendingRequest ? (
                            <div className="space-y-1">
                              <Badge variant="warning" size="sm" className="font-bold">
                                <Clock className="w-3 h-3 mr-1 animate-pulse" />
                                Chờ xét duyệt
                              </Badge>
                              {u.authorRequestNote && (
                                <p className="text-[11px] text-stone-500 italic max-w-xs truncate" title={u.authorRequestNote}>
                                  &ldquo;{u.authorRequestNote}&rdquo;
                                </p>
                              )}
                            </div>
                          ) : u.authorRequestStatus === "Approved" ? (
                            <span className="text-[11px] font-semibold text-emerald-600 flex items-center gap-1">
                              <CheckCircle2 className="w-3.5 h-3.5" /> Đã được duyệt
                            </span>
                          ) : (
                            <span className="text-stone-400 text-xs">—</span>
                          )}
                        </td>

                        {/* 5. Nút Thao tác Phân quyền */}
                        <td className="py-4 px-6 text-right">
                          {isAdm ? (
                            <span className="text-[11px] font-semibold text-stone-400 italic">
                              Quản trị viên tối cao
                            </span>
                          ) : (
                            <div className="flex items-center justify-end gap-2">
                              {hasPendingRequest && (
                                <Button
                                  size="sm"
                                  variant="default"
                                  className="bg-emerald-600 hover:bg-emerald-700 text-white font-semibold py-1 px-3 text-xs"
                                  leftIcon={<UserCheck className="w-3.5 h-3.5" />}
                                  onClick={() => setActionTarget({ user: u, targetRole: "Author" })}
                                >
                                  Duyệt Đầu bếp
                                </Button>
                              )}

                              {!isAuth ? (
                                <Button
                                  size="sm"
                                  variant="outline"
                                  className="border-brand-300 text-brand-700 hover:bg-brand-50 text-xs py-1 px-2.5"
                                  leftIcon={<ChefHat className="w-3.5 h-3.5" />}
                                  onClick={() => setActionTarget({ user: u, targetRole: "Author" })}
                                >
                                  Cấp quyền Đầu bếp
                                </Button>
                              ) : (
                                <Button
                                  size="sm"
                                  variant="ghost"
                                  className="text-stone-500 hover:text-red-600 hover:bg-red-50 text-xs py-1 px-2.5"
                                  leftIcon={<UserMinus className="w-3.5 h-3.5" />}
                                  onClick={() => setActionTarget({ user: u, targetRole: "User" })}
                                >
                                  Chuyển về Độc giả
                                </Button>
                              )}
                            </div>
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {/* Modal Xác nhận Đổi vai trò */}
      {actionTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs">
          <div className="w-full max-w-md rounded-3xl bg-white dark:bg-stone-900 border border-stone-200 dark:border-stone-800 p-6 shadow-2xl space-y-5 animate-in fade-in zoom-in-95 duration-150">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-brand-100 dark:bg-brand-950/60 text-brand-700 dark:text-brand-300 flex items-center justify-center">
                <ArrowRightLeft className="w-5 h-5" />
              </div>
              <div>
                <h3 className="font-bold text-base text-stone-900 dark:text-stone-100">
                  Xác nhận thay đổi vai trò
                </h3>
                <p className="text-xs text-stone-500">
                  Người dùng: <strong className="text-stone-800 dark:text-stone-200">{actionTarget.user.displayName}</strong>
                </p>
              </div>
            </div>

            <p className="text-xs sm:text-sm text-stone-600 dark:text-stone-300 leading-relaxed">
              Bạn có chắc chắn muốn chuyển vai trò của người dùng này sang{" "}
              <strong className={actionTarget.targetRole === "Author" ? "text-brand-600 font-bold" : "text-stone-800 font-bold"}>
                {actionTarget.targetRole === "Author" ? "Đầu bếp (Author)" : "Độc giả (User)"}
              </strong>
              ?{" "}
              {actionTarget.targetRole === "Author"
                ? "Người dùng sẽ có quyền soạn thảo và đăng tải công thức món ăn mới."
                : "Người dùng sẽ chỉ có quyền xem, bình luận và đánh giá công thức."}
            </p>

            <div className="flex items-center justify-end gap-2.5 pt-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setActionTarget(null)}
                disabled={isProcessingAction}
              >
                Hủy bỏ
              </Button>
              <Button
                variant="default"
                size="sm"
                className={actionTarget.targetRole === "Author" ? "bg-brand-600 hover:bg-brand-700" : "bg-stone-800 hover:bg-stone-900"}
                onClick={handleConfirmRoleChange}
                isLoading={isProcessingAction}
              >
                Xác nhận thay đổi
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
