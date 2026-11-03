import type { Metadata } from "next";
export const metadata: Metadata = { title: "Duyệt công thức", robots: { index: false, follow: false } };
export default function Layout({ children }: { children: React.ReactNode }) { return children; }
