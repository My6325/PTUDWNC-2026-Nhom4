"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { useRouter, usePathname } from "next/navigation";
import { ChefHat, Search, Menu, X, User, LogOut, PenSquare, Shield } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { authService } from "@/services/authService";
import { useAuthStore } from "@/stores/auth-store";
import { cn } from "@/lib/utils";

export const SiteHeader: React.FC = () => {
  const router = useRouter();
  const pathname = usePathname();
  const [searchTerm, setSearchTerm] = useState("");
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  // Zustand Store phản ứng tức thì với trạng thái auth
  const { user, isAuthenticated, logout: storeLogout, initAuth } = useAuthStore();

  useEffect(() => {
    initAuth();
  }, [initAuth]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchTerm.trim()) {
      router.push(`/recipes?SearchTerm=${encodeURIComponent(searchTerm.trim())}`);
      setIsMobileMenuOpen(false);
    }
  };

  const handleLogout = async () => {
    try {
      await authService.logout();
    } catch {
      // Bỏ qua lỗi mạng khi logout
    } finally {
      storeLogout();
      router.push("/");
    }
  };

  const navLinks = [
    { label: "Trang chủ", href: "/" },
    { label: "Công thức", href: "/recipes" },
    { label: "Danh mục", href: "/categories" },
  ];

  // Phân quyền RBAC cho giao diện: Admin, Author (Đầu bếp), User (Độc giả/Người dùng)
  const isAdmin = user?.roles?.some((r) => r.toLowerCase() === "admin") ?? false;
  const isAuthor = user?.roles?.some((r) => r.toLowerCase() === "author") ?? false;
  const canCreateRecipe = isAdmin || isAuthor;

  return (
    <header className="sticky top-0 z-50 w-full border-b border-surface-border bg-white/95 dark:bg-stone-900/95 backdrop-blur supports-[backdrop-filter]:bg-white/80 dark:supports-[backdrop-filter]:bg-stone-900/80 transition-colors">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16 md:h-20 gap-4">
          {/* Logo Brand */}
          <Link
            href="/"
            className="flex items-center gap-2.5 text-stone-900 dark:text-stone-100 hover:opacity-90 transition-opacity focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 rounded-lg p-1"
          >
            <div className="w-10 h-10 rounded-xl bg-brand-600 text-white flex items-center justify-center shadow-md shadow-brand-600/20">
              <ChefHat className="w-6 h-6" />
            </div>
            <div className="flex flex-col">
              <span className="font-extrabold text-lg md:text-xl tracking-tight leading-none text-stone-900 dark:text-white">
                Culinary<span className="text-brand-600 dark:text-brand-400">Blog</span>
              </span>
              <span className="text-[10px] font-semibold tracking-wider text-stone-500 dark:text-stone-400 uppercase mt-0.5">
                Ẩm thực & Nấu ăn
              </span>
            </div>
          </Link>

          {/* Desktop Navigation */}
          <nav className="hidden md:flex items-center gap-1 lg:gap-2" aria-label="Điều hướng chính">
            {navLinks.map((link) => {
              const isActive = pathname === link.href || (link.href !== "/" && pathname?.startsWith(link.href));
              return (
                <Link
                  key={link.href}
                  href={link.href}
                  className={cn(
                    "px-3.5 py-2 rounded-xl text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500",
                    isActive
                      ? "text-brand-600 dark:text-brand-400 bg-brand-50 dark:bg-brand-950/50"
                      : "text-stone-600 dark:text-stone-300 hover:text-stone-900 dark:hover:text-white hover:bg-stone-100 dark:hover:bg-stone-800"
                  )}
                >
                  {link.label}
                </Link>
              );
            })}
          </nav>

          {/* Desktop Search Bar */}
          <div className="hidden lg:flex flex-1 max-w-xs xl:max-w-sm mx-4">
            <form onSubmit={handleSearchSubmit} className="relative w-full" role="search">
              <input
                type="search"
                placeholder="Tìm món ngon, nguyên liệu..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full pl-9 pr-4 py-2 text-sm bg-stone-100 dark:bg-stone-800/80 border border-transparent dark:border-stone-700/60 rounded-full focus:bg-white dark:focus:bg-stone-900 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none transition-all placeholder:text-stone-400 text-stone-900 dark:text-stone-100"
                aria-label="Tìm kiếm công thức nấu ăn"
              />
              <Search className="absolute left-3 top-2.5 w-4 h-4 text-stone-400 pointer-events-none" aria-hidden="true" />
            </form>
          </div>

          {/* User Account & Actions - Phân rõ 4 trạng thái: Guest, Admin, Author, User */}
          <div className="hidden md:flex items-center gap-2">
            {isAuthenticated ? (
              <div className="flex items-center gap-2">
                {/* 1. Nút Quản trị - CHỈ DÀNH RIÊNG CHO ADMIN */}
                {isAdmin && (
                  <Button
                    href="/admin/categories"
                    variant="outline"
                    size="sm"
                    className="border-brand-500/50 text-brand-600 dark:text-brand-400 bg-brand-50/70 dark:bg-brand-950/40 font-semibold shadow-sm hover:bg-brand-100 dark:hover:bg-brand-900/40"
                    leftIcon={<Shield className="w-3.5 h-3.5" />}
                    title="Truy cập khu vực Quản trị hệ thống & Danh mục"
                  >
                    Quản trị
                  </Button>
                )}

                {/* 2. Nút Viết món mới - CHỈ HIỂN THỊ VỚI ĐẦU BẾP (AUTHOR) VÀ QUẢN TRỊ VIÊN (ADMIN) */}
                {canCreateRecipe && (
                  <Button
                    href="/recipes/create"
                    variant="default"
                    size="sm"
                    leftIcon={<PenSquare className="w-4 h-4" />}
                    title="Đăng tải công thức nấu ăn mới"
                  >
                    Viết món mới
                  </Button>
                )}

                {/* 3. Nút Hồ sơ người dùng kèm Role Badge nhận diện trực quan */}
                <Button
                  href="/profile"
                  variant="ghost"
                  size="sm"
                  className="flex items-center gap-1.5"
                  leftIcon={
                    isAdmin ? (
                      <Shield className="w-4 h-4 text-brand-600 dark:text-brand-400" />
                    ) : isAuthor ? (
                      <ChefHat className="w-4 h-4 text-amber-600 dark:text-amber-400" />
                    ) : (
                      <User className="w-4 h-4 text-stone-500" />
                    )
                  }
                >
                  <span className="max-w-[120px] truncate">{user?.displayName || "Tài khoản"}</span>
                  {isAdmin && (
                    <span className="text-[10px] font-bold px-1.5 py-0.5 rounded bg-brand-100 text-brand-700 dark:bg-brand-900/60 dark:text-brand-300">
                      Admin
                    </span>
                  )}
                  {!isAdmin && isAuthor && (
                    <span className="text-[10px] font-bold px-1.5 py-0.5 rounded bg-amber-100 text-amber-800 dark:bg-amber-950/60 dark:text-amber-300">
                      Đầu bếp
                    </span>
                  )}
                  {!isAdmin && !isAuthor && (
                    <span className="text-[10px] font-medium px-1.5 py-0.5 rounded bg-stone-100 text-stone-600 dark:bg-stone-800 dark:text-stone-300">
                      Thành viên
                    </span>
                  )}
                </Button>

                {/* 4. Nút Đăng xuất */}
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleLogout}
                  leftIcon={<LogOut className="w-4 h-4 text-stone-400" />}
                  title="Đăng xuất khỏi hệ thống"
                >
                  Thoát
                </Button>
              </div>
            ) : (
              /* Trạng thái Khách vãng lai (Guest - Chưa đăng nhập) */
              <div className="flex items-center gap-2">
                <Button href="/login" variant="ghost" size="sm">
                  Đăng nhập
                </Button>
                <Button href="/register" variant="default" size="sm">
                  Đăng ký
                </Button>
              </div>
            )}
          </div>

          {/* Mobile Menu Button */}
          <div className="flex md:hidden items-center gap-2">
            <button
              type="button"
              onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
              className="p-2 text-stone-600 dark:text-stone-300 hover:text-stone-900 dark:hover:text-white rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
              aria-label={isMobileMenuOpen ? "Đóng menu" : "Mở menu"}
              aria-expanded={isMobileMenuOpen}
            >
              {isMobileMenuOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
            </button>
          </div>
        </div>

        {/* Mobile Dropdown Menu */}
        {isMobileMenuOpen && (
          <div className="md:hidden py-4 border-t border-surface-border space-y-4 animate-in slide-in-from-top duration-200">
            {/* Search trên mobile */}
            <form onSubmit={handleSearchSubmit} className="relative w-full" role="search">
              <input
                type="search"
                placeholder="Tìm món ngon, nguyên liệu..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full pl-9 pr-4 py-2.5 text-sm bg-stone-100 dark:bg-stone-800 border border-transparent rounded-lg focus:bg-white dark:focus:bg-stone-900 focus:border-brand-500 focus:outline-none text-stone-900 dark:text-stone-100"
                aria-label="Tìm kiếm công thức nấu ăn"
              />
              <Search className="absolute left-3 top-3 w-4 h-4 text-stone-400 pointer-events-none" aria-hidden="true" />
            </form>

            {/* Links */}
            <nav className="flex flex-col space-y-1" aria-label="Điều hướng di động">
              {navLinks.map((link) => (
                <Link
                  key={link.href}
                  href={link.href}
                  onClick={() => setIsMobileMenuOpen(false)}
                  className="px-3 py-2 rounded-xl text-sm font-semibold text-stone-700 dark:text-stone-200 hover:bg-stone-100 dark:hover:bg-stone-800"
                >
                  {link.label}
                </Link>
              ))}
            </nav>

            {/* Auth Buttons trên Mobile */}
            <div className="pt-2 border-t border-surface-border flex flex-col gap-2">
              {isAuthenticated ? (
                <>
                  {isAdmin && (
                    <Button
                      href="/admin/categories"
                      variant="outline"
                      size="sm"
                      className="w-full justify-start border-brand-500/50 text-brand-600 dark:text-brand-400 bg-brand-50/70 dark:bg-brand-950/40 font-semibold"
                      leftIcon={<Shield className="w-4 h-4" />}
                      onClick={() => setIsMobileMenuOpen(false)}
                    >
                      Bảng Quản trị (Admin)
                    </Button>
                  )}
                  {canCreateRecipe && (
                    <Button
                      href="/recipes/create"
                      variant="default"
                      size="sm"
                      className="w-full justify-start"
                      leftIcon={<PenSquare className="w-4 h-4" />}
                      onClick={() => setIsMobileMenuOpen(false)}
                    >
                      Viết món mới
                    </Button>
                  )}
                  <Button
                    href="/profile"
                    variant="outline"
                    size="sm"
                    className="w-full justify-between"
                    leftIcon={
                      isAdmin ? (
                        <Shield className="w-4 h-4 text-brand-600" />
                      ) : isAuthor ? (
                        <ChefHat className="w-4 h-4 text-amber-600" />
                      ) : (
                        <User className="w-4 h-4 text-stone-500" />
                      )
                    }
                    onClick={() => setIsMobileMenuOpen(false)}
                  >
                    <span className="truncate">{user?.displayName || "Trang cá nhân"}</span>
                    {isAdmin && (
                      <span className="text-[10px] font-bold px-2 py-0.5 rounded bg-brand-100 text-brand-700">
                        Admin
                      </span>
                    )}
                    {!isAdmin && isAuthor && (
                      <span className="text-[10px] font-bold px-2 py-0.5 rounded bg-amber-100 text-amber-800">
                        Đầu bếp
                      </span>
                    )}
                    {!isAdmin && !isAuthor && (
                      <span className="text-[10px] font-medium px-2 py-0.5 rounded bg-stone-100 text-stone-600">
                        Thành viên
                      </span>
                    )}
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => {
                      setIsMobileMenuOpen(false);
                      handleLogout();
                    }}
                    className="w-full justify-start text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950/40"
                    leftIcon={<LogOut className="w-4 h-4" />}
                  >
                    Đăng xuất
                  </Button>
                </>
              ) : (
                <div className="grid grid-cols-2 gap-2">
                  <Button
                    href="/login"
                    variant="outline"
                    size="sm"
                    className="w-full"
                    onClick={() => setIsMobileMenuOpen(false)}
                  >
                    Đăng nhập
                  </Button>
                  <Button
                    href="/register"
                    variant="default"
                    size="sm"
                    className="w-full"
                    onClick={() => setIsMobileMenuOpen(false)}
                  >
                    Đăng ký
                  </Button>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </header>
  );
};
