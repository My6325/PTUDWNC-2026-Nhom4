"use client";

import React, { useEffect, useState, useMemo, useCallback } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  ChefHat,
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
  RotateCcw,
  Sparkles,
  Archive,
  Eye,
  Calendar,
  Layers,
  FolderHeart,
} from "lucide-react";
import { recipeManagementService as management } from "@/services/recipeManagementService";
import { recipeService } from "@/services/recipeService";
import { AdminRecipeListDto } from "@/types/recipe";
import { useAuthStore } from "@/stores/auth-store";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Alert } from "@/components/ui/Alert";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { extractApiErrorMessage } from "@/utils/apiError";
import { cn } from "@/utils/cn";

type StatusTab = "all" | "published" | "draft" | "trash";

export default function AdminRecipesPage() {
  const router = useRouter();
  const { user, isAuthenticated, isLoading: isAuthLoading } = useAuthStore();

  const [activeTab, setActiveTab] = useState<StatusTab>("all");
  const [searchTerm, setSearchTerm] = useState<string>("");
  const [debouncedSearch, setDebouncedSearch] = useState<string>("");

  const [recipes, setRecipes] = useState<AdminRecipeListDto[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [pageIndex, setPageIndex] = useState<number>(1);
  const pageSize = 12;

  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Modal Xóa
  const [deletingRecipe, setDeletingRecipe] = useState<AdminRecipeListDto | null>(null);
  const [isActionLoading, setIsActionLoading] = useState<boolean>(false);

  // Debounce search input
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchTerm);
      setPageIndex(1);
    }, 400);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // Kiểm tra quyền Admin
  const isAdmin = useMemo(() => {
    if (!user || !user.roles) return false;
    return user.roles.some((r) => r.toLowerCase() === "admin");
  }, [user]);

  // Tải danh sách công thức
  const fetchRecipes = useCallback(async () => {
    if (!isAdmin) return;
    setIsLoading(true);
    setError(null);
    try {
      let statusParam: number | undefined = undefined;
      let onlyDeletedParam = false;

      if (activeTab === "published") statusParam = 2;
      else if (activeTab === "draft") statusParam = 1;
      else if (activeTab === "trash") onlyDeletedParam = true;

      const data = await recipeService.getAdminRecipes({
        searchTerm: debouncedSearch.trim() || undefined,
        status: statusParam,
        onlyDeleted: onlyDeletedParam,
        pageIndex,
        pageSize,
      });

      setRecipes(data.items || []);
      setTotalCount(data.totalCount || 0);
    } catch (err: unknown) {
      setError(extractApiErrorMessage(err, "Không thể tải danh sách công thức. Vui lòng thử lại."));
    } finally {
      setIsLoading(false);
    }
  }, [isAdmin, activeTab, debouncedSearch, pageIndex]);

  useEffect(() => {
    if (isAdmin) {
      fetchRecipes();
    }
  }, [isAdmin, fetchRecipes]);

  // Hành động Xuất bản
  const handlePublish = async (recipe: AdminRecipeListDto) => {
    setIsActionLoading(true);
    setSuccessMessage(null);
    setError(null);
    try {
      await management.act(recipe, "publish");
      setSuccessMessage(`Đã xuất bản công thức "${recipe.title}" thành công!`);
      await fetchRecipes();
    } catch (err: unknown) {
      setError(extractApiErrorMessage(err, "Không thể xuất bản công thức."));
    } finally {
      setIsActionLoading(false);
    }
  };

  // Hành động Thu hồi về nháp
  const handleUnpublish = async (recipe: AdminRecipeListDto) => {
    setIsActionLoading(true);
    setSuccessMessage(null);
    setError(null);
    try {
      await management.act(recipe, "unpublish");
      setSuccessMessage(`Đã thu hồi công thức "${recipe.title}" về bản nháp.`);
      await fetchRecipes();
    } catch (err: unknown) {
      setError(extractApiErrorMessage(err, "Không thể thu hồi công thức."));
    } finally {
      setIsActionLoading(false);
    }
  };

  // Hành động Lưu trữ
  const handleArchive = async (recipe: AdminRecipeListDto) => {
    setIsActionLoading(true);
    setSuccessMessage(null);
    setError(null);
    try {
      await management.act(recipe, "archive");
      setSuccessMessage(`Đã đưa công thức "${recipe.title}" vào lưu trữ.`);
      await fetchRecipes();
    } catch (err: unknown) {
      setError(extractApiErrorMessage(err, "Không thể lưu trữ công thức."));
    } finally {
      setIsActionLoading(false);
    }
  };

  // Hành động Bỏ lưu trữ
  const handleUnarchive = async (recipe: AdminRecipeListDto) => {
    setIsActionLoading(true);
    setSuccessMessage(null);
    setError(null);
    try {
      await management.act(recipe, "unarchive");
      setSuccessMessage(`Đã chuyển về bản nháp cho công thức "${recipe.title}".`);
      await fetchRecipes();
    } catch (err: unknown) {
      setError(extractApiErrorMessage(err, "Không thể bỏ lưu trữ công thức."));
    } finally {
      setIsActionLoading(false);
    }
  };


  // Hành động Xóa mềm
  const handleDeleteConfirm = async () => {
    if (!deletingRecipe) return;
    setIsActionLoading(true);
    try {
      await management.act(deletingRecipe, "delete");
      setSuccessMessage(`Đã chuyển công thức "${deletingRecipe.title}" vào thùng rác.`);
      setDeletingRecipe(null);
      await fetchRecipes();
    } catch (err: unknown) {
      setError(extractApiErrorMessage(err, "Không thể xóa công thức này."));
    } finally {
      setIsActionLoading(false);
    }
  };

  // Hành động Khôi phục từ thùng rác
  const handleRestore = async (recipe: AdminRecipeListDto) => {
    setIsActionLoading(true);
    setSuccessMessage(null);
    setError(null);
    try {
      await management.act(recipe, "restore");
      setSuccessMessage(`Đã khôi phục công thức "${recipe.title}" từ thùng rác.`);
      await fetchRecipes();
    } catch (err: unknown) {
      setError(extractApiErrorMessage(err, "Không thể khôi phục công thức."));
    } finally {
      setIsActionLoading(false);
    }
  };

  // Định dạng ngày
  const formatDate = (dateStr?: string | null) => {
    if (!dateStr) return "—";
    try {
      return new Date(dateStr).toLocaleDateString("vi-VN", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
      });
    } catch {
      return dateStr;
    }
  };

  // Render Badge trạng thái
  const renderStatusBadge = (recipe: AdminRecipeListDto) => {
    if (recipe.isDeleted) {
      return (
        <Badge variant="danger" className="font-medium">
          Thùng rác
        </Badge>
      );
    }
    const statusVal = Number(recipe.status);
    if (statusVal === 2 || recipe.status === "Published") {
      return (
        <Badge variant="herb" className="bg-emerald-100 text-emerald-800 dark:bg-emerald-950/60 dark:text-emerald-300 font-medium">
          Đã xuất bản
        </Badge>
      );
    }
    if (statusVal === 4 || recipe.status === "PendingApproval") return <Badge variant="outline">Chờ duyệt</Badge>;
    if (statusVal === 3 || recipe.status === "Archived") {
      return (
        <Badge variant="default" className="font-medium">
          Đã lưu trữ
        </Badge>
      );
    }
    return (
      <Badge variant="warning" className="bg-amber-100 text-amber-800 dark:bg-amber-950/60 dark:text-amber-300 font-medium">
        Bản nháp
      </Badge>
    );
  };

  // Render Badge độ khó
  const renderDifficultyBadge = (difficulty: unknown) => {
    const diffVal = Number(difficulty);
    if (diffVal === 3 || difficulty === "Hard" || difficulty === "hard") {
      return <Badge variant="outline" className="border-red-300 text-red-700 dark:text-red-400">Khó</Badge>;
    }
    if (diffVal === 2 || difficulty === "Medium" || difficulty === "medium") {
      return <Badge variant="outline" className="border-amber-300 text-amber-700 dark:text-amber-400">Vừa</Badge>;
    }
    return <Badge variant="outline" className="border-emerald-300 text-emerald-700 dark:text-emerald-400">Dễ</Badge>;
  };

  // Tổng số trang
  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  // Trạng thái kiểm tra quyền
  if (isAuthLoading) {
    return (
      <div className="container-custom max-w-7xl mx-auto px-4 py-12 space-y-6">
        <Skeleton className="h-10 w-64 rounded-lg" />
        <Skeleton className="h-6 w-96 rounded-md" />
        <Skeleton className="h-96 w-full rounded-2xl" />
      </div>
    );
  }

  if (!isAuthenticated || !isAdmin) {
    return (
      <div className="container-custom max-w-4xl mx-auto px-4 py-16 text-center">
        <div className="mx-auto w-16 h-16 rounded-full bg-red-100 dark:bg-red-950/60 flex items-center justify-center text-red-600 mb-4">
          <ShieldAlert className="w-8 h-8" />
        </div>
        <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-100 mb-2">
          Truy cập bị từ chối
        </h1>
        <p className="text-stone-600 dark:text-stone-400 max-w-md mx-auto mb-6">
          Khu vực này chỉ dành riêng cho tài khoản Quản trị viên (Admin) của hệ thống Culinary Blog.
        </p>
        <div className="flex justify-center gap-4">
          <Link href="/login">
            <Button variant="default">Đăng nhập tài khoản Admin</Button>
          </Link>
          <Link href="/">
            <Button variant="outline">Về trang chủ</Button>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-stone-50/50 dark:bg-stone-950/20 py-8">
      <div className="container-custom max-w-7xl mx-auto px-4 space-y-6">
        {/* Breadcrumb & Navigation */}
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 text-xs text-stone-500 mb-1">
              <Link href="/" className="hover:text-stone-900 dark:hover:text-stone-200">Trang chủ</Link>
              <ChevronRight className="w-3 h-3" />
              <span className="text-stone-700 dark:text-stone-300 font-medium">Bảng điều khiển Quản trị</span>
            </div>
            <Button href="/admin/recipes/review" variant="outline">Duyệt bài chờ và ghi lý do từ chối</Button>
            <h1 className="text-2xl md:text-3xl font-extrabold text-stone-900 dark:text-stone-100 flex items-center gap-2.5">
              <ChefHat className="w-7 h-7 text-brand-600" />
              Quản trị Công thức Nấu ăn
            </h1>
            <p className="text-sm text-stone-600 dark:text-stone-400 mt-1">
              Quản lý toàn diện vòng đời công thức, duyệt xuất bản, kiểm duyệt nội dung và khôi phục bài viết.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2.5">
            <Link href="/admin/categories">
              <Button variant="outline" size="sm" leftIcon={<FolderHeart className="w-4 h-4 text-stone-500" />}>
                Quản lý Danh mục
              </Button>
            </Link>
            <Link href="/recipes/create">
              <Button variant="default" size="sm" leftIcon={<Plus className="w-4 h-4" />} className="bg-brand-600 hover:bg-brand-700 text-white font-semibold shadow-xs">
                Tạo công thức mới
              </Button>
            </Link>
          </div>
        </div>

        {/* Thông báo thành công / lỗi */}
        {successMessage && (
          <Alert variant="success" dismissible onDismiss={() => setSuccessMessage(null)}>
            <div className="flex items-center gap-2 font-medium">
              <CheckCircle2 className="w-4 h-4 shrink-0 text-emerald-600" />
              <span>{successMessage}</span>
            </div>
          </Alert>
        )}
        {error && (
          <Alert variant="error" dismissible onDismiss={() => setError(null)}>
            <div className="flex items-center gap-2 font-medium">
              <AlertCircle className="w-4 h-4 shrink-0 text-red-600" />
              <span>{error}</span>
            </div>
          </Alert>
        )}

        {/* Tabs Trạng thái & Ô Tìm kiếm */}
        <div className="bg-white dark:bg-stone-900 rounded-xl p-4 shadow-xs border border-stone-200 dark:border-stone-800 space-y-4">
          <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
            {/* Status Tabs */}
            <div className="flex flex-wrap items-center gap-1.5 p-1 bg-stone-100 dark:bg-stone-800 rounded-lg">
              <button
                onClick={() => { setActiveTab("all"); setPageIndex(1); }}
                className={cn(
                  "px-3 py-1.5 text-xs font-semibold rounded-md transition-colors",
                  activeTab === "all" ? "bg-white dark:bg-stone-900 text-stone-900 dark:text-stone-100 shadow-xs" : "text-stone-600 dark:text-stone-400 hover:text-stone-900"
                )}
              >
                Tất cả
              </button>
              <button
                onClick={() => { setActiveTab("published"); setPageIndex(1); }}
                className={cn(
                  "px-3 py-1.5 text-xs font-semibold rounded-md transition-colors",
                  activeTab === "published" ? "bg-white dark:bg-stone-900 text-emerald-700 dark:text-emerald-400 shadow-xs" : "text-stone-600 dark:text-stone-400 hover:text-stone-900"
                )}
              >
                Đã xuất bản
              </button>
              <button
                onClick={() => { setActiveTab("draft"); setPageIndex(1); }}
                className={cn(
                  "px-3 py-1.5 text-xs font-semibold rounded-md transition-colors",
                  activeTab === "draft" ? "bg-white dark:bg-stone-900 text-amber-700 dark:text-amber-400 shadow-xs" : "text-stone-600 dark:text-stone-400 hover:text-stone-900"
                )}
              >
                Bản nháp
              </button>
              <button
                onClick={() => { setActiveTab("trash"); setPageIndex(1); }}
                className={cn(
                  "px-3 py-1.5 text-xs font-semibold rounded-md transition-colors flex items-center gap-1",
                  activeTab === "trash" ? "bg-white dark:bg-stone-900 text-red-600 shadow-xs" : "text-stone-600 dark:text-stone-400 hover:text-stone-900"
                )}
              >
                <Trash2 className="w-3.5 h-3.5" />
                Thùng rác
              </button>
            </div>

            {/* Search Input */}
            <div className="flex items-center gap-2">
              <div className="relative w-full sm:w-72">
                <Search className="w-4 h-4 text-stone-400 absolute left-3 top-1/2 -translate-y-1/2" />
                <input
                  type="text"
                  placeholder="Tìm theo tên công thức..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="w-full pl-9 pr-4 py-1.5 text-sm rounded-lg border border-stone-200 dark:border-stone-800 bg-stone-50 dark:bg-stone-950/50 text-stone-900 dark:text-stone-100 placeholder:text-stone-400 focus:outline-hidden focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500"
                />
                {searchTerm && (
                  <button onClick={() => setSearchTerm("")} className="absolute right-2.5 top-1/2 -translate-y-1/2 text-stone-400 hover:text-stone-600">
                    <X className="w-3.5 h-3.5" />
                  </button>
                )}
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={fetchRecipes}
                isLoading={isLoading}
                title="Làm mới dữ liệu"
              >
                <RefreshCw className="w-3.5 h-3.5" />
              </Button>
            </div>
          </div>
        </div>

        {/* Bảng Quản trị Công thức */}
        <div className="bg-white dark:bg-stone-900 rounded-xl shadow-xs border border-stone-200 dark:border-stone-800 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-stone-50 dark:bg-stone-800/60 border-b border-stone-200 dark:border-stone-800 text-[11px] font-semibold text-stone-500 uppercase tracking-wider">
                  <th className="py-3.5 px-4 w-12 text-center">#</th>
                  <th className="py-3.5 px-4 min-w-[280px]">Món ăn & Công thức</th>
                  <th className="py-3.5 px-4 min-w-[130px]">Danh mục</th>
                  <th className="py-3.5 px-4 min-w-[140px]">Tác giả</th>
                  <th className="py-3.5 px-4 min-w-[120px]">Trạng thái</th>
                  <th className="py-3.5 px-4 min-w-[90px]">Độ khó</th>
                  <th className="py-3.5 px-4 min-w-[110px]">Ngày tạo</th>
                  <th className="py-3.5 px-4 min-w-[180px] text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-stone-100 dark:divide-stone-800/80 text-sm">
                {isLoading ? (
                  Array.from({ length: 6 }).map((_, i) => (
                    <tr key={i} className="animate-pulse">
                      <td className="py-4 px-4 text-center"><Skeleton className="h-4 w-6 mx-auto" /></td>
                      <td className="py-4 px-4"><div className="flex gap-3"><Skeleton className="h-12 w-12 rounded-lg shrink-0" /><div className="space-y-2 flex-1"><Skeleton className="h-4 w-48" /><Skeleton className="h-3 w-32" /></div></div></td>
                      <td className="py-4 px-4"><Skeleton className="h-4 w-24" /></td>
                      <td className="py-4 px-4"><Skeleton className="h-4 w-28" /></td>
                      <td className="py-4 px-4"><Skeleton className="h-6 w-20 rounded-full" /></td>
                      <td className="py-4 px-4"><Skeleton className="h-4 w-16" /></td>
                      <td className="py-4 px-4"><Skeleton className="h-4 w-20" /></td>
                      <td className="py-4 px-4"><Skeleton className="h-8 w-24 ml-auto rounded-md" /></td>
                    </tr>
                  ))
                ) : recipes.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="py-12 text-center">
                      <EmptyState
                        icon={<ChefHat className="w-10 h-10 text-stone-300 mx-auto" />}
                        title="Không tìm thấy công thức nào"
                        description={
                          searchTerm
                            ? `Không có công thức nào khớp với từ khóa "${searchTerm}".`
                            : "Danh sách hiện tại đang trống."
                        }
                      />
                    </td>
                  </tr>
                ) : (
                  recipes.map((recipe, index) => {
                    const rowNumber = (pageIndex - 1) * pageSize + index + 1;
                    return (
                      <tr
                        key={recipe.id}
                        className={cn(
                          "hover:bg-stone-50/80 dark:hover:bg-stone-800/40 transition-colors",
                          recipe.isDeleted && "opacity-75 bg-red-50/20 dark:bg-red-950/10"
                        )}
                      >
                        {/* Số thứ tự */}
                        <td className="py-3 px-4 text-center text-xs text-stone-400 font-mono">
                          {rowNumber}
                        </td>

                        {/* Món ăn & Cover Image */}
                        <td className="py-3 px-4">
                          <div className="flex items-center gap-3">
                            <div className="w-12 h-12 rounded-lg bg-stone-100 dark:bg-stone-800 overflow-hidden shrink-0 border border-stone-200/60 dark:border-stone-700">
                              {recipe.coverImageUrl ? (
                                <img
                                  src={recipe.coverImageUrl}
                                  alt={recipe.title}
                                  className="w-full h-full object-cover"
                                  loading="lazy"
                                />
                              ) : (
                                <div className="w-full h-full flex items-center justify-center text-stone-400">
                                  <ImageIcon className="w-5 h-5" />
                                </div>
                              )}
                            </div>
                            <div className="min-w-0">
                              <Link
                                href={`/recipes/${recipe.slug || recipe.id}`}
                                className="font-semibold text-stone-900 dark:text-stone-100 hover:text-brand-600 dark:hover:text-brand-400 transition-colors line-clamp-1 flex items-center gap-1.5"
                                title={recipe.title}
                              >
                                <span>{recipe.title}</span>
                                <ExternalLink className="w-3.5 h-3.5 text-stone-400 shrink-0" />
                              </Link>
                              <p className="text-xs text-stone-400 font-mono truncate mt-0.5 max-w-[260px]">
                                /{recipe.slug}
                              </p>
                            </div>
                          </div>
                        </td>

                        {/* Danh mục */}
                        <td className="py-3 px-4">
                          {recipe.categoryName ? (
                            <Badge variant="outline" className="text-xs bg-stone-50 dark:bg-stone-800/50">
                              {recipe.categoryName}
                            </Badge>
                          ) : (
                            <span className="text-xs text-stone-400 italic">Chưa phân loại</span>
                          )}
                        </td>

                        {/* Tác giả */}
                        <td className="py-3 px-4 text-xs font-medium text-stone-700 dark:text-stone-300">
                          {recipe.authorName || "Ẩn danh"}
                        </td>

                        {/* Trạng thái */}
                        <td className="py-3 px-4">
                          {renderStatusBadge(recipe)}
                        </td>

                        {/* Độ khó */}
                        <td className="py-3 px-4">
                          {renderDifficultyBadge(recipe.difficulty)}
                        </td>

                        {/* Ngày tạo */}
                        <td className="py-3 px-4 text-xs text-stone-500 whitespace-nowrap">
                          {formatDate(recipe.createdAt)}
                        </td>

                        {/* Thao tác */}
                        <td className="py-3 px-4 text-right">
                          <div className="flex items-center justify-end gap-1.5">
                            {/* Nếu trong thùng rác: nút khôi phục và nút xóa luôn */}
                            {recipe.isDeleted ? (
                              <div className="flex items-center justify-end gap-1.5">
                                <Button
                                  variant="outline"
                                  size="sm"
                                  onClick={() => handleRestore(recipe)}
                                  disabled={isActionLoading}
                                  leftIcon={<RotateCcw className="w-3.5 h-3.5 text-emerald-600" />}
                                  className="text-xs text-emerald-700 border-emerald-300 hover:bg-emerald-50 dark:border-emerald-800"
                                >
                                  Khôi phục
                                </Button>

                              </div>
                            ) : (
                              <>
                                {/* Duyệt xuất bản bài đang chờ */}
                                {(Number(recipe.status) === 4 || recipe.status === "PendingApproval") && (
                                  <button
                                    onClick={() => handlePublish(recipe)}
                                    disabled={isActionLoading}
                                    title="Xuất bản công thức"
                                    className="p-1.5 text-emerald-600 hover:bg-emerald-50 dark:hover:bg-emerald-950/40 rounded-md transition-colors"
                                  >
                                    <Sparkles className="w-4 h-4" />
                                  </button>
                                )}

                                {/* Nút Thu hồi về nháp (nếu là Published) */}
                                {(Number(recipe.status) === 2 || recipe.status === "Published") && (
                                  <button
                                    onClick={() => handleUnpublish(recipe)}
                                    disabled={isActionLoading}
                                    title="Thu hồi về bản nháp"
                                    className="p-1.5 text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-950/40 rounded-md transition-colors"
                                  >
                                    <RotateCcw className="w-4 h-4" />
                                  </button>
                                )}

                                {/* Nút Sửa */}
                                <Link
                                  href={`/dashboard/recipes/${recipe.id}/edit`}
                                  className="p-1.5 text-brand-600 hover:bg-brand-50 dark:hover:bg-brand-950/40 rounded-md transition-colors"
                                  title="Chỉnh sửa công thức"
                                >
                                  <Pencil className="w-4 h-4" />
                                </Link>

                                {/* Nút Xem chi tiết */}
                                <Link
                                  href={`/recipes/${recipe.slug || recipe.id}`}
                                  className="p-1.5 text-stone-500 hover:bg-stone-100 dark:hover:bg-stone-800 rounded-md transition-colors"
                                  title="Xem trước công thức"
                                >
                                  <Eye className="w-4 h-4" />
                                </Link>

                                {/* Nút Xóa */}
                                <button
                                  onClick={() => setDeletingRecipe(recipe)}
                                  disabled={isActionLoading}
                                  title="Chuyển vào thùng rác"
                                  className="p-1.5 text-red-500 hover:bg-red-50 dark:hover:bg-red-950/40 rounded-md transition-colors"
                                >
                                  <Trash2 className="w-4 h-4" />
                                </button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>

          {/* Phân trang Footer */}
          <div className="flex flex-col sm:flex-row items-center justify-between gap-4 p-4 border-t border-stone-200 dark:border-stone-800 text-xs text-stone-500">
            <div>
              Tổng số <span className="font-semibold text-stone-900 dark:text-stone-100">{totalCount}</span> công thức
              {totalCount > 0 && ` (Trang ${pageIndex} / ${totalPages})`}
            </div>

            {totalPages > 1 && (
              <div className="flex items-center gap-1.5">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPageIndex((p) => Math.max(p - 1, 1))}
                  disabled={pageIndex <= 1 || isLoading}
                  className="text-xs h-8 px-2.5"
                >
                  Trước
                </Button>
                {Array.from({ length: Math.min(totalPages, 5) }).map((_, i) => {
                  let pNum = i + 1;
                  if (totalPages > 5 && pageIndex > 3) {
                    pNum = pageIndex - 3 + i;
                    if (pNum > totalPages) pNum = totalPages - (4 - i);
                  }
                  return (
                    <button
                      key={pNum}
                      onClick={() => setPageIndex(pNum)}
                      className={cn(
                        "w-8 h-8 rounded-md text-xs font-semibold transition-colors",
                        pageIndex === pNum
                          ? "bg-brand-600 text-white shadow-xs"
                          : "text-stone-600 hover:bg-stone-100 dark:hover:bg-stone-800"
                      )}
                    >
                      {pNum}
                    </button>
                  );
                })}
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPageIndex((p) => Math.min(p + 1, totalPages))}
                  disabled={pageIndex >= totalPages || isLoading}
                  className="text-xs h-8 px-2.5"
                >
                  Sau
                </Button>
              </div>
            )}
          </div>
        </div>



        {/* Modal Xác nhận Xóa mềm công thức */}
        {deletingRecipe && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-in fade-in duration-200">
            <div className="bg-white dark:bg-stone-900 rounded-2xl max-w-md w-full p-6 shadow-xl border border-stone-200 dark:border-stone-800 space-y-4">
              <div className="flex items-center gap-3 text-red-600">
                <div className="p-2.5 rounded-full bg-red-100 dark:bg-red-950/60">
                  <Trash2 className="w-5 h-5" />
                </div>
                <h3 className="text-lg font-bold text-stone-900 dark:text-stone-100">
                  Xóa công thức?
                </h3>
              </div>
              <p className="text-sm text-stone-600 dark:text-stone-400">
                Bạn có chắc chắn muốn chuyển công thức{" "}
                <strong className="text-stone-900 dark:text-stone-100">
                  &quot;{deletingRecipe.title}&quot;
                </strong>{" "}
                vào Thùng rác? Món ăn sẽ tạm thời bị ẩn khỏi cộng đồng và có thể khôi phục lại bất kỳ lúc nào.
              </p>
              <div className="flex items-center justify-end gap-3 pt-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setDeletingRecipe(null)}
                  disabled={isActionLoading}
                >
                  Hủy bỏ
                </Button>
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={handleDeleteConfirm}
                  isLoading={isActionLoading}
                >
                  Xác nhận xóa
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
