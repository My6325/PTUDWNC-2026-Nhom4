"use client";

import type { ReactNode } from "react";
import { useAuthStore } from "@/stores/auth-store";
import { Button } from "@/components/ui/Button";

export function RecipeAccess({ children, admin = false }: { children: ReactNode; admin?: boolean }) {
  const { isLoading, isAuthenticated, user } = useAuthStore();
  if (isLoading) return <p role="status" className="p-8">Đang kiểm tra đăng nhập...</p>;
  if (!isAuthenticated) return <div className="p-8 space-y-4"><p>Đăng nhập để quản lý công thức của bạn.</p><Button href="/login">Đăng nhập</Button></div>;
  const roles = user?.roles.map(role => role.toLowerCase()) ?? [];
  if (!roles.includes("admin") && (admin || !roles.includes("author")))
    return <p role="alert" className="p-8">Bạn không có quyền truy cập trang này.</p>;
  return <>{children}</>;
}
