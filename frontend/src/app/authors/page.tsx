"use client";

import React, { useEffect, useState, useMemo } from "react";
import Link from "next/link";
import {
  ChefHat,
  Search,
  Sparkles,
  User,
  UtensilsCrossed,
  BookOpen,
  ArrowRight,
  Shield,
  Clock,
  RefreshCw,
  AlertCircle,
  ChevronRight,
} from "lucide-react";
import { authService } from "@/services/authService";
import { AuthorDto } from "@/types/auth";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { cn } from "@/utils/cn";

export default function AuthorsPage() {
  const [authors, setAuthors] = useState<AuthorDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState<string>("");

  const loadAuthors = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await authService.getAuthors();
      setAuthors(data);
    } catch {
      setError("Không thể tải danh sách tác giả. Vui lòng thử lại sau.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadAuthors();
  }, []);

  // Lọc tác giả theo từ khóa tìm kiếm
  const filteredAuthors = useMemo(() => {
    if (!searchTerm.trim()) return authors;
    const term = searchTerm.toLowerCase().trim();
    return authors.filter(
      (a) =>
        a.displayName.toLowerCase().includes(term) ||
        a.userName.toLowerCase().includes(term) ||
        (a.bio && a.bio.toLowerCase().includes(term))
    );
  }, [authors, searchTerm]);

  // Tổng số công thức của tất cả tác giả
  const totalRecipes = useMemo(() => {
    return authors.reduce((sum, a) => sum + (a.recipeCount || 0), 0);
  }, [authors]);

  return (
    <div className="min-h-screen bg-surface-warm dark:bg-stone-950/50 pb-20">
      {/* Breadcrumb Navigation */}
      <div className="border-b border-surface-border dark:border-stone-800 bg-white dark:bg-stone-900/80">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-3">
          <nav className="flex items-center gap-1.5 text-xs text-stone-500 dark:text-stone-400">
            <Link
              href="/"
              className="hover:text-stone-900 dark:hover:text-stone-100 transition-colors"
            >
              Trang chủ
            </Link>
            <ChevronRight className="h-3 w-3 shrink-0" />
            <span className="text-stone-800 dark:text-stone-200 font-medium">
              Đội ngũ Đầu bếp & Tác giả
            </span>
          </nav>
        </div>
      </div>

      {/* Hero Banner Header */}
      <section className="bg-white dark:bg-stone-900 border-b border-surface-border dark:border-stone-800 py-12 md:py-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center space-y-4">
          <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-brand-50 dark:bg-brand-950/60 border border-brand-200 dark:border-brand-800 text-brand-700 dark:text-brand-300 text-xs font-semibold">
            <Sparkles className="w-4 h-4 text-brand-600" />
            <span>Cộng đồng Ẩm thực Chuyên nghiệp</span>
          </div>

          <h1 className="text-3xl sm:text-4xl lg:text-5xl font-black text-stone-900 dark:text-stone-100 tracking-tight">
            Đội ngũ <span className="text-brand-600 dark:text-brand-400">Đầu bếp & Tác giả</span>
          </h1>

          <p className="max-w-2xl mx-auto text-sm sm:text-base text-stone-600 dark:text-stone-400 leading-relaxed">
            Khám phá những bàn tay tài hoa đứng sau các công thức món ăn đặc sắc trên CulinaryBlog. Hãy theo dõi và học hỏi kinh nghiệm nấu nướng từ các tác giả ẩm thực giàu đam mê.
          </p>

          {/* Thống kê nhanh */}
          <div className="flex flex-wrap items-center justify-center gap-6 pt-4 text-xs font-medium text-stone-600 dark:text-stone-400">
            <div className="flex items-center gap-2 bg-stone-50 dark:bg-stone-800/60 px-4 py-2 rounded-2xl border border-stone-200/60 dark:border-stone-700">
              <ChefHat className="w-4 h-4 text-brand-600 dark:text-brand-400" />
              <span>
                <strong className="text-stone-900 dark:text-stone-100 font-bold">{authors.length}</strong> Tác giả & Đầu bếp
              </span>
            </div>
            <div className="flex items-center gap-2 bg-stone-50 dark:bg-stone-800/60 px-4 py-2 rounded-2xl border border-stone-200/60 dark:border-stone-700">
              <UtensilsCrossed className="w-4 h-4 text-amber-600 dark:text-amber-400" />
              <span>
                <strong className="text-stone-900 dark:text-stone-100 font-bold">{totalRecipes}</strong> Món ăn đã xuất bản
              </span>
            </div>
          </div>
        </div>
      </section>

      {/* Main Content Area */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-8">
        {/* Search & Filter Bar */}
        <div className="flex flex-col sm:flex-row items-center justify-between gap-4 mb-8">
          <div className="relative w-full sm:max-w-md">
            <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-stone-400" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Tìm theo tên tác giả hoặc tiểu sử..."
              className="w-full pl-10 pr-4 py-2.5 text-sm rounded-2xl border border-stone-200 dark:border-stone-800 bg-white dark:bg-stone-900 text-stone-900 dark:text-stone-100 placeholder-stone-400 focus:outline-none focus:ring-2 focus:ring-brand-500 shadow-xs"
            />
          </div>

          <div className="flex items-center gap-3 w-full sm:w-auto justify-end">
            <Button
              variant="outline"
              size="sm"
              onClick={loadAuthors}
              disabled={isLoading}
              leftIcon={<RefreshCw className={cn("w-3.5 h-3.5", isLoading && "animate-spin")} />}
            >
              Làm mới
            </Button>
          </div>
        </div>

        {/* Lỗi tải */}
        {error && (
          <div className="mb-8 p-4 rounded-2xl bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-800 flex items-center justify-between gap-3 text-red-700 dark:text-red-300 text-sm">
            <div className="flex items-center gap-2">
              <AlertCircle className="w-5 h-5 shrink-0" />
              <span>{error}</span>
            </div>
            <Button size="sm" variant="outline" onClick={loadAuthors}>
              Thử lại
            </Button>
          </div>
        )}

        {/* Loading Skeletons */}
        {isLoading && (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            {Array.from({ length: 8 }).map((_, idx) => (
              <div
                key={idx}
                className="bg-white dark:bg-stone-900 rounded-3xl border border-stone-200/80 dark:border-stone-800 p-6 space-y-4"
              >
                <div className="flex items-center gap-4">
                  <Skeleton className="w-16 h-16 rounded-full" />
                  <div className="space-y-2 flex-1">
                    <Skeleton className="h-5 w-3/4 rounded-md" />
                    <Skeleton className="h-3 w-1/2 rounded-md" />
                  </div>
                </div>
                <Skeleton className="h-12 w-full rounded-xl" />
                <Skeleton className="h-8 w-full rounded-xl" />
              </div>
            ))}
          </div>
        )}

        {/* Danh sách tác giả */}
        {!isLoading && filteredAuthors.length > 0 && (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            {filteredAuthors.map((author) => (
              <div
                key={author.id}
                className="group bg-white dark:bg-stone-900 rounded-3xl border border-stone-200/80 dark:border-stone-800 p-6 flex flex-col justify-between shadow-xs hover:shadow-lg hover:border-brand-300 dark:hover:border-brand-700 transition-all duration-300"
              >
                <div>
                  {/* Avatar & Badges */}
                  <div className="flex items-start justify-between gap-3 mb-4">
                    <div className="relative">
                      {author.avatarUrl ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img
                          src={author.avatarUrl}
                          alt={author.displayName}
                          className="w-16 h-16 rounded-full object-cover border-2 border-brand-500 shadow-sm group-hover:scale-105 transition-transform"
                        />
                      ) : (
                        <div className="w-16 h-16 rounded-full bg-brand-100 dark:bg-brand-950/60 text-brand-700 dark:text-brand-300 flex items-center justify-center font-bold text-xl border-2 border-brand-400 shadow-sm group-hover:scale-105 transition-transform">
                          {author.displayName.charAt(0).toUpperCase()}
                        </div>
                      )}
                      <div className="absolute -bottom-1 -right-1 p-1 bg-amber-500 text-white rounded-full shadow-xs" title="Đầu bếp">
                        <ChefHat className="w-3.5 h-3.5" />
                      </div>
                    </div>

                    <div className="flex flex-col items-end gap-1">
                      <span className="inline-flex items-center gap-1 text-[11px] font-bold px-2 py-0.5 rounded-full bg-amber-50 text-amber-700 dark:bg-amber-950/50 dark:text-amber-300 border border-amber-200 dark:border-amber-800">
                        <ChefHat className="w-3 h-3" />
                        Đầu bếp
                      </span>
                      <span className="inline-flex items-center gap-1 text-[11px] text-stone-500 dark:text-stone-400 mt-1">
                        <BookOpen className="w-3 h-3 text-brand-600" />
                        <strong>{author.recipeCount}</strong> món
                      </span>
                    </div>
                  </div>

                  {/* Tên & Username */}
                  <h3 className="font-bold text-base text-stone-900 dark:text-stone-100 group-hover:text-brand-600 dark:group-hover:text-brand-400 transition-colors line-clamp-1">
                    {author.displayName}
                  </h3>
                  <p className="text-xs text-stone-500 dark:text-stone-400 font-mono mb-3">
                    @{author.userName}
                  </p>

                  {/* Bio */}
                  <p className="text-xs text-stone-600 dark:text-stone-400 line-clamp-3 min-h-[3rem] mb-4">
                    {author.bio && author.bio.trim() !== ""
                      ? author.bio
                      : "Tác giả ẩm thực đầy nhiệt huyết tại CulinaryBlog, luôn sẵn sàng sẻ chia những hương vị tinh tế nhất."}
                  </p>
                </div>

                {/* Nút hành động */}
                <div className="pt-3 border-t border-stone-100 dark:border-stone-800/80">
                  <Link
                    href={`/recipes?SearchTerm=${encodeURIComponent(author.displayName)}`}
                    className="w-full inline-flex items-center justify-center gap-1.5 py-2 px-3 rounded-xl text-xs font-semibold bg-stone-50 dark:bg-stone-800 text-stone-700 dark:text-stone-200 group-hover:bg-brand-500 group-hover:text-white transition-all shadow-2xs"
                  >
                    <span>Xem các món của tác giả</span>
                    <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 transition-transform" />
                  </Link>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Trạng thái trống (Không tìm thấy kết quả) */}
        {!isLoading && filteredAuthors.length === 0 && (
          <div className="py-12 bg-white dark:bg-stone-900 rounded-3xl border border-stone-200 dark:border-stone-800 text-center">
            <EmptyState
              title="Không tìm thấy tác giả phù hợp"
              description={
                searchTerm
                  ? `Không có đầu bếp nào khớp với từ khóa "${searchTerm}". Bạn thử tìm kiếm bằng tên khác xem sao nhé!`
                  : "Chưa có tác giả nào trong hệ thống."
              }
              action={
                searchTerm ? (
                  <Button variant="outline" size="sm" onClick={() => setSearchTerm("")}>
                    Xóa từ khóa tìm kiếm
                  </Button>
                ) : undefined
              }
            />
          </div>
        )}

        {/* Call-to-action Banner dành cho Độc giả */}
        <div className="mt-16 rounded-3xl bg-linear-to-r from-amber-500/10 via-brand-500/10 to-orange-500/10 dark:from-brand-950/30 dark:to-amber-950/30 border border-brand-200 dark:border-brand-800/50 p-8 sm:p-10 text-center space-y-4">
          <div className="w-12 h-12 mx-auto rounded-2xl bg-brand-600 text-white flex items-center justify-center shadow-md">
            <ChefHat className="w-7 h-7" />
          </div>
          <h2 className="text-2xl font-bold text-stone-900 dark:text-stone-100">
            Bạn cũng có niềm đam mê nấu nướng và sáng tạo món ăn?
          </h2>
          <p className="max-w-xl mx-auto text-sm text-stone-600 dark:text-stone-400">
            Hãy đăng ký tài khoản và gửi yêu cầu nâng cấp vai trò thành <strong>Đầu bếp</strong> trong trang Hồ sơ cá nhân để bắt đầu chia sẻ công thức của riêng bạn cùng hàng ngàn độc giả!
          </p>
          <div className="pt-2 flex justify-center gap-3">
            <Button variant="primary" href="/profile">
              Gửi yêu cầu trở thành Đầu bếp
            </Button>
            <Button variant="outline" href="/recipes">
              Khám phá món ăn
            </Button>
          </div>
        </div>
      </main>
    </div>
  );
}

