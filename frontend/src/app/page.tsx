export default function HomePage() {
  return (
    <main className="flex-1 flex flex-col items-center justify-center p-8 text-center">
      <div className="max-w-2xl bg-white dark:bg-zinc-900 shadow-xl rounded-2xl p-8 border border-zinc-200 dark:border-zinc-800">
        <h1 className="text-4xl font-extrabold tracking-tight text-emerald-600 dark:text-emerald-400 mb-4">
          Culinary Blog
        </h1>
        <p className="text-lg text-zinc-600 dark:text-zinc-300 mb-6">
          Nền tảng chia sẻ công thức ẩm thực chuyên nghiệp xây dựng bằng .NET 10 Minimal APIs & Next.js 15.
        </p>
        <div className="inline-flex items-center gap-2 px-4 py-2 bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 rounded-lg text-sm font-semibold border border-emerald-200 dark:border-emerald-800">
          🚀 Frontend Next.js 15 + TypeScript đã sẵn sàng kết nối API
        </div>
      </div>
    </main>
  );
}
