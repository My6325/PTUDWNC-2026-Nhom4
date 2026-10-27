"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import {
  Clock,
  Users,
  ChefHat,
  Calendar,
  Share2,
  Printer,
  ChevronRight,
  ArrowLeft,
  CheckCircle2,
  Play,
  Pause,
  RotateCcw,
  Utensils,
  AlertTriangle,
  Info,
  Check,
  Flame,
  Activity,
  Heart,
} from "lucide-react";
import { recipeService } from "@/services/recipeService";
import { RecipeDetailDto, RecipeStepDto } from "@/types/recipe";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Skeleton } from "@/components/ui/Skeleton";
import { Alert } from "@/components/ui/Alert";
import { extractApiErrorMessage } from "@/utils/apiError";
import { cn } from "@/utils/cn";
import { toRecipeJsonLd } from "@/utils/recipeSeo";

/**
 * Component Đồng hồ bấm giờ đếm ngược cho từng bước nấu ăn
 */
const StepTimer: React.FC<{ timerMinutes: number; stepNumber: number }> = ({
  timerMinutes,
  stepNumber,
}) => {
  const totalSeconds = timerMinutes * 60;
  const [secondsLeft, setSecondsLeft] = useState<number>(totalSeconds);
  const [isRunning, setIsRunning] = useState<boolean>(false);
  const [isCompleted, setIsCompleted] = useState<boolean>(false);

  useEffect(() => {
    let interval: NodeJS.Timeout | null = null;
    if (isRunning && secondsLeft > 0) {
      interval = setInterval(() => {
        setSecondsLeft((prev) => {
          if (prev <= 1) {
            setIsRunning(false);
            setIsCompleted(true);
            // Phát âm thanh bíp nhẹ bằng Web Audio API
            try {
              const audioCtx = new (window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext)();
              const osc = audioCtx.createOscillator();
              const gain = audioCtx.createGain();
              osc.connect(gain);
              gain.connect(audioCtx.destination);
              osc.type = "sine";
              osc.frequency.setValueAtTime(800, audioCtx.currentTime);
              gain.gain.setValueAtTime(0.3, audioCtx.currentTime);
              osc.start();
              osc.stop(audioCtx.currentTime + 0.5);
            } catch {
              // Trình duyệt chặn autoplay audio nếu chưa có tương tác
            }
            return 0;
          }
          return prev - 1;
        });
      }, 1000);
    }
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [isRunning, secondsLeft]);

  const toggleTimer = () => {
    if (secondsLeft === 0) {
      setSecondsLeft(totalSeconds);
      setIsCompleted(false);
      setIsRunning(true);
    } else {
      setIsRunning(!isRunning);
    }
  };

  const resetTimer = () => {
    setIsRunning(false);
    setSecondsLeft(totalSeconds);
    setIsCompleted(false);
  };

  const minutes = Math.floor(secondsLeft / 60);
  const seconds = secondsLeft % 60;
  const formattedTime = `${minutes.toString().padStart(2, "0")}:${seconds.toString().padStart(2, "0")}`;

  return (
    <div
      className={cn(
        "inline-flex flex-wrap items-center gap-3 rounded-xl px-3.5 py-2 text-sm border transition-all my-2",
        isCompleted
          ? "bg-emerald-50 dark:bg-emerald-950/40 border-emerald-300 dark:border-emerald-700 text-emerald-800 dark:text-emerald-200"
          : isRunning
          ? "bg-amber-50 dark:bg-amber-950/40 border-amber-300 dark:border-amber-700 text-amber-900 dark:text-amber-100 shadow-sm"
          : "bg-stone-100 dark:bg-stone-800 border-stone-200 dark:border-stone-700 text-stone-700 dark:text-stone-300"
      )}
    >
      <div className="flex items-center gap-1.5 font-semibold">
        <Clock className={cn("h-4 w-4", isRunning && "animate-spin text-amber-600 dark:text-amber-400")} />
        <span>Hẹn giờ bước {stepNumber}:</span>
        <span className="font-mono text-base font-bold tracking-wider">{formattedTime}</span>
      </div>

      <div className="flex items-center gap-1.5">
        <button
          type="button"
          onClick={toggleTimer}
          className={cn(
            "inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-xs font-semibold shadow-xs transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500",
            isRunning
              ? "bg-amber-600 hover:bg-amber-700 text-white"
              : isCompleted
              ? "bg-emerald-600 hover:bg-emerald-700 text-white"
              : "bg-brand-600 hover:bg-brand-700 text-white"
          )}
          aria-label={isRunning ? "Tạm dừng hẹn giờ" : "Bắt đầu hẹn giờ"}
        >
          {isRunning ? (
            <>
              <Pause className="h-3 w-3" /> Tạm dừng
            </>
          ) : isCompleted ? (
            <>
              <RotateCcw className="h-3 w-3" /> Nấu lại ({timerMinutes}p)
            </>
          ) : (
            <>
              <Play className="h-3 w-3 fill-current" /> Bắt đầu ({timerMinutes}p)
            </>
          )}
        </button>

        {(isRunning || secondsLeft < totalSeconds) && (
          <button
            type="button"
            onClick={resetTimer}
            className="p-1 rounded-lg text-stone-500 hover:text-stone-800 dark:hover:text-stone-200 hover:bg-stone-200 dark:hover:bg-stone-700 transition-colors"
            title="Đặt lại hẹn giờ"
            aria-label="Đặt lại hẹn giờ"
          >
            <RotateCcw className="h-3.5 w-3.5" />
          </button>
        )}
      </div>

      {isCompleted && (
        <span className="flex items-center gap-1 text-xs font-bold text-emerald-600 dark:text-emerald-400 animate-pulse">
          <CheckCircle2 className="h-3.5 w-3.5" /> Đã hoàn thành!
        </span>
      )}
    </div>
  );
};

export default function RecipeDetailPage() {
  const params = useParams();
  const router = useRouter();
  const slug = typeof params?.slug === "string" ? params.slug : "";

  const [recipe, setRecipe] = useState<RecipeDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [isNotFound, setIsNotFound] = useState<boolean>(false);

  // Danh sách ID nguyên liệu đã tick chọn
  const [checkedIngredients, setCheckedIngredients] = useState<Record<string, boolean>>({});
  // Trạng thái copy link chia sẻ
  const [copied, setCopied] = useState<boolean>(false);

  useEffect(() => {
    if (!slug) return;

    let isMounted = true;
    const fetchRecipe = async () => {
      setIsLoading(true);
      setError(null);
      setIsNotFound(false);
      try {
        const data = await recipeService.getRecipeBySlug(slug);
        if (isMounted) {
          setRecipe(data);
        }
      } catch (err: unknown) {
        if (!isMounted) return;
        const status = (err as { response?: { status?: number } })?.response?.status;
        if (status === 404) {
          setIsNotFound(true);
        } else {
          setError(extractApiErrorMessage(err, "Không thể tải thông tin công thức nấu ăn. Vui lòng thử lại."));
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    };

    fetchRecipe();

    return () => {
      isMounted = false;
    };
  }, [slug]);

  const toggleIngredient = (index: number) => {
    setCheckedIngredients((prev) => ({
      ...prev,
      [index]: !prev[index],
    }));
  };

  const handlePrint = () => {
    if (typeof window !== "undefined") {
      window.print();
    }
  };

  const handleShare = async () => {
    if (typeof window === "undefined") return;
    try {
      if (navigator.share) {
        await navigator.share({
          title: recipe?.title || "Culinary Blog Recipe",
          text: recipe?.description || "Xem công thức nấu ăn trên Culinary Blog",
          url: window.location.href,
        });
      } else {
        await navigator.clipboard.writeText(window.location.href);
        setCopied(true);
        setTimeout(() => setCopied(false), 3000);
      }
    } catch {
      // Bỏ qua lỗi do người dùng hủy chia sẻ
    }
  };

  // Định dạng ngày hiển thị an toàn
  const formattedDate = recipe?.datePublished
    ? (() => {
        try {
          return new Date(recipe.datePublished).toLocaleDateString("vi-VN", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
          });
        } catch {
          return "";
        }
      })()
    : "";

  // Chọn ảnh chính: Ưu tiên ảnh isPrimary, sau đó ảnh đầu tiên trong images
  const primaryImage =
    recipe?.images?.find((img) => img.isPrimary)?.originalUrl ||
    recipe?.images?.[0]?.originalUrl ||
    null;

  // Lấy các ảnh phụ nếu có
  const galleryImages = recipe?.images?.filter((img) => img.originalUrl !== primaryImage) || [];

  // Trạng thái đang tải (Loading Skeleton)
  if (isLoading) {
    return (
      <div className="min-h-screen bg-surface-warm dark:bg-stone-950/50 py-8">
        <div className="container-custom max-w-4xl mx-auto px-4 space-y-6">
          <Skeleton className="h-6 w-48 rounded-md" />
          <div className="space-y-3">
            <Skeleton className="h-10 w-3/4 rounded-lg" />
            <Skeleton className="h-5 w-full rounded-md" />
            <Skeleton className="h-5 w-2/3 rounded-md" />
          </div>
          <div className="flex gap-4">
            <Skeleton className="h-10 w-32 rounded-full" />
            <Skeleton className="h-10 w-32 rounded-full" />
          </div>
          <Skeleton className="h-[400px] w-full rounded-2xl" />
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <Skeleton className="h-24 rounded-xl" />
            <Skeleton className="h-24 rounded-xl" />
            <Skeleton className="h-24 rounded-xl" />
            <Skeleton className="h-24 rounded-xl" />
          </div>
          <div className="space-y-4">
            <Skeleton className="h-8 w-48 rounded-md" />
            <Skeleton className="h-20 w-full rounded-xl" />
            <Skeleton className="h-20 w-full rounded-xl" />
          </div>
        </div>
      </div>
    );
  }

  // Trạng thái Không tìm thấy (404)
  if (isNotFound) {
    return (
      <div className="min-h-[70vh] flex items-center justify-center py-16 px-4">
        <div className="max-w-md w-full text-center space-y-5">
          <div className="w-16 h-16 mx-auto rounded-full bg-amber-100 dark:bg-amber-900/40 flex items-center justify-center text-amber-600 dark:text-amber-400">
            <Utensils className="w-8 h-8" />
          </div>
          <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-100">
            Không tìm thấy công thức
          </h1>
          <p className="text-sm text-stone-600 dark:text-stone-400">
            Công thức bạn đang tìm kiếm không tồn tại hoặc đã được chuyển sang trạng thái lưu trữ.
          </p>
          <div className="flex justify-center gap-3 pt-2">
            <Button variant="primary" href="/recipes" leftIcon={<ArrowLeft className="w-4 h-4" />}>
              Xem tất cả công thức
            </Button>
            <Button variant="outline" href="/">
              Về trang chủ
            </Button>
          </div>
        </div>
      </div>
    );
  }

  // Trạng thái Lỗi API
  if (error || !recipe) {
    return (
      <div className="container-custom max-w-2xl mx-auto py-16 px-4">
        <Alert
          variant="error"
          title="Không thể tải công thức"
          className="mb-6"
        >
          {error || "Đã xảy ra lỗi không xác định."}
        </Alert>
        <div className="flex justify-center gap-3">
          <Button variant="primary" onClick={() => window.location.reload()}>
            Tải lại trang
          </Button>
          <Button variant="outline" href="/recipes">
            Quay lại danh sách
          </Button>
        </div>
      </div>
    );
  }

  const jsonLd = recipe ? toRecipeJsonLd(recipe as any) : null;

  return (
    <div className="min-h-screen bg-surface-warm dark:bg-stone-950/40 pb-20">
      {jsonLd && (
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd).replace(/</g, "\\u003c") }}
        />
      )}
      {/* Thông báo copy link */}
      {copied && (
        <div className="fixed bottom-6 right-6 z-50 flex items-center gap-2 rounded-xl bg-stone-900 text-white px-4 py-3 shadow-lg text-sm font-medium animate-in fade-in slide-in-from-bottom-5">
          <Check className="h-4 w-4 text-emerald-400" />
          Đã sao chép liên kết vào bộ nhớ tạm!
        </div>
      )}

      {/* Header & Breadcrumb */}
      <div className="border-b border-surface-border dark:border-stone-800 bg-white dark:bg-stone-900/80">
        <div className="container-custom max-w-4xl mx-auto px-4 py-3">
          <nav className="flex items-center gap-1.5 text-xs text-stone-500 dark:text-stone-400 overflow-x-auto whitespace-nowrap">
            <Link href="/" className="hover:text-stone-900 dark:hover:text-stone-100 transition-colors">
              Trang chủ
            </Link>
            <ChevronRight className="h-3 w-3 shrink-0" />
            <Link href="/recipes" className="hover:text-stone-900 dark:hover:text-stone-100 transition-colors">
              Công thức
            </Link>
            {recipe.category && (
              <>
                <ChevronRight className="h-3 w-3 shrink-0" />
                <Link
                  href={`/categories/${recipe.category.slug}`}
                  className="hover:text-stone-900 dark:hover:text-stone-100 transition-colors"
                >
                  {recipe.category.name}
                </Link>
              </>
            )}
            <ChevronRight className="h-3 w-3 shrink-0" />
            <span className="text-stone-800 dark:text-stone-200 font-medium truncate max-w-[200px] md:max-w-xs">
              {recipe.title}
            </span>
          </nav>
        </div>
      </div>

      <div className="container-custom max-w-4xl mx-auto px-4 pt-8">
        {/* Tiêu đề & Danh mục */}
        <div className="space-y-4">
          <div className="flex flex-wrap items-center gap-2">
            {recipe.category ? (
              <Link href={`/categories/${recipe.category.slug}`}>
                <Badge variant="brand" size="md" className="hover:opacity-90 cursor-pointer">
                  {recipe.category.name}
                </Badge>
              </Link>
            ) : (
              <Badge variant="outline" size="md" className="text-stone-500 dark:text-stone-400 border-stone-300 dark:border-stone-700">
                Chưa phân loại
              </Badge>
            )}
            <Badge difficulty={recipe.difficulty} size="md" />
          </div>

          <h1 className="text-3xl sm:text-4xl lg:text-5xl font-extrabold text-stone-900 dark:text-stone-50 tracking-tight leading-tight">
            {recipe.title}
          </h1>

          {recipe.description && (
            <p className="text-base sm:text-lg text-stone-600 dark:text-stone-300 leading-relaxed font-serif italic">
              {recipe.description}
            </p>
          )}

          {/* Thông tin tác giả, ngày đăng và nút chia sẻ */}
          <div className="flex flex-wrap items-center justify-between gap-4 pt-2 pb-4 border-b border-stone-200/80 dark:border-stone-800">
            <div className="flex items-center gap-3">
              {recipe.author?.avatarUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={recipe.author.avatarUrl}
                  alt={recipe.author.name}
                  className="h-10 w-10 rounded-full object-cover border border-stone-200 dark:border-stone-700"
                />
              ) : (
                <div className="h-10 w-10 rounded-full bg-brand-100 dark:bg-brand-950/60 text-brand-700 dark:text-brand-300 flex items-center justify-center font-bold text-sm">
                  {recipe.author?.name ? recipe.author.name.charAt(0).toUpperCase() : <ChefHat className="h-5 w-5" />}
                </div>
              )}
              <div>
                <p className="text-sm font-bold text-stone-900 dark:text-stone-100">
                  {recipe.author?.name || "Đầu bếp ẩm thực"}
                </p>
                <div className="flex items-center gap-3 text-xs text-stone-500 dark:text-stone-400">
                  <span className="flex items-center gap-1">
                    <Calendar className="h-3.5 w-3.5" />
                    <span suppressHydrationWarning>{formattedDate}</span>
                  </span>
                </div>
              </div>
            </div>

            {/* Nút In & Chia sẻ */}
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={handlePrint}
                leftIcon={<Printer className="h-4 w-4" />}
                className="hidden sm:inline-flex"
              >
                In công thức
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={handleShare}
                leftIcon={<Share2 className="h-4 w-4" />}
              >
                Chia sẻ
              </Button>
              <a
                href="#recipe-card-box"
                className="inline-flex items-center gap-1 px-3 py-1.5 rounded-xl bg-brand-600 hover:bg-brand-700 text-white text-xs font-bold transition-colors shadow-xs"
              >
                Nhảy đến công thức ↓
              </a>
            </div>
          </div>
        </div>

        {/* Khung ảnh đại diện chính */}
        <div className="mt-6 overflow-hidden rounded-2xl bg-stone-100 dark:bg-stone-900 border border-stone-200/80 dark:border-stone-800 shadow-sm">
          {primaryImage ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={primaryImage}
              alt={recipe.title}
              className="w-full h-auto max-h-[520px] object-cover"
            />
          ) : (
            <div className="py-24 flex flex-col items-center justify-center text-stone-400 space-y-2">
              <Utensils className="h-16 w-16 stroke-[1.2]" />
              <span className="text-sm font-medium">Culinary Blog - Hương vị món ngon</span>
            </div>
          )}
        </div>

        {/* Thư viện ảnh phụ nếu có */}
        {galleryImages.length > 0 && (
          <div className="mt-4 grid grid-cols-3 sm:grid-cols-4 gap-3">
            {galleryImages.map((img, idx) => (
              <div
                key={idx}
                className="aspect-square rounded-xl overflow-hidden bg-stone-100 dark:bg-stone-800 border border-stone-200 dark:border-stone-700"
              >
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={img.originalUrl}
                  alt={`${recipe.title} ảnh ${idx + 2}`}
                  className="w-full h-full object-cover hover:scale-105 transition-transform duration-300 cursor-pointer"
                />
              </div>
            ))}
          </div>
        )}

        {/* Lời dẫn / Giới thiệu món ăn nếu có instructions */}
        {recipe.instructions && (
          <div className="mt-8 prose prose-stone dark:prose-invert max-w-none text-stone-700 dark:text-stone-300 leading-relaxed bg-white dark:bg-stone-900/60 p-6 rounded-2xl border border-stone-200/80 dark:border-stone-800">
            <h2 className="text-lg font-bold text-stone-900 dark:text-stone-100 mb-3 flex items-center gap-2">
              <Info className="h-5 w-5 text-brand-600 dark:text-brand-400" />
              Tổng quan món ăn
            </h2>
            <p className="whitespace-pre-line text-sm sm:text-base leading-relaxed">
              {recipe.instructions}
            </p>
          </div>
        )}

        {/* ========================================================
            HỘP CÔNG THỨC CHÍNH (RECIPE CARD BOX - BUDGET BYTES STYLE)
            ======================================================== */}
        <section
          id="recipe-card-box"
          className="mt-10 rounded-3xl bg-white dark:bg-stone-900 border-2 border-brand-200 dark:border-brand-900/60 shadow-lg overflow-hidden"
        >
          {/* Header hộp công thức */}
          <div className="bg-gradient-to-r from-brand-600 to-brand-700 text-white p-6 sm:p-8">
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div>
                <span className="text-xs uppercase font-extrabold tracking-widest text-brand-200">
                  Công thức chuẩn vị
                </span>
                <h2 className="text-2xl sm:text-3xl font-extrabold tracking-tight mt-1">
                  {recipe.title}
                </h2>
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={handlePrint}
                className="bg-white/10 hover:bg-white/20 text-white border-white/30"
                leftIcon={<Printer className="h-4 w-4" />}
              >
                In bản in
              </Button>
            </div>

            {/* Thanh tổng quan thời gian & khẩu phần */}
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mt-6 pt-6 border-t border-white/20 text-center">
              <div className="bg-white/10 backdrop-blur-xs rounded-xl p-3">
                <span className="text-xs text-brand-100 font-medium block">Chuẩn bị</span>
                <span className="text-lg sm:text-xl font-extrabold mt-0.5 block">
                  {recipe.prepTimeMinutes} <span className="text-xs font-normal">phút</span>
                </span>
              </div>
              <div className="bg-white/10 backdrop-blur-xs rounded-xl p-3">
                <span className="text-xs text-brand-100 font-medium block">Nấu chín</span>
                <span className="text-lg sm:text-xl font-extrabold mt-0.5 block">
                  {recipe.cookTimeMinutes} <span className="text-xs font-normal">phút</span>
                </span>
              </div>
              <div className="bg-white/10 backdrop-blur-xs rounded-xl p-3">
                <span className="text-xs text-brand-100 font-medium block">Tổng cộng</span>
                <span className="text-lg sm:text-xl font-extrabold mt-0.5 block">
                  {recipe.totalTimeMinutes} <span className="text-xs font-normal">phút</span>
                </span>
              </div>
              <div className="bg-white/10 backdrop-blur-xs rounded-xl p-3">
                <span className="text-xs text-brand-100 font-medium block">Khẩu phần</span>
                <span className="text-lg sm:text-xl font-extrabold mt-0.5 block">
                  {recipe.servings} <span className="text-xs font-normal">phần</span>
                </span>
              </div>
            </div>
          </div>

          <div className="p-6 sm:p-8 space-y-10">
            {/* 1. NGUYÊN LIỆU (INGREDIENTS) VỚI TÍNH NĂNG CHECKLIST */}
            <div>
              <div className="flex items-center justify-between pb-3 border-b border-stone-200 dark:border-stone-800">
                <h3 className="text-xl font-bold text-stone-900 dark:text-stone-100 flex items-center gap-2">
                  <Utensils className="h-5 w-5 text-brand-600 dark:text-brand-400" />
                  Nguyên liệu cần chuẩn bị
                </h3>
                <span className="text-xs text-stone-500 dark:text-stone-400 font-medium">
                  (Bấm để đánh dấu đã chuẩn bị)
                </span>
              </div>

              {recipe.ingredients && recipe.ingredients.length > 0 ? (
                <ul className="mt-4 divide-y divide-stone-100 dark:divide-stone-800/80">
                  {recipe.ingredients
                    .slice()
                    .sort((a, b) => a.orderIndex - b.orderIndex)
                    .map((ing, idx) => {
                      const isChecked = !!checkedIngredients[idx];
                      return (
                        <li
                          key={idx}
                          onClick={() => toggleIngredient(idx)}
                          className={cn(
                            "py-3 flex items-start gap-3.5 cursor-pointer select-none transition-colors rounded-lg px-2 hover:bg-stone-50 dark:hover:bg-stone-800/50",
                            isChecked && "opacity-50"
                          )}
                        >
                          <div
                            className={cn(
                              "mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-md border transition-all",
                              isChecked
                                ? "bg-brand-600 border-brand-600 text-white"
                                : "border-stone-300 dark:border-stone-600 bg-white dark:bg-stone-800"
                            )}
                          >
                            {isChecked && <Check className="h-3.5 w-3.5 stroke-[3]" />}
                          </div>
                          <div className="flex-1 text-sm sm:text-base">
                            <span
                              className={cn(
                                "font-medium text-stone-900 dark:text-stone-100",
                                isChecked && "line-through text-stone-400 dark:text-stone-500"
                              )}
                            >
                              {ing.quantity !== null && ing.quantity !== undefined && (
                                <strong className="font-semibold text-brand-700 dark:text-brand-300 mr-1.5">
                                  {ing.quantity}
                                </strong>
                              )}
                              {ing.unit && (
                                <span className="font-medium text-stone-600 dark:text-stone-300 mr-1.5">
                                  {ing.unit}
                                </span>
                              )}
                              {ing.name}
                            </span>
                            {ing.notes && (
                              <span className="block text-xs text-stone-500 dark:text-stone-400 mt-0.5 italic">
                                ({ing.notes})
                              </span>
                            )}
                          </div>
                        </li>
                      );
                    })}
                </ul>
              ) : (
                <p className="mt-4 text-sm text-stone-500 dark:text-stone-400 italic">
                  Chưa có danh sách nguyên liệu cụ thể cho món ăn này.
                </p>
              )}
            </div>

            {/* 2. CÁC BƯỚC THỰC HIỆN (STEPS) VỚI ĐỒNG HỒ HẸN GIỜ & ẢNH */}
            <div>
              <div className="pb-3 border-b border-stone-200 dark:border-stone-800">
                <h3 className="text-xl font-bold text-stone-900 dark:text-stone-100 flex items-center gap-2">
                  <ChefHat className="h-5 w-5 text-brand-600 dark:text-brand-400" />
                  Hướng dẫn các bước nấu
                </h3>
              </div>

              {recipe.steps && recipe.steps.length > 0 ? (
                <div className="mt-6 space-y-8">
                  {recipe.steps
                    .slice()
                    .sort((a, b) => a.stepNumber - b.stepNumber)
                    .map((step) => (
                      <div
                        key={step.stepNumber}
                        className="relative pl-6 sm:pl-8 border-l-2 border-brand-200 dark:border-brand-900/60 pb-2 space-y-3"
                      >
                        {/* Huy hiệu số bước */}
                        <div className="absolute -left-4 top-0 flex h-8 w-8 items-center justify-center rounded-full bg-brand-600 text-white font-extrabold text-sm shadow-md">
                          {step.stepNumber}
                        </div>

                        {/* Tiêu đề & Nội dung bước */}
                        <div>
                          <h4 className="text-base sm:text-lg font-bold text-stone-900 dark:text-stone-100">
                            {step.title}
                          </h4>
                          <p className="mt-2 text-sm sm:text-base text-stone-700 dark:text-stone-300 leading-relaxed whitespace-pre-line">
                            {step.description}
                          </p>
                        </div>

                        {/* Đồng hồ hẹn giờ đếm ngược cho bước nếu có TimerMinutes */}
                        {step.timerMinutes && step.timerMinutes > 0 ? (
                          <StepTimer timerMinutes={step.timerMinutes} stepNumber={step.stepNumber} />
                        ) : null}

                        {/* Ảnh minh họa của bước nếu có */}
                        {step.imageUrl && (
                          <div className="mt-3 overflow-hidden rounded-xl border border-stone-200 dark:border-stone-800 max-w-lg">
                            {/* eslint-disable-next-line @next/next/no-img-element */}
                            <img
                              src={step.imageUrl}
                              alt={`Minh họa bước ${step.stepNumber}: ${step.title}`}
                              className="w-full h-auto object-cover max-h-72"
                              loading="lazy"
                            />
                          </div>
                        )}
                      </div>
                    ))}
                </div>
              ) : (
                <p className="mt-4 text-sm text-stone-500 dark:text-stone-400 italic">
                  Chưa có các bước hướng dẫn chi tiết.
                </p>
              )}
            </div>

            {/* 3. THÔNG TIN DINH DƯỠNG (NUTRITION) NẾU CÓ DỮ LIỆU */}
            {recipe.nutrition &&
              (recipe.nutrition.calories ||
                recipe.nutrition.protein ||
                recipe.nutrition.carbohydrates ||
                recipe.nutrition.fat ||
                recipe.nutrition.fiber ||
                recipe.nutrition.sodium) && (
                <div className="pt-6 border-t border-stone-200 dark:border-stone-800">
                  <h3 className="text-lg font-bold text-stone-900 dark:text-stone-100 flex items-center gap-2 mb-4">
                    <Activity className="h-5 w-5 text-emerald-600 dark:text-emerald-400" />
                    Thông tin dinh dưỡng (Ước tính mỗi khẩu phần)
                  </h3>

                  <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-6 gap-3 text-center">
                    {recipe.nutrition.calories !== null && recipe.nutrition.calories !== undefined && (
                      <div className="p-3 rounded-xl bg-stone-50 dark:bg-stone-800 border border-stone-200/80 dark:border-stone-700/80">
                        <span className="text-xs text-stone-500 dark:text-stone-400 font-medium block">Calories</span>
                        <span className="text-base font-bold text-stone-900 dark:text-stone-100 mt-0.5 block">
                          {recipe.nutrition.calories} <span className="text-xs font-normal">kcal</span>
                        </span>
                      </div>
                    )}
                    {recipe.nutrition.protein !== null && recipe.nutrition.protein !== undefined && (
                      <div className="p-3 rounded-xl bg-stone-50 dark:bg-stone-800 border border-stone-200/80 dark:border-stone-700/80">
                        <span className="text-xs text-stone-500 dark:text-stone-400 font-medium block">Chất đạm</span>
                        <span className="text-base font-bold text-stone-900 dark:text-stone-100 mt-0.5 block">
                          {recipe.nutrition.protein} <span className="text-xs font-normal">g</span>
                        </span>
                      </div>
                    )}
                    {recipe.nutrition.carbohydrates !== null && recipe.nutrition.carbohydrates !== undefined && (
                      <div className="p-3 rounded-xl bg-stone-50 dark:bg-stone-800 border border-stone-200/80 dark:border-stone-700/80">
                        <span className="text-xs text-stone-500 dark:text-stone-400 font-medium block">Carb</span>
                        <span className="text-base font-bold text-stone-900 dark:text-stone-100 mt-0.5 block">
                          {recipe.nutrition.carbohydrates} <span className="text-xs font-normal">g</span>
                        </span>
                      </div>
                    )}
                    {recipe.nutrition.fat !== null && recipe.nutrition.fat !== undefined && (
                      <div className="p-3 rounded-xl bg-stone-50 dark:bg-stone-800 border border-stone-200/80 dark:border-stone-700/80">
                        <span className="text-xs text-stone-500 dark:text-stone-400 font-medium block">Chất béo</span>
                        <span className="text-base font-bold text-stone-900 dark:text-stone-100 mt-0.5 block">
                          {recipe.nutrition.fat} <span className="text-xs font-normal">g</span>
                        </span>
                      </div>
                    )}
                    {recipe.nutrition.fiber !== null && recipe.nutrition.fiber !== undefined && (
                      <div className="p-3 rounded-xl bg-stone-50 dark:bg-stone-800 border border-stone-200/80 dark:border-stone-700/80">
                        <span className="text-xs text-stone-500 dark:text-stone-400 font-medium block">Chất xơ</span>
                        <span className="text-base font-bold text-stone-900 dark:text-stone-100 mt-0.5 block">
                          {recipe.nutrition.fiber} <span className="text-xs font-normal">g</span>
                        </span>
                      </div>
                    )}
                    {recipe.nutrition.sodium !== null && recipe.nutrition.sodium !== undefined && (
                      <div className="p-3 rounded-xl bg-stone-50 dark:bg-stone-800 border border-stone-200/80 dark:border-stone-700/80">
                        <span className="text-xs text-stone-500 dark:text-stone-400 font-medium block">Natri</span>
                        <span className="text-base font-bold text-stone-900 dark:text-stone-100 mt-0.5 block">
                          {recipe.nutrition.sodium} <span className="text-xs font-normal">mg</span>
                        </span>
                      </div>
                    )}
                  </div>
                </div>
              )}
          </div>
        </section>

        {/* Nút quay lại danh sách công thức */}
        <div className="mt-12 text-center">
          <Button
            variant="outline"
            href="/recipes"
            leftIcon={<ArrowLeft className="h-4 w-4" />}
            size="lg"
          >
            Khám phá thêm các công thức khác
          </Button>
        </div>
      </div>
    </div>
  );
}
