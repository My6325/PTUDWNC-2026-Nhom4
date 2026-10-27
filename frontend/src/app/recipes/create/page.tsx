"use client";

import React, { useState, useEffect, useRef } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
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
  CheckCircle2,
  AlertCircle,
  HelpCircle,
  Flame,
  Image as ImageIcon,
  LogIn,
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";
import { Card, CardHeader, CardTitle, CardContent, CardFooter } from "@/components/ui/Card";
import { Alert } from "@/components/ui/Alert";
import { categoryService } from "@/services/categoryService";
import { recipeService } from "@/services/recipeService";
import { CategoryDto } from "@/types/category";
import { CreateRecipeDraftRequest, RecipeDifficulty } from "@/types/recipe";
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

/**
 * Trang Soạn thảo & Tạo Công thức Món ăn Mới (/recipes/create)
 * Chuẩn Budget Bytes: Bố cục 2 cột chuyên nghiệp, Danh sách Nguyên liệu động,
 * Các bước nấu động kèm Hẹn giờ, Tải ảnh trực tiếp lên Supabase Storage & Live Preview thẻ món ăn.
 */
export default function CreateRecipePage() {
  const router = useRouter();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { isAuthenticated, user, isLoading: isAuthLoading } = useAuthStore();

  // Dữ liệu danh mục
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [isLoadingCategories, setIsLoadingCategories] = useState<boolean>(true);

  // Form State
  const [title, setTitle] = useState<string>("");
  const [description, setDescription] = useState<string>("");
  const [categoryId, setCategoryId] = useState<string>("");
  const [difficulty, setDifficulty] = useState<"Easy" | "Medium" | "Hard">("Easy");
  const [prepTimeMinutes, setPrepTimeMinutes] = useState<string>("15");
  const [cookTimeMinutes, setCookTimeMinutes] = useState<string>("30");
  const [servings, setServings] = useState<string>("4");
  const [imageUrl, setImageUrl] = useState<string>("");

  // Tệp ảnh tải lên từ thiết bị
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [filePreviewUrl, setFilePreviewUrl] = useState<string | null>(null);

  // Nguyên liệu động
  const [ingredients, setIngredients] = useState<IngredientFormItem[]>([
    { id: "ing-1", name: "Thịt bò thăn", quantity: "500", unit: "gam", notes: "Thái lát mỏng vừa ăn" },
    { id: "ing-2", name: "Hành tây", quantity: "1", unit: "củ", notes: "Bổ múi cau" },
    { id: "ing-3", name: "Gừng tươi & Tỏi", quantity: "2", unit: "nhánh", notes: "Băm nhuyễn" },
  ]);

  // Các bước nấu động
  const [steps, setSteps] = useState<StepFormItem[]>([
    {
      id: "step-1",
      title: "Sơ chế nguyên liệu tươi",
      description: "Thịt bò rửa sạch, thấm khô nước rồi ướp cùng 1 thìa dầu hào, tỏi băm và hạt tiêu trong 15 phút.",
      timerMinutes: "15",
    },
    {
      id: "step-2",
      title: "Xào chín tới trên lửa lớn",
      description: "Làm nóng chảo với dầu ăn, phi thơm gừng tỏi rồi trút thịt bò vào đảo nhanh tay trong 3 phút.",
      timerMinutes: "3",
    },
  ]);

  // Trạng thái thao tác
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Nạp danh mục từ API
  useEffect(() => {
    async function loadCategories() {
      try {
        const data = await categoryService.getCategories();
        setCategories(data);
        if (data.length > 0) {
          setCategoryId(data[0].id);
        }
      } catch (err) {
        console.error("Không thể nạp danh mục:", err);
      } finally {
        setIsLoadingCategories(false);
      }
    }
    loadCategories();
  }, []);

  // Xử lý chọn tệp ảnh
  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      if (file.size > 5 * 1024 * 1024) {
        setErrorMessage("Kích thước hình ảnh không được vượt quá 5MB.");
        return;
      }
      setSelectedFile(file);
      const objUrl = URL.createObjectURL(file);
      setFilePreviewUrl(objUrl);
    }
  };

  // Xử lý thêm/xóa nguyên liệu
  const handleAddIngredient = () => {
    const newItem: IngredientFormItem = {
      id: `ing-${Date.now()}`,
      name: "",
      quantity: "",
      unit: "gam",
      notes: "",
    };
    setIngredients([...ingredients, newItem]);
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

  // Xử lý thêm/xóa bước nấu
  const handleAddStep = () => {
    const newStep: StepFormItem = {
      id: `step-${Date.now()}`,
      title: `Bước ${steps.length + 1}`,
      description: "",
      timerMinutes: "5",
    };
    setSteps([...steps, newStep]);
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

  // Submit tạo công thức (bản nháp hoặc xuất bản ngay)
  const handleSubmitRecipe = async (e: React.FormEvent, shouldPublish: boolean = false) => {
    e.preventDefault();
    setErrorMessage(null);
    setSuccessMessage(null);

    if (!isAuthenticated) {
      setErrorMessage("Vui lòng đăng nhập để lưu công thức món ăn.");
      router.push("/login?redirect=/recipes/create");
      return;
    }

    if (!title.trim() || title.trim().length < 5) {
      setErrorMessage("Vui lòng nhập tiêu đề cho món ăn (từ 5 đến 200 ký tự).");
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

    const validSteps = steps
      .filter((s) => s.description.trim())
      .map((s, idx) => ({
        title: s.title.trim() || `Bước ${idx + 1}`,
        description: s.description.trim(),
        timerMinutes: s.timerMinutes && parseInt(s.timerMinutes, 10) > 0 ? parseInt(s.timerMinutes, 10) : undefined,
      }));

    const validIngredients = ingredients
      .filter((i) => i.name.trim())
      .map((i) => ({
        name: i.name.trim(),
        quantity: i.quantity && parseFloat(i.quantity) > 0 ? parseFloat(i.quantity) : undefined,
        unit: i.unit.trim() || undefined,
        notes: i.notes.trim() || undefined,
      }));

    const payload: CreateRecipeDraftRequest = {
      title: title.trim(),
      description: description.trim() || undefined,
      categoryId: categoryId || undefined,
      prepTimeMinutes: prep,
      cookTimeMinutes: cook,
      servings: serv,
      difficulty: difficultyMap[difficulty] ?? RecipeDifficulty.Easy,
      steps: validSteps.length > 0 ? validSteps : undefined,
      ingredients: validIngredients.length > 0 ? validIngredients : undefined,
    };

    setIsSubmitting(true);

    try {
      // 1. Gọi API tạo bản nháp (FR-RCP-003)
      const created = await recipeService.createDraft(payload);

      // 2. Nếu có tệp ảnh tải lên, gửi lên Supabase Storage (FR-FILE-001)
      if (selectedFile) {
        try {
          await recipeService.uploadRecipeImage(created.id, selectedFile);
        } catch (uploadErr) {
          console.warn("Không thể tải ảnh lên Supabase:", uploadErr);
        }
      }

      // 3. Nếu người dùng chọn Xuất bản ngay (FR-RCP-007)
      if (shouldPublish) {
        try {
          await recipeService.publishRecipe(created.id);
        } catch (pubErr) {
          console.warn("Lỗi khi xuất bản công thức:", pubErr);
        }
      }

      setSuccessMessage(
        shouldPublish
          ? "Đã xuất bản công thức món ăn thành công! Đang chuyển hướng..."
          : "Đã lưu bản nháp công thức thành công vào sổ tay ẩm thực của bạn!"
      );

      setTimeout(() => {
        router.push(created.slug ? `/recipes/${created.slug}` : "/recipes");
      }, 1500);
    } catch (err: unknown) {
      setErrorMessage(
        extractApiErrorMessage(
          err,
          "Không thể lưu công thức món ăn. Vui lòng kiểm tra lại thông tin và kết nối mạng."
        )
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const selectedCategory = categories.find((c) => c.id === categoryId);
  const totalCookingMinutes = (parseInt(prepTimeMinutes, 10) || 0) + (parseInt(cookTimeMinutes, 10) || 0);
  const displayPreviewImage = filePreviewUrl || imageUrl;

  return (
    <div className="min-h-screen bg-surface-warm py-8 sm:py-12">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Navigation Breadcrumb */}
        <div className="flex items-center justify-between mb-8">
          <Link
            href="/recipes"
            className="inline-flex items-center gap-2 text-sm font-semibold text-stone-600 hover:text-brand-600 transition-colors"
          >
            <ArrowLeft className="w-4 h-4" /> Quay lại danh sách công thức
          </Link>
          <div className="flex items-center gap-2 text-xs font-medium text-stone-500 bg-white px-3 py-1.5 rounded-full border border-surface-border">
            <ChefHat className="w-4 h-4 text-brand-600" />
            <span>Studio Soạn Thảo Món Ăn</span>
          </div>
        </div>

        {/* Tiêu đề trang */}
        <div className="mb-8">
          <h1 className="text-3xl sm:text-4xl font-extrabold text-stone-900 tracking-tight">
            Tạo Công Thức <span className="text-brand-600">Nấu Ăn Mới</span>
          </h1>
          <p className="text-stone-600 text-sm sm:text-base mt-2 max-w-2xl">
            Chia sẻ bí quyết nấu nướng gia truyền hoặc món ăn thường ngày thơm ngon, chuẩn vị với cộng đồng ẩm thực Bếp Việt.
          </p>
        </div>

        {/* Cảnh báo chưa đăng nhập */}
        {!isAuthLoading && !isAuthenticated && (
          <div className="mb-6 p-4 rounded-2xl bg-amber-50 border border-amber-200 text-amber-900 flex items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <AlertCircle className="w-5 h-5 text-amber-600 shrink-0" />
              <p className="text-sm font-medium">
                Bạn chưa đăng nhập. Hãy đăng nhập tài khoản để có thể lưu công thức vào hồ sơ cá nhân.
              </p>
            </div>
            <Link href="/login?redirect=/recipes/create">
              <Button size="sm" variant="default" leftIcon={<LogIn className="w-4 h-4" />}>
                Đăng nhập ngay
              </Button>
            </Link>
          </div>
        )}

        {/* Báo lỗi / Thành công */}
        {errorMessage && (
          <Alert variant="error" title="Không thể lưu công thức" className="mb-6" dismissible onDismiss={() => setErrorMessage(null)}>
            {errorMessage}
          </Alert>
        )}
        {successMessage && (
          <Alert variant="success" title="Thao tác thành công" className="mb-6">
            {successMessage}
          </Alert>
        )}

        {/* Bố cục 2 cột chuẩn Budget Bytes */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
          {/* CỘT TRÁI: FORM SOẠN THẢO CHI TIẾT (8 Cột) */}
          <form onSubmit={(e) => handleSubmitRecipe(e, false)} className="lg:col-span-7 xl:col-span-8 space-y-8">
            {/* Khối 1: Thông tin cơ bản */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Sparkles className="w-5 h-5 text-brand-600" />
                  <span>1. Thông tin chung của món ăn</span>
                </CardTitle>
                <p className="text-xs text-stone-500">
                  Đặt một tiêu đề gợi cảm giác thèm ăn và tóm tắt hương vị nổi bật.
                </p>
              </CardHeader>
              <CardContent className="space-y-5">
                <div>
                  <label className="block text-sm font-semibold text-stone-800 mb-1.5">
                    Tiêu đề công thức <span className="text-red-500">*</span>
                  </label>
                  <Input
                    placeholder="Ví dụ: Phở bò tái lăn chuẩn vị Hà Thành, súp gà hạt sen..."
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    required
                  />
                </div>

                <div>
                  <label className="block text-sm font-semibold text-stone-800 mb-1.5">
                    Mô tả hương vị & nguồn gốc
                  </label>
                  <textarea
                    rows={3}
                    placeholder="Mô tả ngắn gọn về độ hấp dẫn, vị đậm đà và dịp thích hợp để thưởng thức món ăn này..."
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    className="block w-full rounded-xl border border-surface-border bg-white text-stone-900 placeholder-stone-400 text-sm p-4 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none transition-all"
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
                      className="block w-full rounded-xl border border-surface-border bg-white text-stone-900 text-sm h-11 px-4 focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none"
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
                              ? "bg-brand-600 text-white border-brand-600 shadow-sm font-bold"
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

                {/* Tải ảnh món ăn: Tệp trực tiếp hoặc URL */}
                <div className="pt-2 space-y-3">
                  <label className="block text-sm font-semibold text-stone-800">
                    Ảnh món ăn chính
                  </label>

                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    {/* Cách 1: Tải ảnh từ thiết bị lên Supabase */}
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
                        {selectedFile ? selectedFile.name : "Tải ảnh từ máy tính"}
                      </p>
                      <p className="text-[11px] text-stone-500 mt-1">
                        Hỗ trợ JPG, PNG, WebP (Tối đa 5MB)
                      </p>
                    </div>

                    {/* Cách 2: Dán đường dẫn URL */}
                    <div className="flex flex-col justify-center">
                      <Input
                        placeholder="Hoặc dán URL: https://images.unsplash..."
                        value={imageUrl}
                        onChange={(e) => {
                          setImageUrl(e.target.value);
                          if (filePreviewUrl) setFilePreviewUrl(null);
                        }}
                        leftIcon={<UploadCloud className="w-4 h-4 text-stone-400" />}
                        helperText="Liên kết ảnh trực tiếp từ Unsplash hoặc dịch vụ ảnh công khai."
                      />
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Khối 2: Quản lý Nguyên liệu Động */}
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <div>
                  <CardTitle className="flex items-center gap-2">
                    <Users className="w-5 h-5 text-brand-600" />
                    <span>2. Danh sách nguyên liệu</span>
                  </CardTitle>
                  <p className="text-xs text-stone-500">
                    Ghi rõ định lượng và mẹo chọn thực phẩm tươi ngon.
                  </p>
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
                        placeholder="Tên nguyên liệu (VD: Thịt nạc vai, Nước dừa...)"
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
                        placeholder="Đơn vị (gam, ml)"
                        value={ing.unit}
                        onChange={(e) => handleIngredientChange(ing.id, "unit", e.target.value)}
                      />
                    </div>
                    <div className="w-full sm:w-44">
                      <Input
                        placeholder="Ghi chú (băm nhỏ...)"
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

            {/* Khối 3: Các bước thực hiện & Hẹn giờ */}
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <div>
                  <CardTitle className="flex items-center gap-2">
                    <Clock className="w-5 h-5 text-brand-600" />
                    <span>3. Quy trình từng bước chế biến</span>
                  </CardTitle>
                  <p className="text-xs text-stone-500">
                    Phân tách rõ từng công đoạn giúp người nấu dễ dàng theo dõi theo thời gian thực.
                  </p>
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
                          placeholder={`Tiêu đề bước (VD: Sơ chế, Ninh nước dùng...)`}
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
                      placeholder="Mô tả cụ thể cách thao tác: độ lửa, dấu hiệu món ăn chín, mẹo nhỏ tránh bị khét..."
                      value={st.description}
                      onChange={(e) => handleStepChange(st.id, "description", e.target.value)}
                      className="block w-full rounded-lg border border-stone-200 bg-white p-3 text-sm text-stone-800 focus:outline-none focus:border-brand-500 transition-colors"
                    />
                  </div>
                ))}
              </CardContent>
              <CardFooter className="flex flex-wrap items-center justify-between gap-4 pt-4 border-t border-stone-100">
                <Button href="/recipes" variant="ghost" size="md">
                  Hủy bỏ
                </Button>
                <div className="flex flex-wrap items-center gap-3">
                  <Button
                    type="submit"
                    variant="outline"
                    size="md"
                    isLoading={isSubmitting}
                    leftIcon={<Save className="w-4 h-4" />}
                  >
                    Lưu bản nháp
                  </Button>
                  <Button
                    type="button"
                    variant="default"
                    size="md"
                    isLoading={isSubmitting}
                    onClick={(e) => handleSubmitRecipe(e, true)}
                    leftIcon={<Sparkles className="w-4 h-4" />}
                  >
                    Xuất bản ngay
                  </Button>
                </div>
              </CardFooter>
            </Card>
          </form>

          {/* CỘT PHẢI: LIVE PREVIEW THẺ MÓN ĂN CHUẨN BUDGET BYTES (4 Cột) */}
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

              {/* Mô phỏng Recipe Card */}
              <div className="rounded-2xl border border-surface-border overflow-hidden bg-white shadow-md hover:shadow-lg transition-all">
                {/* Ảnh món ăn preview */}
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

                  {/* Badge danh mục nổi trên ảnh */}
                  <div className="absolute top-3 left-3">
                    <Badge variant="brand" size="sm" className="shadow-sm">
                      {selectedCategory?.name || "Danh mục ẩm thực"}
                    </Badge>
                  </div>
                </div>

                {/* Nội dung thẻ */}
                <div className="p-5 space-y-3">
                  <h4 className="font-bold text-lg text-stone-900 line-clamp-2 leading-snug">
                    {title || "Tiêu đề công thức của bạn sẽ xuất hiện ở đây"}
                  </h4>

                  <p className="text-xs text-stone-500 line-clamp-2 leading-relaxed">
                    {description || "Mô tả ngắn gọn về độ thơm ngon, vị cay nồng hay giòn ngọt của món ăn..."}
                  </p>

                  {/* Meta Chips thời gian & độ khó */}
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

              {/* Tóm tắt số liệu */}
              <div className="mt-5 pt-4 border-t border-stone-100 grid grid-cols-2 gap-3 text-center">
                <div className="p-3 bg-stone-50 rounded-xl">
                  <span className="block text-xl font-extrabold text-brand-600">
                    {ingredients.filter((i) => i.name.trim()).length}
                  </span>
                  <span className="text-[11px] font-medium text-stone-500">Nguyên liệu đã nhập</span>
                </div>
                <div className="p-3 bg-stone-50 rounded-xl">
                  <span className="block text-xl font-extrabold text-stone-800">
                    {steps.filter((s) => s.description.trim()).length}
                  </span>
                  <span className="text-[11px] font-medium text-stone-500">Bước nấu hoàn tất</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
