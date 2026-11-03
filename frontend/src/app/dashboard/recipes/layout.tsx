import type { Metadata } from "next";
export const metadata: Metadata = { title: "Món ăn của tôi", robots: { index: false, follow: false } };
export default function Layout({ children }: { children: React.ReactNode }) { return children; }
