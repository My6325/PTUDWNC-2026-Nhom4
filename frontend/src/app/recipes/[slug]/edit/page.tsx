"use client";

import React, { useState, useEffect, useRef, useCallback } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import {
  ChefHat,
  Sparkles,
  Clock,
  Users,
  Plus,
  Trash2,
  UploadCloud,
  ArrowLeft,
  Save,
  Eye,
  AlertCircle,
  Flame,
  Image as ImageIcon,
  ShieldAlert,
  RotateCcw,
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";
import { Card, CardHeader, CardTitle, CardContent, CardFooter } from "@/components/ui/Card";
import { Alert } from "@/components/ui/Alert";
import { Skeleton } from "@/components/ui/Skeleton";
import { categoryService } from "@/services/categoryService";
import { recipeService } from "@/services/recipeService";
import { CategoryDto } from "@/types/category";
import { RecipeDetailDto, RecipeDifficulty, UpdateRecipeRequest } from "@/types/recipe";
import { useAuthStore } from "@/stores/auth-store";
import { extractApiErrorMessage } from "@/utils/apiError";
import { cn } from "@/lib/utils";

interface IngredientFormItem {
  id: string;
  name: string;
  quantity: string;
  unit: string;
  notes: string;
}

interface StepFormItem {
  id: string;
  title: string;
  description: string;
  timerMinutes: string;
}

const difficultyMap: Record<string, RecipeDifficulty> = {
  Easy: RecipeDifficulty.Easy,
  Medium: RecipeDifficulty.Medium,
  Hard: RecipeDifficulty.Hard,
};

function normalizeDifficulty(diff: unknown): "Easy" | "Medium" | "Hard" {
  if (diff === 1 || diff === "Easy" || diff === "easy") return "Easy";
  if (diff === 2 || diff === "Medium" || diff === "medium") return "Medium";
  if (diff === 3 || diff === "Hard" || diff === "hard") return "Hard";
  return "Easy";
}

/**
 * Trang Chỉnh sửa Công thức Nấu ăn (/recipes/[slug]/edit)
 * Hỗ trợ Khóa lạc quan (OCC) thông qua RowVersion Token (FR-RCP-004),
 * Tải ảnh trực tiếp lên Supabase Storage (FR-FILE-001),
 * Cập nhật động danh sách nguyên liệu và các bước nấu chuẩn Budget Bytes.
 */
export default function EditRecipePage() {
  const params = useParams();
  const router = useRouter();
  const slug = typeof params?.slug === "string" ? params.slug : "";
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { isAuthenticated, user, isLoading: isAuthLoading } = useAuthStore();

  // Dữ liệu ban đầu
  const [recipe, setRecipe] = useState<RecipeDetailDto | null>(null);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  // Form State
  const [title, setTitle] = useState<string>("");
  const [description, setDescription] = useState<string>("");
  const [categoryId, setCategoryId] = useState<string>("");
  const [difficulty, setDifficulty] = useState<"Easy" | "Medium" | "Hard">("Easy");
  const [prepTimeMinutes, setPrepTimeMinutes] = useState<string>("15");
  const [cookTimeMinutes, setCookTimeMinutes] = useState<string>("30");
  const [servings, setServings] = useState<string>("4");
  const [imageUrl, setImageUrl] = useState<string>("");
  const [rowVersion, setRowVersion] = useState<string>("");

  // Tệp ảnh mới chọn
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [filePreviewUrl, setFilePreviewUrl] = useState<string | null>(null);

  // Nguyên liệu & Bước nấu
  const [ingredients, setIngredients] = useState<IngredientFormItem[]>([]);
  const [steps, setSteps] = useState<StepFormItem[]>([]);

  // Trạng thái thao tác
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [isConflictError, setIsConflictError] = useState<boolean>(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Tải dữ liệu công thức & danh mục
  const loadData = useCallback(async () => {
    if (!slug) return;
    setIsLoading(true);
    setLoadError(null);
    setIsConflictError(false);

    try {
      const [recipeData, categoriesData] = await Promise.all([
        recipeService.getRecipeBySlug(slug),
        categoryService.getCategories(),
      ]);

      setRecipe(recipeData);
      setCategories(categoriesData);

      // Điền dữ liệu vào form
      setTitle(recipeData.title);
      setDescription(recipeData.description || "");
      setCategoryId(recipeData.category?.id || (categoriesData[0]?.id ?? ""));
      setDifficulty(normalizeDifficulty(recipeData.difficulty));
      setPrepTimeMinutes(String(recipeData.prepTimeMinutes || 15));
      setCookTimeMinutes(String(recipeData.cookTimeMinutes || 30));
      setServings(String(recipeData.servings || 4));
      setRowVersion(recipeData.rowVersion || "");

      const primaryImg =
        recipeData.images?.find((img) => img.isPrimary)?.originalUrl ||
        recipeData.images?.[0]?.originalUrl ||
        "";
      setImageUrl(primaryImg);

      if (recipeData.ingredients && recipeData.ingredients.length > 0) {
        setIngredients(
          recipeData.ingredients.map((ing, idx) => ({
            id: ing.id || `ing-${idx}`,
            name: ing.name,
            quantity: ing.quantity ? String(ing.quantity) : "",
            unit: ing.unit || "",
            notes: ing.notes || "",
          }))
        );
      } else {
        setIngredients([{ id: "ing-1", name: "", quantity: "", unit: "gam", notes: "" }]);
      }

      if (recipeData.steps && recipeData.steps.length > 0) {
        setSteps(
          recipeData.steps.map((st, idx) => ({
            id: st.id || `step-${idx}`,
            title: st.title || `Bước ${st.stepNumber || idx + 1}`,
            description: st.description,
            timerMinutes: st.timerMinutes ? String(st.timerMinutes) : "",
          }))
        );
      } else {
        setSteps([{ id: "step-1", title: "Bước 1", description: "", timerMinutes: "" }]);
      }
    } catch (err: unknown) {
      setLoadError(extractApiErrorMessage(err, "Không thể tải thông tin công thức để chỉnh sửa."));
    } finally {
      setIsLoading(false);
    }
  }, [slug]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Kiểm tra quyền sở hữu hoặc Quản trị viên
  const isOwnerOrAdmin = Boolean(
    isAuthenticated &&
      user &&
      recipe &&
      (user.id === recipe.authorId ||
        user.roles?.includes("Admin") ||
        user.roles?.includes("ADMIN") ||
        user.roles?.includes("admin"))
  );

  // Xử lý tệp hình ảnh
  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      if (file.size > 5 * 1024 * 1024) {
        setErrorMessage("Kích thước tệp không được vượt quá 5MB.");
        return;
      }
      setSelectedFile(file);
      setFilePreviewUrl(URL.createObjectURL(file));
    }
  };

  // Nguyên liệu
  const handleAddIngredient = () => {
    setIngredients([
      ...ingredients,
      { id: `ing-${Date.now()}`, name: "", quantity: "", unit: "gam", notes: "" },
    ]);
  };

  const handleRemoveIngredient = (id: string) => {
    if (ingredients.length <= 1) return;
    setIngredients(ingredients.filter((item) => item.id !== id));
  };

  const handleIngredientChange = (id: string, field: keyof IngredientFormItem, value: string) => {
    setIngredients(
      ingredients.map((item) => (item.id === id ? { ...item, [field]: value } : item))
    );
  };

  // Các bước
  const handleAddStep = () => {
    setSteps([
      ...steps,
      { id: `step-${Date.now()}`, title: `Bước ${steps.length + 1}`, description: "", timerMinutes: "" },
    ]);
  };

  const handleRemoveStep = (id: string) => {
    if (steps.length <= 1) return;
    setSteps(steps.filter((item) => item.id !== id));
  };

  const handleStepChange = (id: string, field: keyof StepFormItem, value: string) => {
    setSteps(
      steps.map((item) => (item.id === id ? { ...item, [field]: value } : item))
    );
  };

  // Submit cập nhật công thức kèm RowVersion OCC Token
  const handleUpdateRecipe = async (e: React.FormEvent, shouldPublish: boolean = false) => {
    e.preventDefault();
    if (!recipe) return;

    setErrorMessage(null);
    setSuccessMessage(null);
    setIsConflictError(false);

    if (!title.trim() || title.trim().length < 5) {
      setErrorMessage("Tiêu đề công thức phải có từ 5 đến 200 ký tự.");
      return;
    }

    const prep = parseInt(prepTimeMinutes, 10);
    const cook = parseInt(cookTimeMinutes, 10);
    const serv = parseInt(servings, 10);

    if (isNaN(prep) || prep <= 0) {
      setErrorMessage("Thời gian chuẩn bị phải lớn hơn 0 phút.");
      return;
    }
    if (isNaN(cook) || cook <= 0) {
      setErrorMessage("Thời gian nấu nướng phải lớn hơn 0 phút.");
      return;
    }
    if (isNaN(serv) || serv <= 0) {
      setErrorMessage("Khẩu phần ăn phải lớn hơn 0 người.");
      return;
    }

    const requestPayload: UpdateRecipeRequest = {
      title: title.trim(),
      description: description.trim() || undefined,
      categoryId: categoryId || undefined,
      prepTimeMinutes: prep,
      cookTimeMinutes: cook,
      servings: serv,
      difficulty: difficultyMap[difficulty] ?? RecipeDifficulty.Easy,
      rowVersion: rowVersion, // Token Khóa Lạc Quan OCC (FR-RCP-004)
    };

    setIsSubmitting(true);

    try {
      // 1. Gửi lệnh cập nhật công thức (FR-RCP-004)
      const updated = await recipeService.update(recipe.id, requestPayload);

      // 2. Nếu có chọn tệp hình ảnh mới hoặc dán URL mới, tải lên Supabase Storage
      let fileToUpload = selectedFile;
      const initialPrimaryImg =
        recipe.images?.find((img) => img.isPrimary)?.originalUrl ||
        recipe.images?.[0]?.originalUrl ||
        "";
      if (!fileToUpload && imageUrl.trim() && imageUrl.trim() !== initialPrimaryImg) {
        try {
          const res = await fetch(imageUrl.trim());
          if (res.ok) {
            const blob = await res.blob();
            const mimeType = blob.type || "image/jpeg";
            const ext = mimeType.includes("png") ? ".png" : mimeType.includes("webp") ? ".webp" : ".jpg";
            fileToUpload = new File([blob], `cover${ext}`, { type: mimeType });
          }
        } catch (fetchErr) {
          console.warn("Không thể tải ảnh từ URL:", fetchErr);
        }
      }

      let imageWarning = "";
      if (fileToUpload) {
        try {
          await recipeService.uploadRecipeImage(recipe.id, fileToUpload);
        } catch (uploadErr) {
          console.warn("Không thể tải ảnh đại diện mới:", uploadErr);
          imageWarning = ` (Lưu ý: Không thể tải ảnh lên Supabase - ${extractApiErrorMessage(uploadErr, "Lỗi kết nối Storage")})`;
        }
      }

      let publishWarning = "";
      // 3. Nếu người dùng chọn Xuất bản ngay
      if (shouldPublish) {
        try {
          await recipeService.publishRecipe(recipe.id);
        } catch (pubErr) {
          console.warn("Lỗi khi xuất bản công thức:", pubErr);
          publishWarning = ` (Lưu ý: Chưa thể xuất bản: ${extractApiErrorMessage(pubErr, "Cần có ảnh đại diện chính, nguyên liệu và các bước")})`;
        }
      }

      setSuccessMessage(
        (shouldPublish && !publishWarning
          ? "Đã cập nhật và xuất bản công thức thành công!"
          : "Đã cập nhật công thức thành công!") +
        (imageWarning ? imageWarning : "") +
        (publishWarning ? publishWarning : " Đang chuyển hướng...")
      );
      setRowVersion(updated.rowVersion);

      setTimeout(() => {
        router.push(updated.slug ? `/recipes/${updated.slug}` : `/recipes`);
      }, 1500);
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 409) {
        setIsConflictError(true);
        setErrorMessage(
          "Xung đột chỉnh sửa đồng thời (409 Conflict): Công thức này đã được chỉnh sửa trong một phiên khác. Vui lòng tải lại dữ liệu mới nhất trước khi lưu."
        );
      } else {
        setErrorMessage(
          extractApiErrorMessage(
            err,
            "Không thể cập nhật công thức món ăn. Vui lòng kiểm tra lại thông tin."
          )
        );
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  // Trạng thái Loading Skeleton
  if (isLoading) {
    return (
      <div className="min-h-screen bg-surface-warm py-12">
        <div className="max-w-7xl mx-auto px-4 space-y-6">
          <Skeleton className="h-6 w-48 rounded-md" />
          <Skeleton className="h-10 w-96 rounded-lg" />
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-8">
            <div className="lg:col-span-8 space-y-6">
              <Skeleton className="h-64 w-full rounded-2xl" />
              <Skeleton className="h-48 w-full rounded-2xl" />
            </div>
            <div className="lg:col-span-4">
              <Skeleton className="h-96 w-full rounded-3xl" />
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Báo lỗi không tìm thấy hoặc lỗi tải dữ liệu
  if (loadError || !recipe) {
    return (
      <div className="min-h-[70vh] flex items-center justify-center py-16 px-4">
        <div className="max-w-md w-full text-center space-y-5">
          <div className="w-16 h-16 mx-auto rounded-full bg-red-100 flex items-center justify-center text-red-600">
            <AlertCircle className="w-8 h-8" />
          </div>
          <h2 className="text-2xl font-bold text-stone-900">Không thể tải công thức</h2>
          <p className="text-sm text-stone-600">{loadError || "Công thức không tồn tại hoặc đã bị xóa."}</p>
          <Button href="/recipes" variant="outline">
            Quay lại danh sách công thức
          </Button>
        </div>
      </div>
    );
  }

  // Kiểm tra quyền chỉnh sửa
  if (!isAuthLoading && !isOwnerOrAdmin) {
    return (
      <div className="min-h-[70vh] flex items-center justify-center py-16 px-4">
        <div className="max-w-md w-full text-center space-y-5 bg-white p-8 rounded-3xl border border-surface-border shadow-sm">
          <div className="w-16 h-16 mx-auto rounded-full bg-amber-100 flex items-center justify-center text-amber-600">
            <ShieldAlert className="w-8 h-8" />
          </div>
          <h2 className="text-2xl font-bold text-stone-900">Quyền truy cập bị từ chối</h2>
          <p className="text-sm text-stone-600">
            Bạn không phải là tác giả của công thức này hoặc không có quyền Quản trị viên để chỉnh sửa.
          </p>
          <Button href={`/recipes/${recipe.slug}`} variant="default">
            Xem công thức
          </Button>
        </div>
      </div>
    );
  }

  const selectedCategory = categories.find((c) => c.id === categoryId);
  const totalCookingMinutes = (parseInt(prepTimeMinutes, 10) || 0) + (parseInt(cookTimeMinutes, 10) || 0);
  const displayPreviewImage = filePreviewUrl || imageUrl;

  return (
    <div className="min-h-screen bg-surface-warm py-8 sm:py-12">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Navigation Breadcrumb */}
        <div className="flex items-center justify-between mb-8">
          <Link
            href={`/recipes/${recipe.slug}`}
            className="inline-flex items-center gap-2 text-sm font-semibold text-stone-600 hover:text-brand-600 transition-colors"
          >
            <ArrowLeft className="w-4 h-4" /> Quay lại xem chi tiết món ăn
          </Link>
          <div className="flex items-center gap-2 text-xs font-medium text-stone-500 bg-white px-3 py-1.5 rounded-full border border-surface-border">
            <ChefHat className="w-4 h-4 text-brand-600" />
            <span>Chỉnh sửa công thức (OCC: {rowVersion ? rowVersion.slice(0, 8) : "N/A"})</span>
          </div>
        </div>

        {/* Tiêu đề trang */}
        <div className="mb-8">
          <h1 className="text-3xl sm:text-4xl font-extrabold text-stone-900 tracking-tight">
            Chỉnh Sửa Công Thức: <span className="text-brand-600">{recipe.title}</span>
          </h1>
          <p className="text-stone-600 text-sm sm:text-base mt-2 max-w-2xl">
            Cập nhật nguyên liệu, điều chỉnh thời gian nấu hoặc thay mới hình ảnh món ăn để công thức luôn hoàn hảo nhất.
          </p>
        </div>

        {/* Cảnh báo Xung đột đồng thời (OCC 409 Conflict) */}
        {isConflictError && (
          <div className="mb-6 p-4 rounded-2xl bg-amber-50 border border-amber-300 text-amber-900 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <RotateCcw className="w-5 h-5 text-amber-700 shrink-0" />
              <div>
                <p className="text-sm font-bold">Xung đột dữ liệu đồng thời (409 Conflict)</p>
                <p className="text-xs text-amber-700 mt-0.5">
                  Công thức này đã được cập nhật bởi một người dùng khác. Hãy tải lại trang để nạp phiên bản mới nhất.
                </p>
              </div>
            </div>
            <Button
              type="button"
              variant="default"
              size="sm"
              onClick={loadData}
              leftIcon={<RotateCcw className="w-4 h-4" />}
            >
              Tải lại công thức
            </Button>
          </div>
        )}

        {/* Báo lỗi / Thành công chung */}
        {errorMessage && !isConflictError && (
          <Alert variant="error" title="Không thể lưu thay đổi" className="mb-6" dismissible onDismiss={() => setErrorMessage(null)}>
            {errorMessage}
          </Alert>
        )}
        {successMessage && (
          <Alert variant="success" title="Cập nhật thành công" className="mb-6">
            {successMessage}
          </Alert>
        )}

        {/* Form chỉnh sửa */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
          {/* CỘT TRÁI: FORM CHỈNH SỬA CHI TIẾT (8 Cột) */}
          <form onSubmit={handleUpdateRecipe} className="lg:col-span-7 xl:col-span-8 space-y-8">
            {/* Khối 1: Thông tin cơ bản */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Sparkles className="w-5 h-5 text-brand-600" />
                  <span>1. Thông tin chung của món ăn</span>
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-5">
                <div>
                  <label className="block text-sm font-semibold text-stone-800 mb-1.5">
                    Tiêu đề công thức <span className="text-red-500">*</span>
                  </label>
                  <Input
                    placeholder="Tiêu đề món ăn..."
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    required
                  />
                </div>

                <div>
                  <label className="block text-sm font-semibold text-stone-800 mb-1.5">
                    Mô tả món ăn
                  </label>
                  <textarea
                    rows={3}
                    placeholder="Mô tả tóm tắt về hương vị..."
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    className="block w-full rounded-xl border border-surface-border bg-white text-stone-900 text-sm p-4 focus:border-brand-500 focus:outline-none"
                  />
                </div>

                {/* Danh mục & Độ khó */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-semibold text-stone-800 mb-1.5">
                      Danh mục ẩm thực
                    </label>
                    <select
                      value={categoryId}
                      onChange={(e) => setCategoryId(e.target.value)}
                      className="block w-full rounded-xl border border-surface-border bg-white text-stone-900 text-sm h-11 px-4 focus:border-brand-500 focus:outline-none"
                    >
                      {categories.map((cat) => (
                        <option key={cat.id} value={cat.id}>
                          {cat.name}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-semibold text-stone-800 mb-1.5">
                      Độ khó chế biến
                    </label>
                    <div className="grid grid-cols-3 gap-2">
                      {(["Easy", "Medium", "Hard"] as const).map((lvl) => (
                        <button
                          key={lvl}
                          type="button"
                          onClick={() => setDifficulty(lvl)}
                          className={cn(
                            "h-11 rounded-xl text-xs font-semibold border transition-all",
                            difficulty === lvl
                              ? "bg-brand-600 text-white border-brand-600 font-bold"
                              : "bg-white text-stone-700 border-surface-border hover:bg-stone-50"
                          )}
                        >
                          {lvl === "Easy" ? "Dễ" : lvl === "Medium" ? "Trung bình" : "Khó"}
                        </button>
                      ))}
                    </div>
                  </div>
                </div>

                {/* Thời gian & Khẩu phần */}
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-2">
                  <div>
                    <label className="block text-xs font-semibold text-stone-600 mb-1">
                      Chuẩn bị (phút)
                    </label>
                    <Input
                      type="number"
                      min="1"
                      value={prepTimeMinutes}
                      onChange={(e) => setPrepTimeMinutes(e.target.value)}
                      leftIcon={<Clock className="w-4 h-4 text-stone-400" />}
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-semibold text-stone-600 mb-1">
                      Nấu nướng (phút)
                    </label>
                    <Input
                      type="number"
                      min="1"
                      value={cookTimeMinutes}
                      onChange={(e) => setCookTimeMinutes(e.target.value)}
                      leftIcon={<Flame className="w-4 h-4 text-stone-400" />}
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-semibold text-stone-600 mb-1">
                      Khẩu phần (người ăn)
                    </label>
                    <Input
                      type="number"
                      min="1"
                      value={servings}
                      onChange={(e) => setServings(e.target.value)}
                      leftIcon={<Users className="w-4 h-4 text-stone-400" />}
                    />
                  </div>
                </div>

                {/* Cập nhật ảnh đại diện */}
                <div className="pt-2 space-y-3">
                  <label className="block text-sm font-semibold text-stone-800">
                    Ảnh đại diện món ăn
                  </label>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <div
                      onClick={() => fileInputRef.current?.click()}
                      className="border-2 border-dashed border-stone-300 hover:border-brand-500 rounded-2xl p-4 flex flex-col items-center justify-center text-center cursor-pointer transition-colors bg-stone-50/50 hover:bg-brand-50/20"
                    >
                      <input
                        ref={fileInputRef}
                        type="file"
                        accept="image/jpeg,image/png,image/webp"
                        className="hidden"
                        onChange={handleFileChange}
                      />
                      <ImageIcon className="w-8 h-8 text-stone-400 mb-2" />
                      <p className="text-xs font-bold text-stone-700">
                        {selectedFile ? selectedFile.name : "Tải ảnh mới từ máy tính"}
                      </p>
                      <p className="text-[11px] text-stone-500 mt-1">
                        Hỗ trợ JPG, PNG, WebP (Tối đa 5MB)
                      </p>
                    </div>

                    <div className="flex flex-col justify-center">
                      <Input
                        placeholder="Hoặc URL ảnh: https://..."
                        value={imageUrl}
                        onChange={(e) => {
                          setImageUrl(e.target.value);
                          if (filePreviewUrl) setFilePreviewUrl(null);
                        }}
                        leftIcon={<UploadCloud className="w-4 h-4 text-stone-400" />}
                        helperText="Liên kết ảnh trực tiếp hoặc ảnh đã tải lên trước đó."
                      />
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Khối 2: Danh sách nguyên liệu */}
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <div>
                  <CardTitle className="flex items-center gap-2">
                    <Users className="w-5 h-5 text-brand-600" />
                    <span>2. Danh sách nguyên liệu</span>
                  </CardTitle>
                </div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={handleAddIngredient}
                  leftIcon={<Plus className="w-4 h-4" />}
                >
                  Thêm nguyên liệu
                </Button>
              </CardHeader>
              <CardContent className="space-y-3">
                {ingredients.map((ing, idx) => (
                  <div
                    key={ing.id}
                    className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2 p-3 rounded-xl bg-stone-50 border border-stone-200/60"
                  >
                    <span className="w-6 text-xs font-bold text-stone-400 text-center shrink-0">
                      #{idx + 1}
                    </span>
                    <div className="flex-1">
                      <Input
                        placeholder="Tên nguyên liệu..."
                        value={ing.name}
                        onChange={(e) => handleIngredientChange(ing.id, "name", e.target.value)}
                        required
                      />
                    </div>
                    <div className="w-full sm:w-24">
                      <Input
                        placeholder="Số lượng"
                        type="number"
                        value={ing.quantity}
                        onChange={(e) => handleIngredientChange(ing.id, "quantity", e.target.value)}
                      />
                    </div>
                    <div className="w-full sm:w-28">
                      <Input
                        placeholder="Đơn vị"
                        value={ing.unit}
                        onChange={(e) => handleIngredientChange(ing.id, "unit", e.target.value)}
                      />
                    </div>
                    <div className="w-full sm:w-44">
                      <Input
                        placeholder="Ghi chú"
                        value={ing.notes}
                        onChange={(e) => handleIngredientChange(ing.id, "notes", e.target.value)}
                      />
                    </div>
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() => handleRemoveIngredient(ing.id)}
                      disabled={ingredients.length <= 1}
                      className="text-stone-400 hover:text-red-500 p-2"
                    >
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  </div>
                ))}
              </CardContent>
            </Card>

            {/* Khối 3: Các bước thực hiện */}
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <div>
                  <CardTitle className="flex items-center gap-2">
                    <Clock className="w-5 h-5 text-brand-600" />
                    <span>3. Quy trình từng bước chế biến</span>
                  </CardTitle>
                </div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={handleAddStep}
                  leftIcon={<Plus className="w-4 h-4" />}
                >
                  Thêm bước nấu
                </Button>
              </CardHeader>
              <CardContent className="space-y-4">
                {steps.map((st, idx) => (
                  <div
                    key={st.id}
                    className="p-4 rounded-xl bg-stone-50 border border-stone-200/60 space-y-3"
                  >
                    <div className="flex items-center justify-between gap-2">
                      <div className="flex items-center gap-2 flex-1">
                        <span className="w-7 h-7 rounded-full bg-brand-600 text-white font-bold text-xs flex items-center justify-center shrink-0">
                          {idx + 1}
                        </span>
                        <Input
                          placeholder="Tiêu đề bước..."
                          value={st.title}
                          onChange={(e) => handleStepChange(st.id, "title", e.target.value)}
                        />
                      </div>
                      <div className="flex items-center gap-2">
                        <div className="w-32">
                          <Input
                            type="number"
                            min="0"
                            placeholder="Phút đếm"
                            value={st.timerMinutes}
                            onChange={(e) => handleStepChange(st.id, "timerMinutes", e.target.value)}
                            leftIcon={<Clock className="w-3.5 h-3.5 text-stone-400" />}
                          />
                        </div>
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => handleRemoveStep(st.id)}
                          disabled={steps.length <= 1}
                          className="text-stone-400 hover:text-red-500 p-2"
                        >
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      </div>
                    </div>
                    <textarea
                      rows={3}
                      placeholder="Mô tả cụ thể cách thao tác..."
                      value={st.description}
                      onChange={(e) => handleStepChange(st.id, "description", e.target.value)}
                      className="block w-full rounded-lg border border-stone-200 bg-white p-3 text-sm text-stone-800 focus:outline-none focus:border-brand-500"
                    />
                  </div>
                ))}
              </CardContent>
              <CardFooter className="flex items-center justify-between pt-4 border-t border-stone-100">
                <Button href={`/recipes/${recipe.slug}`} variant="ghost" size="md">
                  Hủy bỏ
                </Button>
                <div className="flex items-center gap-3">
                  {(recipe.status === 1 || recipe.status === "Draft") && (
                    <Button
                      type="button"
                      variant="outline"
                      size="md"
                      isLoading={isSubmitting}
                      onClick={(e) => handleUpdateRecipe(e, true)}
                      leftIcon={<Sparkles className="w-4 h-4 text-emerald-600" />}
                      className="border-emerald-300 hover:bg-emerald-50 text-emerald-700 font-semibold"
                    >
                      Lưu & Xuất bản ngay
                    </Button>
                  )}
                  <Button
                    type="submit"
                    variant="default"
                    size="md"
                    isLoading={isSubmitting}
                    leftIcon={<Save className="w-4 h-4" />}
                  >
                    Lưu thay đổi công thức
                  </Button>
                </div>
              </CardFooter>
            </Card>
          </form>

          {/* CỘT PHẢI: LIVE PREVIEW THẺ MÓN ĂN (4 Cột) */}
          <div className="lg:col-span-5 xl:col-span-4 sticky top-24 space-y-6">
            <div className="bg-white rounded-3xl border border-surface-border p-6 shadow-sm">
              <div className="flex items-center justify-between mb-4 pb-3 border-b border-stone-100">
                <div className="flex items-center gap-2">
                  <Eye className="w-4 h-4 text-brand-600" />
                  <span className="font-bold text-sm text-stone-800 uppercase tracking-wide">
                    Live Preview Thẻ Món
                  </span>
                </div>
                <span className="text-[11px] font-semibold text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded-full border border-emerald-200">
                  Thời gian thực
                </span>
              </div>

              {/* Recipe Card Preview */}
              <div className="rounded-2xl border border-surface-border overflow-hidden bg-white shadow-md">
                <div className="relative aspect-[4/3] bg-stone-100 flex items-center justify-center overflow-hidden">
                  {displayPreviewImage ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img
                      src={displayPreviewImage}
                      alt={title || "Xem trước công thức"}
                      className="w-full h-full object-cover"
                      onError={(e) => {
                        (e.target as HTMLImageElement).src =
                          "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=600&q=80";
                      }}
                    />
                  ) : (
                    <div className="flex flex-col items-center justify-center text-stone-400 p-4 text-center">
                      <ChefHat className="w-12 h-12 text-stone-300 mb-2" />
                      <span className="text-xs">Chưa có ảnh món ăn</span>
                    </div>
                  )}

                  <div className="absolute top-3 left-3">
                    <Badge variant="brand" size="sm" className="shadow-sm">
                      {selectedCategory?.name || "Danh mục ẩm thực"}
                    </Badge>
                  </div>
                </div>

                <div className="p-5 space-y-3">
                  <h4 className="font-bold text-lg text-stone-900 line-clamp-2 leading-snug">
                    {title || "Tiêu đề công thức"}
                  </h4>

                  <p className="text-xs text-stone-500 line-clamp-2 leading-relaxed">
                    {description || "Mô tả ngắn gọn về món ăn..."}
                  </p>

                  <div className="flex flex-wrap items-center gap-3 pt-3 border-t border-stone-100 text-xs text-stone-600">
                    <span className="flex items-center gap-1">
                      <Clock className="w-3.5 h-3.5 text-brand-600" />
                      <strong className="text-stone-800">{totalCookingMinutes}</strong> phút
                    </span>
                    <span className="flex items-center gap-1">
                      <Users className="w-3.5 h-3.5 text-stone-400" />
                      <strong className="text-stone-800">{servings}</strong> người
                    </span>
                    <Badge difficulty={difficulty} size="sm" />
                  </div>
                </div>
              </div>

              {/* Tóm tắt thông số */}
              <div className="mt-5 pt-4 border-t border-stone-100 grid grid-cols-2 gap-3 text-center">
                <div className="p-3 bg-stone-50 rounded-xl">
                  <span className="block text-xl font-extrabold text-brand-600">
                    {ingredients.filter((i) => i.name.trim()).length}
                  </span>
                  <span className="text-[11px] font-medium text-stone-500">Nguyên liệu</span>
                </div>
                <div className="p-3 bg-stone-50 rounded-xl">
                  <span className="block text-xl font-extrabold text-stone-800">
                    {steps.filter((s) => s.description.trim()).length}
                  </span>
                  <span className="text-[11px] font-medium text-stone-500">Bước nấu</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
