"use client";

import React, { useState, useEffect } from "react";
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
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";
import { Card, CardHeader, CardTitle, CardContent, CardFooter } from "@/components/ui/Card";
import { Alert } from "@/components/ui/Alert";
import { categoryService } from "@/services/categoryService";
import { CategoryDto } from "@/types/category";
import { useAuthStore } from "@/stores/auth-store";
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

/**
 * Trang Soạn thảo & Tạo Công thức Món ăn Mới (/recipes/create)
 * Chuẩn Budget Bytes: Bố cục 2 cột chuyên nghiệp, Danh sách Nguyên liệu động,
 * Các bước nấu động kèm Hẹn giờ, Khung tải ảnh kéo thả & Live Preview thẻ món ăn.
 */
export default function CreateRecipePage() {
  const router = useRouter();
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

  // Submit tạo bản nháp
  const handleSaveDraft = (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setSuccessMessage(null);

    if (!title.trim()) {
      setErrorMessage("Vui lòng nhập tiêu đề cho món ăn (từ 5 đến 200 ký tự).");
      return;
    }

    setIsSubmitting(true);

    // Giả lập lưu bản nháp theo hợp đồng API FR-RCP-003
    setTimeout(() => {
      setIsSubmitting(false);
      setSuccessMessage("Đã lưu bản nháp công thức thành công vào sổ tay ẩm thực của bạn!");
      setTimeout(() => {
        router.push("/recipes");
      }, 2000);
    }, 1000);
  };

  const selectedCategory = categories.find((c) => c.id === categoryId);
  const totalCookingMinutes = (parseInt(prepTimeMinutes, 10) || 0) + (parseInt(cookTimeMinutes, 10) || 0);

  return (
    <div className="min-h-screen bg-surface-warm py-8 sm:py-12">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Navigation Breadcrumb */}
        <div className="flex items-center justify-between mb-8">
          <Link
            href="/recipes"
            className="inline-flex items-center gap-2 text-sm font-semibold text-stone-600 hover:text-brand-600 transition-colors"
          >
            <ArrowLeft className="w-4 h-4" />
            <span>Quay lại danh sách công thức</span>
          </Link>
          <div className="flex items-center gap-2 text-xs font-semibold text-stone-500 uppercase tracking-wider bg-white px-3 py-1.5 rounded-full border border-surface-border">
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
          <form onSubmit={handleSaveDraft} className="lg:col-span-7 xl:col-span-8 space-y-8">
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
                      min="0"
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
                      min="0"
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

                {/* Đường dẫn ảnh món ăn */}
                <div className="pt-2">
                  <label className="block text-sm font-semibold text-stone-800 mb-1.5">
                    Ảnh đại diện món ăn (URL)
                  </label>
                  <Input
                    placeholder="https://images.unsplash.com/photo-... (hoặc dán liên kết ảnh món ăn)"
                    value={imageUrl}
                    onChange={(e) => setImageUrl(e.target.value)}
                    leftIcon={<UploadCloud className="w-4 h-4 text-stone-400" />}
                    helperText="Mẹo: Nên sử dụng ảnh chụp ngang tỉ lệ 4:3 có ánh sáng tự nhiên ấm áp."
                  />
                </div>
              </CardContent>
            </Card>

            {/* Khối 2: Quản lý Nguyên liệu Động */}
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <div>
                  <CardTitle className="text-base sm:text-lg">
                    2. Danh sách nguyên liệu cần chuẩn bị
                  </CardTitle>
                  <p className="text-xs text-stone-500 mt-0.5">
                    Định lượng chuẩn giúp người nấu nêm nếm vừa miệng nhất.
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
                {ingredients.map((item, index) => (
                  <div
                    key={item.id}
                    className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2 p-3 rounded-xl border border-stone-200/60 bg-stone-50/60"
                  >
                    <span className="text-xs font-bold text-stone-400 w-6 shrink-0 text-center">
                      #{index + 1}
                    </span>
                    <input
                      type="text"
                      placeholder="Tên nguyên liệu (VD: Thịt ba chỉ, Nước mắm...)"
                      value={item.name}
                      onChange={(e) => handleIngredientChange(item.id, "name", e.target.value)}
                      className="flex-1 rounded-lg border border-surface-border bg-white px-3 py-2 text-sm focus:outline-none focus:border-brand-500"
                    />
                    <div className="flex items-center gap-2">
                      <input
                        type="text"
                        placeholder="Số lượng"
                        value={item.quantity}
                        onChange={(e) => handleIngredientChange(item.id, "quantity", e.target.value)}
                        className="w-20 rounded-lg border border-surface-border bg-white px-2 py-2 text-sm text-center focus:outline-none focus:border-brand-500"
                      />
                      <input
                        type="text"
                        placeholder="Đơn vị (gam, thìa...)"
                        value={item.unit}
                        onChange={(e) => handleIngredientChange(item.id, "unit", e.target.value)}
                        className="w-24 rounded-lg border border-surface-border bg-white px-2 py-2 text-sm text-center focus:outline-none focus:border-brand-500"
                      />
                      <button
                        type="button"
                        onClick={() => handleRemoveIngredient(item.id)}
                        disabled={ingredients.length <= 1}
                        className="p-2 text-stone-400 hover:text-red-600 disabled:opacity-30 rounded-lg transition-colors"
                        title="Xóa nguyên liệu này"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                ))}
              </CardContent>
            </Card>

            {/* Khối 3: Quản lý Các Bước Nấu Động */}
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <div>
                  <CardTitle className="text-base sm:text-lg">
                    3. Các bước thực hiện món ăn
                  </CardTitle>
                  <p className="text-xs text-stone-500 mt-0.5">
                    Hướng dẫn rõ ràng từng công đoạn kèm số phút hẹn giờ nếu cần.
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
                    className="p-4 rounded-xl border border-surface-border bg-white space-y-3 relative shadow-sm"
                  >
                    <div className="flex items-center justify-between gap-2 border-b border-stone-100 pb-2">
                      <div className="flex items-center gap-2">
                        <span className="w-7 h-7 rounded-lg bg-brand-50 text-brand-700 font-bold text-xs flex items-center justify-center border border-brand-200">
                          {idx + 1}
                        </span>
                        <input
                          type="text"
                          placeholder={`Tiêu đề bước ${idx + 1}...`}
                          value={st.title}
                          onChange={(e) => handleStepChange(st.id, "title", e.target.value)}
                          className="font-bold text-sm text-stone-800 bg-transparent border-none p-0 focus:outline-none focus:ring-0"
                        />
                      </div>
                      <div className="flex items-center gap-3">
                        <div className="flex items-center gap-1.5 text-xs text-stone-500">
                          <Clock className="w-3.5 h-3.5 text-brand-600" />
                          <span>Hẹn giờ:</span>
                          <input
                            type="number"
                            min="0"
                            value={st.timerMinutes}
                            onChange={(e) => handleStepChange(st.id, "timerMinutes", e.target.value)}
                            className="w-12 text-center rounded border border-surface-border px-1 py-0.5 text-xs"
                          />
                          <span>phút</span>
                        </div>
                        <button
                          type="button"
                          onClick={() => handleRemoveStep(st.id)}
                          disabled={steps.length <= 1}
                          className="text-stone-400 hover:text-red-600 disabled:opacity-30"
                          title="Xóa bước này"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </div>
                    <textarea
                      rows={2}
                      placeholder="Mô tả cụ thể cách thao tác: độ lửa, dấu hiệu món ăn chín, mẹo nhỏ tránh bị khét..."
                      value={st.description}
                      onChange={(e) => handleStepChange(st.id, "description", e.target.value)}
                      className="block w-full rounded-lg border border-stone-200 bg-stone-50/50 p-3 text-sm text-stone-800 focus:outline-none focus:border-brand-500 focus:bg-white transition-colors"
                    />
                  </div>
                ))}
              </CardContent>
              <CardFooter className="flex items-center justify-between pt-4">
                <Button href="/recipes" variant="ghost" size="md">
                  Hủy bỏ
                </Button>
                <div className="flex items-center gap-3">
                  <Button
                    type="submit"
                    variant="default"
                    size="md"
                    isLoading={isSubmitting}
                    leftIcon={<Save className="w-4 h-4" />}
                  >
                    Lưu bản nháp công thức
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
                  {imageUrl ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img
                      src={imageUrl}
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
