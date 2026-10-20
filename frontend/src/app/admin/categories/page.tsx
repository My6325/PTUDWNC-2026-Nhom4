"use client";

import React, { useEffect, useState, useMemo } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  FolderHeart,
  Plus,
  Pencil,
  Trash2,
  ExternalLink,
  Search,
  RefreshCw,
  ShieldCheck,
  ShieldAlert,
  AlertCircle,
  CheckCircle2,
  X,
  Image as ImageIcon,
  ChevronRight,
  Layers,
  Calendar,
  Clock,
  RotateCcw,
  Info,
} from "lucide-react";
import { categoryService } from "@/services/categoryService";
import { CategoryDto, CreateCategoryRequest, UpdateCategoryRequest } from "@/types/category";
import { useAuthStore } from "@/stores/auth-store";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Alert } from "@/components/ui/Alert";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { extractApiErrorMessage } from "@/utils/apiError";
import { cn } from "@/utils/cn";

/**
 * Trang Quản trị Danh mục Ẩm thực (/admin/categories) dành riêng cho tài khoản ADMIN.
 * Hiện thực chuẩn mực các chức năng trong WBS của Nhóm trưởng (TV1):
 * - FR-CAT-003: Tạo danh mục mới
 * - FR-CAT-004: Cập nhật thông tin danh mục (tên, mô tả, ảnh, thứ tự - bảo lưu Slug SEO)
 * - FR-CAT-005: Xóa mềm danh mục (Soft Delete 30 ngày, kiểm tra toàn vẹn CSDL)
 * - Quản lý Thùng rác (Recycle Bin) & Khôi phục danh mục (Soft Delete Restore)
 */
