import { Suspense } from "react";
import type { Metadata } from "next";
import Link from "next/link";
import { AdminNavTabs } from "@/components/admin/AdminNavTabs";
import { Button } from "@/components/ui/Button";
import { RecipeAccess } from "@/components/recipes/RecipeAccess";
import { ManagedRecipes } from "@/components/recipes/ManagedRecipes";
export const metadata: Metadata = { title: "Hàng đợi duyệt bài" };

export default function Page() {
  return (
    <RecipeAccess admin>
      <div className="container-custom mx-auto max-w-7xl space-y-8 px-4 py-8 sm:px-6">
        <nav aria-label="Đường dẫn" className="flex flex-wrap items-center gap-2 text-sm text-stone-500">
          <Link href="/" className="hover:underline">Trang chủ</Link>
          <span aria-hidden="true">/</span>
          <Link href="/admin" className="hover:underline">Quản trị</Link>
          <span aria-hidden="true">/</span>
          <span aria-current="page">Hàng đợi duyệt bài</span>
        </nav>
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-extrabold text-stone-900 dark:text-stone-100 sm:text-3xl">Kiểm duyệt Công thức Món ăn</h1>
            <p className="mt-2 text-sm text-stone-500">Kiểm tra nội dung trước khi công khai. Bài bị từ chối trở về bản nháp kèm lý do.</p>
          </div>
          <Button href="/admin/recipes" variant="outline">Quản lý công thức</Button>
        </div>
        <AdminNavTabs />
        <Suspense fallback={<p role="status">Đang tải công thức...</p>}>
          <ManagedRecipes reviewQueue embedded />
        </Suspense>
      </div>
    </RecipeAccess>
  );
}
