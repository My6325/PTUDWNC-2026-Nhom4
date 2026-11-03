"use client";

import { useEffect, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/Button";
import { Alert } from "@/components/ui/Alert";
import { useRecipeCategories } from "@/hooks/useRecipeCategories";
import { recipeService } from "@/services/recipeService";
import { recipeManagementService as management, recipeStatusName, type EditorStep, type EditorIngredient, type ManagedRecipe } from "@/services/recipeManagementService";
import { getErrorMessage } from "@/utils/apiError";
import { RecipeDifficulty, type RecipeNutritionDto, type UpdateRecipeRequest } from "@/types/recipe";

const inputClass = "w-full mt-1 rounded-xl border border-surface-border bg-white p-3 text-stone-900 dark:bg-stone-900 dark:text-white";
const emptyForm = { title: "", description: "", categoryId: "", prepTimeMinutes: 15, cookTimeMinutes: 30, servings: 4, difficulty: "Easy" };
const difficultyName = (value: number | string) => ({ 1: "Easy", 2: "Medium", 3: "Hard" } as Record<string, string>)[String(value)] ?? String(value);

export function RecipeEditor({ id }: { id?: string }) {
  const router = useRouter();
  const { categories, isLoading: categoriesLoading, error: categoriesError, retry } = useRecipeCategories();
  const [form, setForm] = useState(emptyForm);
  const [steps, setSteps] = useState<EditorStep[]>([]);
  const [ingredients, setIngredients] = useState<EditorIngredient[]>([]);
  const [nutrition, setNutrition] = useState<RecipeNutritionDto>({});
  const [loaded, setLoaded] = useState<ManagedRecipe | null>(null);
  const [loading, setLoading] = useState(!!id);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [reload, setReload] = useState(0);
  const [dirty, setDirty] = useState(false);
  const [removeImage, setRemoveImage] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    const controller = new AbortController();
    setLoading(true); setError(null);
    management.get(id, controller.signal).then(data => {
      if (controller.signal.aborted) return;
      setLoaded(data);
      const r = data.recipe;
      setForm({ title: r.title, description: r.description ?? "", categoryId: r.categoryId ?? "",
        prepTimeMinutes: r.prepTimeMinutes, cookTimeMinutes: r.cookTimeMinutes, servings: r.servings, difficulty: difficultyName(r.difficulty) });
      setSteps(data.steps); setIngredients(data.ingredients); setNutrition(data.nutrition); setDirty(false);
    }).catch(err => { if (!controller.signal.aborted) setError(getErrorMessage(err)); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [id, reload]);
  useEffect(() => {
    if (!dirty) return;
    const warn = (event: BeforeUnloadEvent) => { event.preventDefault(); event.returnValue = ""; };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);

  async function save(event: FormEvent) {
    event.preventDefault();
    if (busy) return;
    setBusy(true); setError(null); setNotice(null);
    try {
      const request = { ...form, difficulty: ({ Easy: RecipeDifficulty.Easy, Medium: RecipeDifficulty.Medium, Hard: RecipeDifficulty.Hard })[form.difficulty as "Easy" | "Medium" | "Hard"], categoryId: form.categoryId || null, nutrition,
        instructions: loaded?.recipe.instructions ?? null };
      if (!id) {
        const recipe = await recipeService.createDraft({ ...request, steps, ingredients });
        setDirty(false);
        router.replace(`/dashboard/recipes/${recipe.id}/edit`);
      } else if (loaded) {
        const updated = await management.save(id, { ...request, rowVersion: loaded.recipe.rowVersion } as UpdateRecipeRequest, steps, ingredients);
        setLoaded({ ...loaded, recipe: updated }); setDirty(false);
        setNotice("Đã lưu bản nháp. Bạn có thể tải ảnh và gửi phê duyệt từ Món ăn của tôi.");
        setReload(x => x + 1);
      }
    } catch (err) { setError(getErrorMessage(err)); }
    finally { setBusy(false); }
  }
  async function upload(file?: File) {
    if (!id || !file || busy) return;
    if (dirty) { setError("Hãy lưu thay đổi nội dung trước khi quản lý ảnh."); return; }
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) {
      setError("Chọn ảnh JPEG, PNG hoặc WebP tối đa 5 MB."); return;
    }
    setBusy(true); setError(null);
    try { await recipeService.uploadImage(id, file); setReload(x => x + 1); setNotice("Đã tải ảnh. Ảnh đầu tiên được chọn làm ảnh chính."); }
    catch (err) { setError(getErrorMessage(err)); }
    finally { setBusy(false); }
  }
  async function deleteImage() {
    if (!id || !removeImage || busy) return;
    setBusy(true); setError(null);
    try { await recipeService.deleteImage(id, removeImage); setRemoveImage(null); setReload(x => x + 1); }
    catch (err) { setError(getErrorMessage(err)); }
    finally { setBusy(false); }
  }
  if (loading) return <p role="status" className="p-8">Đang tải bản nháp...</p>;
  if (id && !loaded) return <div className="p-8 space-y-4"><Alert variant="error">{error}</Alert><Button onClick={() => setReload(x => x + 1)}>Thử lại</Button><Button href="/dashboard/recipes">Món ăn của tôi</Button></div>;
  if (loaded && recipeStatusName(loaded.recipe.status) !== "Draft") return <div className="p-8 space-y-4"><p>Chỉ bản nháp được chỉnh sửa. Hãy thu hồi bài chờ duyệt hoặc ẩn bài đã xuất bản trước.</p><Button href="/dashboard/recipes">Món ăn của tôi</Button></div>;
  return <main className="mx-auto max-w-5xl px-4 py-10 space-y-6">
    <div className="flex flex-wrap justify-between gap-4"><div><h1 className="text-3xl font-bold">{id ? "Chỉnh sửa bản nháp" : "Viết món mới"}</h1><p className="mt-2 text-stone-500">Lưu bản nháp để tiếp tục chỉnh sửa bất cứ lúc nào. Công thức chỉ công khai sau khi Admin duyệt.</p></div><Button variant="outline" onClick={() => { if (!dirty || window.confirm("Bạn có thay đổi chưa lưu. Rời trang?")) router.push('/dashboard/recipes'); }}>Món ăn của tôi</Button></div>
    {loaded?.reviewNote && <Alert variant="warning">Lý do từ chối: {loaded.reviewNote}</Alert>}
    {error && <Alert variant="error">{error}{id && <Button variant="outline" disabled={busy} onClick={() => { if (!dirty || window.confirm("Tải lại sẽ bỏ thay đổi chưa lưu. Tiếp tục?")) setReload(x => x + 1); }}>Tải lại bản nháp</Button>}</Alert>}
    {notice && <p role="status" className="rounded-xl bg-green-50 p-4 text-green-800">{notice}</p>}
    <form onSubmit={save} onChange={() => { setDirty(true); setNotice(null); }} className="space-y-6">
      <fieldset disabled={busy} className="rounded-2xl border border-surface-border bg-white p-5 dark:bg-stone-900 space-y-4">
        <legend className="px-2 text-xl font-bold">Thông tin công thức</legend>
        <label className="block">Tên món ăn<input aria-label="Tên món ăn" className={inputClass} required minLength={5} maxLength={200} value={form.title} onChange={e => setForm({ ...form, title: e.target.value })} /></label>
        <label className="block">Mô tả<textarea aria-label="Mô tả" className={inputClass} required maxLength={2000} rows={3} value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} /></label>
        <div className="grid gap-4 sm:grid-cols-2">
          <label>Danh mục<select className={inputClass} value={form.categoryId} disabled={categoriesLoading || !!categoriesError} onChange={e => setForm({ ...form, categoryId: e.target.value })}><option value="">Chọn danh mục</option>{categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}</select></label>
          <label>Độ khó<select className={inputClass} value={form.difficulty} onChange={e => setForm({ ...form, difficulty: e.target.value })}><option value="Easy">Dễ</option><option value="Medium">Trung bình</option><option value="Hard">Khó</option></select></label>
        </div>
        {categoriesError && <div role="alert">{categoriesError}<Button variant="outline" onClick={retry}>Thử tải danh mục</Button></div>}
        <div className="grid gap-4 sm:grid-cols-3">{([['prepTimeMinutes','Chuẩn bị (phút)'],['cookTimeMinutes','Nấu (phút)'],['servings','Khẩu phần']] as const).map(([key,label]) => <label key={key}>{label}<input className={inputClass} type="number" min={1} max={10000} required value={form[key]} onChange={e => setForm({ ...form, [key]: Number(e.target.value) })} /></label>)}</div>
      </fieldset>
      <fieldset disabled={busy} className="rounded-2xl border p-5 space-y-4"><legend className="px-2 text-xl font-bold">Nguyên liệu</legend>
        {ingredients.map((item,index) => <div key={item.id ?? index} className="grid gap-3 sm:grid-cols-2 rounded-xl bg-stone-50 p-4 dark:bg-stone-900">
          <label>Tên nguyên liệu<input className={inputClass} required maxLength={200} value={item.name} onChange={e => setIngredients(ingredients.map((v,i) => i === index ? { ...v, name: e.target.value } : v))} /></label>
          <label>Số lượng<input className={inputClass} type="number" min="0.001" step="any" value={item.quantity ?? ''} onChange={e => setIngredients(ingredients.map((v,i) => i === index ? { ...v, quantity: e.target.value ? Number(e.target.value) : null } : v))} /></label>
          <label>Đơn vị<input className={inputClass} value={item.unit ?? ''} onChange={e => setIngredients(ingredients.map((v,i) => i === index ? { ...v, unit: e.target.value } : v))} /></label>
          <label>Ghi chú<input className={inputClass} value={item.notes ?? ''} onChange={e => setIngredients(ingredients.map((v,i) => i === index ? { ...v, notes: e.target.value } : v))} /></label>
          <Button variant="outline" onClick={() => { setIngredients(ingredients.filter((_,i) => i !== index)); setDirty(true); }}>Bỏ nguyên liệu {index + 1}</Button>
        </div>)}<Button variant="outline" onClick={() => { setIngredients([...ingredients, { name: '', quantity: null, unit: '', notes: '' }]); setDirty(true); }}>Thêm nguyên liệu</Button>
      </fieldset>
      <fieldset disabled={busy} className="rounded-2xl border p-5 space-y-4"><legend className="px-2 text-xl font-bold">Các bước nấu</legend>
        {steps.map((step,index) => <div key={step.id ?? index} className="rounded-xl bg-stone-50 p-4 dark:bg-stone-900 space-y-3">
          <label className="block">Tiêu đề bước {index + 1}<input className={inputClass} required maxLength={200} value={step.title} onChange={e => setSteps(steps.map((v,i) => i === index ? { ...v, title: e.target.value } : v))} /></label>
          <label className="block">Hướng dẫn<textarea className={inputClass} required maxLength={2000} value={step.description} onChange={e => setSteps(steps.map((v,i) => i === index ? { ...v, description: e.target.value } : v))} /></label>
          <label className="block">Hẹn giờ (phút, không bắt buộc)<input className={inputClass} type="number" min={1} value={step.timerMinutes ?? ''} onChange={e => setSteps(steps.map((v,i) => i === index ? { ...v, timerMinutes: e.target.value ? Number(e.target.value) : null } : v))} /></label>
          <Button variant="outline" onClick={() => { setSteps(steps.filter((_,i) => i !== index)); setDirty(true); }}>Bỏ bước {index + 1}</Button>
        </div>)}<Button variant="outline" onClick={() => { setSteps([...steps, { title: '', description: '', timerMinutes: null }]); setDirty(true); }}>Thêm bước nấu</Button>
      </fieldset>
      <fieldset disabled={busy} className="rounded-2xl border p-5"><legend className="px-2 text-xl font-bold">Dinh dưỡng mỗi khẩu phần (không bắt buộc)</legend>
        <div className="grid gap-4 sm:grid-cols-3">{([['calories','Năng lượng (kcal)'],['protein','Đạm (g)'],['carbohydrates','Carbohydrate (g)'],['fat','Chất béo (g)'],['fiber','Chất xơ (g)'],['sodium','Natri (mg)']] as const).map(([key,label]) => <label key={key}>{label}<input className={inputClass} type="number" min={0} step={key === 'calories' ? 1 : '0.01'} value={nutrition[key] ?? ''} onChange={e => setNutrition({ ...nutrition, [key]: e.target.value ? Number(e.target.value) : null })} /></label>)}</div>
      </fieldset>
      <Button type="submit" disabled={busy}>{busy ? "Đang lưu..." : "Lưu bản nháp"}</Button>
    </form>
    <section className="rounded-2xl border p-5 space-y-4"><h2 className="text-xl font-bold">Ảnh món ăn</h2>
      {!id ? <p>Lưu bản nháp trước để tải ảnh. Ảnh chính là điều kiện bắt buộc khi gửi duyệt.</p> : <>
        <p className="text-sm text-stone-500">Ảnh JPEG, PNG hoặc WebP tối đa 5 MB. Ảnh đầu tiên là ảnh chính. Lưu nội dung trước khi quản lý ảnh.</p>
        <label className="block">Tải ảnh<input aria-label="Tải ảnh" type="file" accept="image/jpeg,image/png,image/webp" disabled={busy || dirty} className="block mt-2 max-w-full" onChange={e => { void upload(e.target.files?.[0]); e.target.value = ''; }} /></label>
        <div className="grid gap-4 sm:grid-cols-3">{loaded?.images.map(image => <div key={image.id} className="space-y-2">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={image.mediumUrl ?? image.originalUrl} alt={image.isPrimary ? 'Ảnh chính món ăn' : 'Ảnh món ăn'} className="aspect-video w-full rounded-xl object-cover" />
          {image.isPrimary && <span className="text-sm font-semibold">Ảnh chính</span>}
          <Button variant="outline" disabled={busy || dirty} onClick={() => setRemoveImage(image.id)}>Xóa ảnh</Button>
        </div>)}</div>
        {removeImage && <div role="alert" className="space-y-3"><p>Xóa ảnh này? Thao tác xóa ảnh riêng không thể hoàn tác.</p><Button variant="destructive" disabled={busy} onClick={deleteImage}>Xác nhận xóa ảnh</Button><Button variant="outline" onClick={() => setRemoveImage(null)} disabled={busy}>Hủy</Button></div>}
      </>}
    </section>
  </main>;
}
