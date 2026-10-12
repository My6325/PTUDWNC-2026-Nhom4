import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Culinary Blog - Khám Phá Công Thức Ẩm Thực",
  description: "Nền tảng chia sẻ và học hỏi công thức nấu ăn chuyên nghiệp",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi">
      <body className="antialiased min-h-screen flex flex-col">
        {children}
      </body>
    </html>
  );
}
