"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/utils/cn";

const links = [
  { href: "/admin/categories", label: "Danh mục ẩm thực" },
  { href: "/admin/recipes", label: "Quản lý công thức" },
  { href: "/admin/recipes/review", label: "Hàng đợi duyệt bài" },
  { href: "/admin/users", label: "Người dùng & phân quyền" },
];

export function AdminNavTabs() {
  const pathname = usePathname();
  const active = links
    .filter(({ href }) => pathname === href || pathname.startsWith(`${href}/`))
    .sort((a, b) => b.href.length - a.href.length)[0]?.href;

  return (
    <nav aria-label="Điều hướng quản trị" className="flex w-full flex-wrap gap-2">
      {links.map(({ href, label }) => (
        <Link
          key={href}
          href={href}
          aria-current={active === href ? "page" : undefined}
          className={cn(
            "rounded-xl border px-3.5 py-2 text-sm font-semibold transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600",
            active === href
              ? "border-brand-200 bg-brand-50 text-brand-700 dark:border-brand-900 dark:bg-brand-950/50 dark:text-brand-300"
              : "border-stone-200 text-stone-600 hover:bg-stone-100 hover:text-stone-900 dark:border-stone-800 dark:text-stone-400 dark:hover:bg-stone-800 dark:hover:text-stone-100",
          )}
        >
          {label}
        </Link>
      ))}
    </nav>
  );
}
