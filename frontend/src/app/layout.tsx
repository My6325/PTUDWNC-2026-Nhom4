import type { Metadata } from "next";
import { SiteHeader } from "@/components/layout/SiteHeader";
import { SiteFooter } from "@/components/layout/SiteFooter";
import { AuthInitializer } from "@/components/layout/AuthInitializer";
import "./globals.css";

export const metadata: Metadata = {
  title: "Culinary Blog - Khám Phá Công Thức Ẩm Thực Chuẩn Vị",
  description: "Nền tảng chia sẻ và học hỏi công thức nấu ăn phong phú, chuẩn vị và đầy đủ dinh dưỡng",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi">
      <body className="antialiased min-h-screen flex flex-col bg-surface-warm text-stone-900 dark:bg-stone-950 dark:text-stone-100 selection:bg-brand-500 selection:text-white transition-colors">
        <AuthInitializer />
        <SiteHeader />
        <main className="flex-1">
          {children}
        </main>
        <SiteFooter />
      </body>
    </html>
  );
}