export default function AdminCategoriesPage() {
  const router = useRouter();
  const { user, isAuthenticated, isLoading: isAuthLoading } = useAuthStore();

  // Tab: "active" (Đang hoạt động) hoặc "trash" (Thùng rác)
  const [activeTab, setActiveTab] = useState<"active" | "trash">("active");

  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [trashCategories, setTrashCategories] = useState<CategoryDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState<string>("");

  // Modals state
  const [isCreateModalOpen, setIsCreateModalOpen] = useState<boolean>(false);
  const [editingCategory, setEditingCategory] = useState<CategoryDto | null>(null);
  const [deletingCategory, setDeletingCategory] = useState<CategoryDto | null>(null);
  const [restoringCategory, setRestoringCategory] = useState<CategoryDto | null>(null);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [modalError, setModalError] = useState<string | null>(null);

  // Form states
  const [formName, setFormName] = useState<string>("");
  const [formDescription, setFormDescription] = useState<string>("");
  const [formImageUrl, setFormImageUrl] = useState<string>("");
  const [formOrderIndex, setFormOrderIndex] = useState<number>(0);

  // Kiểm tra quyền Admin an toàn
  const isAdmin = useMemo(() => {
    if (!user || !user.roles) return false;
    return user.roles.some((r) => r.toLowerCase() === "admin");
  }, [user]);

  // Format ngày tháng tiếng Việt an toàn chống lỗi SSR
  const formatDateSafe = (dateInput?: string | null): string => {
    if (!dateInput) return "";
    try {
      const d = new Date(dateInput);
      if (isNaN(d.getTime())) return "";
      return d.toLocaleDateString("vi-VN", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
      });
    } catch {
      return "";
    }
  };

  // Tính số ngày còn lại trước khi xóa vĩnh viễn (30 ngày retention)
  const getRemainingDays = (deletedAt?: string | null): number => {
    if (!deletedAt) return 30;
    try {
      const delTime = new Date(deletedAt).getTime();
      const expireTime = delTime + 30 * 24 * 60 * 60 * 1000;
      const diff = Math.ceil((expireTime - Date.now()) / (1000 * 60 * 60 * 24));
      return Math.max(0, diff);
    } catch {
      return 30;
    }
  };

  // Nạp danh sách danh mục hoạt động và thùng rác
  const loadAllData = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [activeData, trashData] = await Promise.all([
        categoryService.getCategories().catch(() => []),
        categoryService.getTrashCategories().catch(() => []),
      ]);

      const sortedActive = [...(activeData || [])].sort((a, b) => a.orderIndex - b.orderIndex);
      setCategories(sortedActive);
      setTrashCategories(trashData || []);
    } catch (err: unknown) {
      setError(extractApiErrorMessage(err, "Không thể tải danh sách danh mục ẩm thực."));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (!isAuthLoading && isAuthenticated && isAdmin) {
      loadAllData();
    }
  }, [isAuthLoading, isAuthenticated, isAdmin]);

  // Lọc danh mục hiển thị theo Tab và ô tìm kiếm
  const displayedCategories = useMemo(() => {
    const sourceList = activeTab === "active" ? categories : trashCategories;
    if (!searchTerm.trim()) return sourceList;
    const term = searchTerm.toLowerCase().trim();
    return sourceList.filter(
      (cat) =>
        cat.name.toLowerCase().includes(term) ||
        cat.slug.toLowerCase().includes(term) ||
        (cat.description && cat.description.toLowerCase().includes(term))
    );
  }, [activeTab, categories, trashCategories, searchTerm]);

  // Mở Modal Thêm mới (FR-CAT-003)
  const handleOpenCreateModal = () => {
    setFormName("");
    setFormDescription("");
    setFormImageUrl("");
    const nextOrder = categories.length > 0 ? Math.max(...categories.map((c) => c.orderIndex)) + 1 : 1;
    setFormOrderIndex(nextOrder);
    setModalError(null);
    setIsCreateModalOpen(true);
  };

  // Submit Thêm mới (FR-CAT-003)
  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formName.trim() || formName.trim().length < 2) {
      setModalError("Tên danh mục phải có từ 2 đến 50 ký tự.");
      return;
    }

    setIsSubmitting(true);
    setModalError(null);
    try {
      const request: CreateCategoryRequest = {
        name: formName.trim(),
        description: formDescription.trim() || undefined,
        imageUrl: formImageUrl.trim() || undefined,
      };

      await categoryService.createCategory(request);
      setIsCreateModalOpen(false);
      setSuccessMessage(`Đã tạo thành công danh mục "${formName.trim()}"!`);
      setTimeout(() => setSuccessMessage(null), 4000);
      await loadAllData();
    } catch (err: unknown) {
      setModalError(extractApiErrorMessage(err, "Lỗi khi tạo mới danh mục ẩm thực."));
    } finally {
      setIsSubmitting(false);
    }
  };

  // Mở Modal Chỉnh sửa (FR-CAT-004)
  const handleOpenEditModal = (cat: CategoryDto) => {
    setEditingCategory(cat);
    setFormName(cat.name);
    setFormDescription(cat.description || "");
    setFormImageUrl(cat.imageUrl || "");
    setFormOrderIndex(cat.orderIndex);
    setModalError(null);
  };

  // Submit Chỉnh sửa (FR-CAT-004)
  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingCategory) return;
    if (!formName.trim() || formName.trim().length < 2) {
      setModalError("Tên danh mục phải có từ 2 đến 100 ký tự.");
      return;
    }

    setIsSubmitting(true);
    setModalError(null);
    try {
      const request: UpdateCategoryRequest = {
        name: formName.trim(),
        description: formDescription.trim() || undefined,
        imageUrl: formImageUrl.trim() || undefined,
        orderIndex: Number(formOrderIndex) || 0,
      };

      await categoryService.updateCategory(editingCategory.id, request);
      setEditingCategory(null);
      setSuccessMessage(`Đã cập nhật thành công danh mục "${formName.trim()}"!`);
      setTimeout(() => setSuccessMessage(null), 4000);
      await loadAllData();
    } catch (err: unknown) {
      setModalError(extractApiErrorMessage(err, "Lỗi khi cập nhật danh mục ẩm thực."));
    } finally {
      setIsSubmitting(false);
    }
  };

  // Mở Modal Xóa (FR-CAT-005)
  const handleOpenDeleteModal = (cat: CategoryDto) => {
    setDeletingCategory(cat);
    setModalError(null);
  };

  // Xác nhận Xóa mềm (FR-CAT-005)
  const handleDeleteSubmit = async () => {
    if (!deletingCategory) return;

    setIsSubmitting(true);
    setModalError(null);
    try {
      await categoryService.deleteCategory(deletingCategory.id);
      const deletedName = deletingCategory.name;
      setDeletingCategory(null);
      setSuccessMessage(`Đã chuyển danh mục "${deletedName}" vào thùng rác thành công!`);
      setTimeout(() => setSuccessMessage(null), 4000);
      await loadAllData();
    } catch (err: unknown) {
      setModalError(
        extractApiErrorMessage(
          err,
          "Không thể xóa danh mục này vì vẫn còn công thức bài viết đang liên kết. Vui lòng chuyển hoặc xóa các bài viết trước."
        )
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  // Mở Modal Khôi phục (Restore)
  const handleOpenRestoreModal = (cat: CategoryDto) => {
    setRestoringCategory(cat);
    setModalError(null);
  };

  // Xác nhận Khôi phục từ Thùng rác (Restore Soft Delete)
  const handleRestoreSubmit = async () => {
    if (!restoringCategory) return;

    setIsSubmitting(true);
    setModalError(null);
    try {
      await categoryService.restoreCategory(restoringCategory.id);
      const restoredName = restoringCategory.name;
      setRestoringCategory(null);
      setSuccessMessage(`Đã khôi phục thành công danh mục "${restoredName}" về trạng thái hoạt động!`);
      setTimeout(() => setSuccessMessage(null), 4000);
      await loadAllData();
    } catch (err: unknown) {
      setModalError(extractApiErrorMessage(err, "Không thể khôi phục danh mục ẩm thực."));
    } finally {
      setIsSubmitting(false);
    }
  };

  // 1. Trạng thái kiểm tra quyền đăng nhập
  if (isAuthLoading) {
    return (
      <div className="container-custom max-w-7xl mx-auto py-12 px-4 space-y-6">
        <Skeleton className="h-10 w-1/3" />
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-96 w-full" />
      </div>
    );
  }

  // 2. Chặn truy cập nếu không phải Admin
  if (!isAuthenticated || !isAdmin) {
    return (
      <div className="min-h-[80vh] flex items-center justify-center px-4 py-16 bg-surface-warm dark:bg-stone-950">
        <div className="max-w-md w-full p-8 rounded-3xl bg-white dark:bg-stone-900 border border-stone-200 dark:border-stone-800 shadow-xl text-center space-y-6">
          <div className="w-16 h-16 mx-auto rounded-2xl bg-amber-100 dark:bg-amber-950/60 text-amber-600 dark:text-amber-400 flex items-center justify-center">
            <ShieldAlert className="w-8 h-8" />
          </div>
          <div className="space-y-2">
            <h2 className="text-2xl font-black tracking-tight text-stone-900 dark:text-stone-100">
              Yêu cầu quyền Quản trị viên
            </h2>
            <p className="text-sm text-stone-600 dark:text-stone-400 leading-relaxed">
              Trang này chỉ dành riêng cho tài khoản có vai trò <strong>Admin</strong>. Tài khoản tác giả (Author) hoặc khách vãng lai không được phép truy cập.
            </p>
          </div>
          <div className="pt-2 space-y-3">
            <Button
              variant="primary"
              className="w-full"
              onClick={() => router.push("/login?redirect=/admin/categories")}
            >
              Đăng nhập bằng tài khoản Admin
            </Button>
            <Button
              variant="outline"
              className="w-full"
              onClick={() => router.push("/")}
            >
              Về Trang chủ
            </Button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-surface-warm dark:bg-stone-950/40 pb-24">
      {/* Header thanh điều hướng */}
      <div className="border-b border-surface-border bg-white dark:bg-stone-900/90 backdrop-blur sticky top-16 z-20">
        <div className="container-custom max-w-7xl mx-auto px-4 py-4 sm:px-6">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <div className="space-y-1">
              <div className="flex items-center gap-2 text-xs text-stone-500 dark:text-stone-400">
                <Link href="/" className="hover:text-stone-900 dark:hover:text-stone-100">
                  Trang chủ
                </Link>
                <ChevronRight className="w-3.5 h-3.5" />
                <span className="font-medium text-stone-900 dark:text-stone-200">Khu vực Quản trị</span>
                <ChevronRight className="w-3.5 h-3.5" />
                <span className="text-brand-600 dark:text-brand-400 font-semibold">Danh mục Ẩm thực</span>
              </div>
              <div className="flex items-center gap-3">
                <h1 className="text-2xl sm:text-3xl font-black tracking-tight text-stone-900 dark:text-white flex items-center gap-2.5">
                  <FolderHeart className="w-7 h-7 text-brand-600" />
                  Quản trị Danh mục Ẩm thực
                </h1>
                <Badge variant="brand" size="sm" className="hidden sm:inline-flex">
                  <ShieldCheck className="w-3.5 h-3.5 mr-1" />
                  Admin Mode
                </Badge>
              </div>
            </div>

            <div className="flex items-center gap-2.5 shrink-0">
              <Button
                variant="outline"
                size="md"
                onClick={loadAllData}
                disabled={isLoading}
                leftIcon={<RefreshCw className={cn("w-4 h-4", isLoading && "animate-spin")} />}
              >
                Làm mới
              </Button>
              <Button
                variant="primary"
                size="md"
                onClick={handleOpenCreateModal}
                leftIcon={<Plus className="w-4 h-4" />}
                className="shadow-sm shadow-brand-600/30"
              >
                Thêm Danh mục Mới
              </Button>
            </div>
          </div>
        </div>
      </div>

      <main className="container-custom max-w-7xl mx-auto px-4 sm:px-6 py-8 space-y-6">
        {/* Thông báo thành công */}
        {successMessage && (
          <Alert variant="success" icon={<CheckCircle2 className="w-5 h-5 text-emerald-600" />} dismissible onDismiss={() => setSuccessMessage(null)}>
            {successMessage}
          </Alert>
        )}

        {/* Thông báo lỗi tổng quát */}
        {error && (
          <Alert variant="error" icon={<AlertCircle className="w-5 h-5 text-red-600" />} dismissible onDismiss={() => setError(null)}>
            {error}
          </Alert>
        )}

        {/* Thanh chuyển đổi Tab: Đang hoạt động vs Thùng rác */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-stone-200 dark:border-stone-800 pb-2">
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={() => setActiveTab("active")}
              className={cn(
                "flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-bold transition-all",
                activeTab === "active"
                  ? "bg-brand-600 text-white shadow-sm shadow-brand-600/30"
                  : "bg-white dark:bg-stone-900 text-stone-600 dark:text-stone-300 hover:bg-stone-100 dark:hover:bg-stone-800 border border-stone-200/80 dark:border-stone-800"
              )}
            >
              <Layers className="w-4 h-4" />
              <span>Đang hoạt động</span>
              <span
                className={cn(
                  "px-2 py-0.5 rounded-full text-xs font-semibold",
                  activeTab === "active" ? "bg-white/20 text-white" : "bg-stone-100 dark:bg-stone-800 text-stone-600 dark:text-stone-300"
                )}
              >
                {categories.length}
              </span>
            </button>

            <button
              type="button"
              onClick={() => setActiveTab("trash")}
              className={cn(
                "flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-bold transition-all",
                activeTab === "trash"
                  ? "bg-stone-900 dark:bg-stone-100 text-white dark:text-stone-900 shadow-sm"
                  : "bg-white dark:bg-stone-900 text-stone-600 dark:text-stone-300 hover:bg-stone-100 dark:hover:bg-stone-800 border border-stone-200/80 dark:border-stone-800"
              )}
            >
              <Trash2 className="w-4 h-4 text-red-500" />
              <span>Thùng rác</span>
              <span
                className={cn(
                  "px-2 py-0.5 rounded-full text-xs font-semibold",
                  activeTab === "trash"
                    ? "bg-red-500 text-white"
                    : "bg-red-50 dark:bg-red-950/50 text-red-600 dark:text-red-400"
                )}
              >
                {trashCategories.length}
              </span>
            </button>
          </div>

          {/* Ô tìm kiếm nhanh */}
          <div className="relative w-full sm:w-72">
            <Search className="w-4 h-4 text-stone-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              placeholder="Tìm theo tên danh mục..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-10 pr-8 py-2 text-sm rounded-xl border border-stone-200 dark:border-stone-700 bg-white dark:bg-stone-800 focus:outline-none focus:ring-2 focus:ring-brand-500"
            />
            {searchTerm && (
              <button
                type="button"
                onClick={() => setSearchTerm("")}
                className="absolute right-2.5 top-1/2 -translate-y-1/2 text-stone-400 hover:text-stone-600"
              >
                <X className="w-4 h-4" />
              </button>
            )}
          </div>
        </div>

        {/* Khung cảnh báo chuyên nghiệp khi ở Tab Thùng rác */}
        {activeTab === "trash" && (
          <div className="p-4 rounded-2xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200/80 dark:border-amber-900/60 flex items-start gap-3 text-sm text-amber-900 dark:text-amber-200 animate-in fade-in">
            <Info className="w-5 h-5 text-amber-600 dark:text-amber-400 shrink-0 mt-0.5" />
            <div className="space-y-1">
              <div className="font-bold">Chính sách lưu vết và xóa mềm (Soft Delete Retention):</div>
              <p className="text-xs text-amber-800 dark:text-amber-300 leading-relaxed">
                Các danh mục bị xóa sẽ được lưu giữ tạm thời trong <strong>Thùng rác trong vòng 30 ngày</strong>. Trong thời gian này, bạn có quyền <strong>Khôi phục (Restore)</strong> danh mục về trạng thái hoạt động bình thường. Sau 30 ngày, hệ thống sẽ tự động quét dọn và xóa vĩnh viễn.
              </p>
            </div>
          </div>
        )}

        {/* Bảng danh sách Danh mục */}
        <div className="rounded-2xl border border-stone-200/80 dark:border-stone-800 bg-white dark:bg-stone-900 shadow-xs overflow-hidden">
          {isLoading ? (
            <div className="p-8 space-y-4">
              {Array.from({ length: 6 }).map((_, i) => (
                <div key={i} className="flex items-center gap-4">
                  <Skeleton className="w-12 h-12 rounded-xl" />
                  <Skeleton className="h-6 w-1/4" />
                  <Skeleton className="h-6 w-1/3" />
                  <Skeleton className="h-8 w-24 ml-auto" />
                </div>
              ))}
            </div>
          ) : displayedCategories.length === 0 ? (
            <div className="py-16">
              <EmptyState
                title={
                  activeTab === "trash"
                    ? "Thùng rác đang trống"
                    : searchTerm
                    ? "Không tìm thấy danh mục phù hợp"
                    : "Chưa có danh mục nào"
                }
                description={
                  activeTab === "trash"
                    ? "Hiện không có danh mục nào bị xóa mềm trong vòng 30 ngày qua."
                    : searchTerm
                    ? `Không có danh mục nào khớp với từ khóa "${searchTerm}".`
                    : "Hệ thống chưa có danh mục ẩm thực nào. Hãy bấm nút Thêm Danh mục Mới ở trên."
                }
              />
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm border-collapse">
                <thead>
                  <tr className="border-b border-stone-200 dark:border-stone-800 bg-stone-50/75 dark:bg-stone-800/50 text-xs font-bold text-stone-500 uppercase tracking-wider">
                    <th className="py-3.5 px-4 w-16 text-center">Thứ tự</th>
                    <th className="py-3.5 px-4 w-20">Ảnh</th>
                    <th className="py-3.5 px-4 min-w-[200px]">Tên danh mục &amp; Slug</th>
                    <th className="py-3.5 px-4 hidden md:table-cell">Mô tả</th>
                    <th className="py-3.5 px-4 w-48 hidden sm:table-cell">
                      {activeTab === "trash" ? "Thông tin xóa" : "Lịch sử thời gian"}
                    </th>
                    <th className="py-3.5 px-4 text-right w-28">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-stone-100 dark:divide-stone-800">
                  {displayedCategories.map((cat) => {
                    const createdStr = formatDateSafe(cat.createdAt);
                    const updatedStr = formatDateSafe(cat.updatedAt);
                    const deletedStr = formatDateSafe(cat.deletedAt);
                    const remainingDays = getRemainingDays(cat.deletedAt);

                    return (
                      <tr
                        key={cat.id}
                        className={cn(
                          "transition-colors group",
                          activeTab === "trash"
                            ? "hover:bg-red-50/40 dark:hover:bg-red-950/20 bg-stone-50/30 dark:bg-stone-900/30"
                            : "hover:bg-stone-50/80 dark:hover:bg-stone-800/40"
                        )}
                      >
                        {/* Thứ tự */}
                        <td className="py-3.5 px-4 text-center font-mono font-bold text-stone-600 dark:text-stone-300">
                          #{cat.orderIndex}
                        </td>

                        {/* Ảnh xem trước */}
                        <td className="py-3.5 px-4">
                          <div className="w-12 h-12 rounded-xl overflow-hidden bg-stone-100 dark:bg-stone-800 border border-stone-200 dark:border-stone-700 shrink-0">
                            {cat.imageUrl ? (
                              // eslint-disable-next-line @next/next/no-img-element
                              <img
                                src={cat.imageUrl}
                                alt={cat.name}
                                className={cn(
                                  "w-full h-full object-cover transition-transform",
                                  activeTab === "trash" ? "grayscale opacity-70" : "group-hover:scale-105"
                                )}
                                onError={(e) => {
                                  (e.target as HTMLElement).style.display = "none";
                                }}
                              />
                            ) : (
                              <div className="w-full h-full flex items-center justify-center text-stone-400">
                                <ImageIcon className="w-5 h-5" />
                              </div>
                            )}
                          </div>
                        </td>

                        {/* Tên danh mục & Slug lồng nhau gọn gàng */}
                        <td className="py-3.5 px-4">
                          <div className="flex flex-col">
                            <div className="flex items-center gap-1.5 font-semibold text-stone-900 dark:text-stone-100">
                              <span className={cn(activeTab === "trash" && "line-through text-stone-500")}>
                                {cat.name}
                              </span>
                              {activeTab === "active" && (
                                <Link
                                  href={`/categories/${cat.slug}`}
                                  target="_blank"
                                  title="Xem danh mục ngoài giao diện"
                                  className="opacity-0 group-hover:opacity-100 text-stone-400 hover:text-brand-600 transition-opacity"
                                >
                                  <ExternalLink className="w-3.5 h-3.5" />
                                </Link>
                              )}
                            </div>
                            <span className="font-mono text-xs text-brand-600/90 dark:text-brand-400 mt-0.5">
                              /{cat.slug}
                            </span>
                          </div>
                        </td>

                        {/* Mô tả */}
                        <td className="py-3.5 px-4 text-xs text-stone-500 dark:text-stone-400 max-w-xs truncate hidden md:table-cell">
                          {cat.description || <span className="italic text-stone-400">Chưa có mô tả</span>}
                        </td>

                        {/* Thời gian: Tab Hoạt động vs Tab Thùng rác */}
                        <td className="py-3.5 px-4 text-xs text-stone-500 dark:text-stone-400 hidden sm:table-cell">
                          {activeTab === "trash" ? (
                            <div className="flex flex-col gap-0.5">
                              <span className="inline-flex items-center gap-1 text-red-600 dark:text-red-400 font-medium">
                                <Trash2 className="w-3 h-3 shrink-0" />
                                Đã xóa: {deletedStr || "Vừa xong"}
                              </span>
                              <span className="inline-flex items-center gap-1 text-amber-600 dark:text-amber-400 text-[11px] font-semibold">
                                <Clock className="w-3 h-3 shrink-0" />
                                Còn lại: {remainingDays} ngày
                              </span>
                            </div>
                          ) : (
                            <div className="flex flex-col gap-0.5">
                              <span className="inline-flex items-center gap-1 text-stone-600 dark:text-stone-300">
                                <Calendar className="w-3 h-3 text-stone-400 shrink-0" />
                                Tạo: {createdStr || "Hệ thống"}
                              </span>
                              {updatedStr ? (
                                <span className="inline-flex items-center gap-1 text-amber-600 dark:text-amber-400 font-medium">
                                  <Clock className="w-3 h-3 text-amber-500 shrink-0" />
                                  Sửa: {updatedStr}
                                </span>
                              ) : (
                                <span className="text-[11px] text-stone-400 pl-4 italic">Chưa chỉnh sửa</span>
                              )}
                            </div>
                          )}
                        </td>

                        {/* Nút thao tác Admin */}
                        <td className="py-3.5 px-4 text-right">
                          {activeTab === "trash" ? (
                            <button
                              type="button"
                              onClick={() => handleOpenRestoreModal(cat)}
                              className="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 hover:bg-emerald-100 dark:hover:bg-emerald-900 border border-emerald-300 dark:border-emerald-800 text-xs font-bold transition-colors"
                              title="Khôi phục danh mục về trạng thái hoạt động"
                            >
                              <RotateCcw className="w-3.5 h-3.5" />
                              <span>Khôi phục</span>
                            </button>
                          ) : (
                            <div className="inline-flex items-center justify-end gap-1.5">
                              <button
                                type="button"
                                onClick={() => handleOpenEditModal(cat)}
                                className="p-2 rounded-lg text-stone-600 hover:text-brand-600 hover:bg-brand-50 dark:text-stone-300 dark:hover:text-brand-400 dark:hover:bg-brand-950/50 transition-colors"
                                title="Chỉnh sửa danh mục"
                                aria-label={`Sửa ${cat.name}`}
                              >
                                <Pencil className="w-4 h-4" />
                              </button>
                              <button
                                type="button"
                                onClick={() => handleOpenDeleteModal(cat)}
                                className="p-2 rounded-lg text-stone-600 hover:text-red-600 hover:bg-red-50 dark:text-stone-300 dark:hover:text-red-400 dark:hover:bg-red-950/50 transition-colors"
                                title="Chuyển vào thùng rác"
                                aria-label={`Xóa ${cat.name}`}
                              >
                                <Trash2 className="w-4 h-4" />
                              </button>
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
      </main>

      {/* ============================================================ */}
      {/* 1. MODAL THÊM DANH MỤC MỚI (FR-CAT-003)                        */}
      {/* ============================================================ */}
      {isCreateModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-in fade-in">
          <div className="w-full max-w-lg rounded-3xl bg-white dark:bg-stone-900 border border-stone-200 dark:border-stone-800 shadow-2xl p-6 sm:p-8 space-y-6">
            <div className="flex items-center justify-between pb-3 border-b border-stone-100 dark:border-stone-800">
              <h3 className="text-xl font-bold text-stone-900 dark:text-white flex items-center gap-2">
                <Plus className="w-5 h-5 text-brand-600" />
                Thêm Danh mục Mới [Admin]
              </h3>
              <button
                type="button"
                onClick={() => setIsCreateModalOpen(false)}
                className="p-1 rounded-lg text-stone-400 hover:text-stone-600 dark:hover:text-stone-200"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {modalError && (
              <Alert variant="error" icon={<AlertCircle className="w-4 h-4 text-red-600" />}>
                {modalError}
              </Alert>
            )}

            <form onSubmit={handleCreateSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-stone-700 dark:text-stone-300 mb-1.5 uppercase tracking-wider">
                  Tên danh mục <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  required
                  placeholder="Ví dụ: Món Ăn Vặt Sài Gòn"
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl border border-stone-200 dark:border-stone-700 bg-white dark:bg-stone-800 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                />
                <p className="mt-1 text-xs text-stone-500">
                  Hệ thống Backend sẽ tự động sinh Slug URL chuẩn SEO không dấu từ tên này.
                </p>
              </div>

              <div>
                <label className="block text-xs font-semibold text-stone-700 dark:text-stone-300 mb-1.5 uppercase tracking-wider">
                  Mô tả giới thiệu
                </label>
                <textarea
                  rows={3}
                  placeholder="Mô tả đặc trưng, phong cách ẩm thực của danh mục..."
                  value={formDescription}
                  onChange={(e) => setFormDescription(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl border border-stone-200 dark:border-stone-700 bg-white dark:bg-stone-800 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-stone-700 dark:text-stone-300 mb-1.5 uppercase tracking-wider">
                  Đường link hình ảnh (Image URL)
                </label>
                <input
                  type="url"
                  placeholder="https://images.unsplash.com/photo-..."
                  value={formImageUrl}
                  onChange={(e) => setFormImageUrl(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl border border-stone-200 dark:border-stone-700 bg-white dark:bg-stone-800 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 font-mono text-xs"
                />
              </div>

              {formImageUrl && (
                <div className="p-3 rounded-xl bg-stone-50 dark:bg-stone-800/60 border border-stone-200 dark:border-stone-700 flex items-center gap-3">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img
                    src={formImageUrl}
                    alt="Xem trước"
                    className="w-14 h-14 rounded-lg object-cover shrink-0"
                    onError={(e) => {
                      (e.target as HTMLElement).style.display = "none";
                    }}
                  />
                  <span className="text-xs text-stone-500">Ảnh xem trước hợp lệ</span>
                </div>
              )}

              <div className="pt-4 flex items-center justify-end gap-3 border-t border-stone-100 dark:border-stone-800">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setIsCreateModalOpen(false)}
                  disabled={isSubmitting}
                >
                  Hủy bỏ
                </Button>
                <Button type="submit" variant="primary" isLoading={isSubmitting}>
                  Tạo danh mục
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ============================================================ */}
      {/* 2. MODAL CẬP NHẬT DANH MỤC (FR-CAT-004)                        */}
      {/* ============================================================ */}
      {editingCategory && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-in fade-in">
          <div className="w-full max-w-lg rounded-3xl bg-white dark:bg-stone-900 border border-stone-200 dark:border-stone-800 shadow-2xl p-6 sm:p-8 space-y-6">
            <div className="flex items-center justify-between pb-3 border-b border-stone-100 dark:border-stone-800">
              <h3 className="text-xl font-bold text-stone-900 dark:text-white flex items-center gap-2">
                <Pencil className="w-5 h-5 text-brand-600" />
                Cập nhật Danh mục [Admin]
              </h3>
              <button
                type="button"
                onClick={() => setEditingCategory(null)}
                className="p-1 rounded-lg text-stone-400 hover:text-stone-600 dark:hover:text-stone-200"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {modalError && (
              <Alert variant="error" icon={<AlertCircle className="w-4 h-4 text-red-600" />}>
                {modalError}
              </Alert>
            )}

            <form onSubmit={handleEditSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-stone-700 dark:text-stone-300 mb-1.5 uppercase tracking-wider">
                  Tên danh mục <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  required
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl border border-stone-200 dark:border-stone-700 bg-white dark:bg-stone-800 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-stone-700 dark:text-stone-300 mb-1.5 uppercase tracking-wider">
                    Thứ tự hiển thị
                  </label>
                  <input
                    type="number"
                    min={0}
                    value={formOrderIndex}
                    onChange={(e) => setFormOrderIndex(parseInt(e.target.value, 10) || 0)}
                    className="w-full px-3.5 py-2.5 rounded-xl border border-stone-200 dark:border-stone-700 bg-white dark:bg-stone-800 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 font-mono"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-stone-500 mb-1.5 uppercase tracking-wider">
                    Slug SEO (Bảo lưu cố định)
                  </label>
                  <input
                    type="text"
                    disabled
                    value={editingCategory.slug}
                    className="w-full px-3.5 py-2.5 rounded-xl border border-stone-200 dark:border-stone-700 bg-stone-100 dark:bg-stone-800 text-stone-500 text-xs font-mono cursor-not-allowed"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-stone-700 dark:text-stone-300 mb-1.5 uppercase tracking-wider">
                  Mô tả giới thiệu
                </label>
                <textarea
                  rows={3}
                  value={formDescription}
                  onChange={(e) => setFormDescription(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl border border-stone-200 dark:border-stone-700 bg-white dark:bg-stone-800 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-stone-700 dark:text-stone-300 mb-1.5 uppercase tracking-wider">
                  Đường link hình ảnh (Image URL)
                </label>
                <input
                  type="url"
                  placeholder="https://..."
                  value={formImageUrl}
                  onChange={(e) => setFormImageUrl(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl border border-stone-200 dark:border-stone-700 bg-white dark:bg-stone-800 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 font-mono text-xs"
                />
              </div>

              {formImageUrl && (
                <div className="p-3 rounded-xl bg-stone-50 dark:bg-stone-800/60 border border-stone-200 dark:border-stone-700 flex items-center gap-3">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img
                    src={formImageUrl}
                    alt="Xem trước"
                    className="w-14 h-14 rounded-lg object-cover shrink-0"
                    onError={(e) => {
                      (e.target as HTMLElement).style.display = "none";
                    }}
                  />
                  <span className="text-xs text-stone-500">Ảnh xem trước</span>
                </div>
              )}

              <div className="pt-4 flex items-center justify-end gap-3 border-t border-stone-100 dark:border-stone-800">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setEditingCategory(null)}
                  disabled={isSubmitting}
                >
                  Hủy bỏ
                </Button>
                <Button type="submit" variant="primary" isLoading={isSubmitting}>
                  Lưu thay đổi
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ============================================================ */}
      {/* 3. MODAL XÓA MỀM DANH MỤC (FR-CAT-005)                         */}
      {/* ============================================================ */}
      {deletingCategory && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-in fade-in">
          <div className="w-full max-w-md rounded-3xl bg-white dark:bg-stone-900 border border-stone-200 dark:border-stone-800 shadow-2xl p-6 sm:p-8 space-y-6">
            <div className="w-14 h-14 rounded-2xl bg-red-100 dark:bg-red-950/60 text-red-600 flex items-center justify-center mx-auto">
              <Trash2 className="w-7 h-7" />
            </div>

            <div className="text-center space-y-2">
              <h3 className="text-xl font-bold text-stone-900 dark:text-white">
                Xóa danh mục ẩm thực?
              </h3>
              <p className="text-sm text-stone-600 dark:text-stone-400 leading-relaxed">
                Bạn có chắc chắn muốn chuyển danh mục{" "}
                <strong className="text-stone-900 dark:text-stone-100">
                  &ldquo;{deletingCategory.name}&rdquo;
                </strong>{" "}
                vào thùng rác không?
              </p>
            </div>

            <div className="p-3.5 rounded-xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-900 text-xs text-amber-800 dark:text-amber-200 leading-relaxed">
              ♻️ <strong>Chính sách Xóa:</strong> Danh mục sẽ được chuyển vào <strong>Thùng rác</strong> và lưu giữ trong<strong>30 ngày</strong>. Dữ liệu các công thức liên quan vẫn được bảo toàn nguyên vẹn và bạn có thể bấm <strong>Khôi phục</strong> bất cứ lúc nào.
            </div>

            {modalError && (
              <Alert variant="error" icon={<AlertCircle className="w-4 h-4 text-red-600" />}>
                {modalError}
              </Alert>
            )}

            <div className="flex items-center justify-end gap-3 pt-2">
              <Button
                type="button"
                variant="outline"
                className="w-full"
                onClick={() => setDeletingCategory(null)}
                disabled={isSubmitting}
              >
                Hủy bỏ
              </Button>
              <Button
                type="button"
                variant="destructive"
                className="w-full"
                onClick={handleDeleteSubmit}
                isLoading={isSubmitting}
              >
                Xác nhận xóa
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* ============================================================ */}
      {/* 4. MODAL KHÔI PHỤC DANH MỤC TỪ THÙNG RÁC                       */}
      {/* ============================================================ */}
      {restoringCategory && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-in fade-in">
          <div className="w-full max-w-md rounded-3xl bg-white dark:bg-stone-900 border border-stone-200 dark:border-stone-800 shadow-2xl p-6 sm:p-8 space-y-6">
            <div className="w-14 h-14 rounded-2xl bg-emerald-100 dark:bg-emerald-950/60 text-emerald-600 flex items-center justify-center mx-auto">
              <RotateCcw className="w-7 h-7" />
            </div>

            <div className="text-center space-y-2">
              <h3 className="text-xl font-bold text-stone-900 dark:text-white">
                Khôi phục danh mục ẩm thực?
              </h3>
              <p className="text-sm text-stone-600 dark:text-stone-400 leading-relaxed">
                Bạn có muốn khôi phục danh mục{" "}
                <strong className="text-stone-900 dark:text-stone-100">
                  &ldquo;{restoringCategory.name}&rdquo;
                </strong>{" "}
                trở lại trạng thái hoạt động trên website không?
              </p>
            </div>

            {modalError && (
              <Alert variant="error" icon={<AlertCircle className="w-4 h-4 text-red-600" />}>
                {modalError}
              </Alert>
            )}

            <div className="flex items-center justify-end gap-3 pt-2">
              <Button
                type="button"
                variant="outline"
                className="w-full"
                onClick={() => setRestoringCategory(null)}
                disabled={isSubmitting}
              >
                Hủy bỏ
              </Button>
              <Button
                type="button"
                variant="default"
                className="w-full bg-emerald-600 hover:bg-emerald-700 text-white"
                onClick={handleRestoreSubmit}
                isLoading={isSubmitting}
              >
                Khôi phục ngay
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
