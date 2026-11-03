"use client";

import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/Button";
import { Pagination } from "@/components/ui/Pagination";
import { Alert } from "@/components/ui/Alert";
import { getErrorMessage } from "@/utils/apiError";
import { recipeManagementService as service, recipeStatusName, type ManagedRecipeItem, type WorkflowAction } from "@/services/recipeManagementService";
import type { PaginatedResult } from "@/types/recipe";

const tabs = { Draft: "Bản nháp", PendingApproval: "Chờ duyệt", Published: "Đã xuất bản", Archived: "Đã lưu trữ", Trash: "Thùng rác" };
const actions: Record<string, [WorkflowAction, string][]> = {
  Draft: [["submit", "Gửi phê duyệt"], ["delete", "Xóa nháp"]],
  PendingApproval: [["withdraw", "Thu hồi về bản nháp"], ["delete", "Chuyển vào thùng rác"]],
  Published: [["unpublish", "Ẩn để chỉnh sửa"], ["archive", "Lưu trữ"], ["delete", "Chuyển vào thùng rác"]],
  Archived: [["unarchive", "Chuyển về bản nháp"], ["delete", "Chuyển vào thùng rác"]],
  Trash: [["restore", "Khôi phục về bản nháp"]],
};

export function ManagedRecipes({ reviewQueue = false }: { reviewQueue?: boolean }) {
  const search = useSearchParams();
  const tab = reviewQueue ? "PendingApproval" : ((search.get("status") ?? "") in tabs ? search.get("status")! : "Draft");
  const rawPage = Number(search.get("page") ?? 1);
  const page = Number.isSafeInteger(rawPage) && rawPage >= 1 && rawPage <= 42949672 ? rawPage : 1;
  const [data, setData] = useState<PaginatedResult<ManagedRecipeItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [reload, setReload] = useState(0);
  const [busy, setBusy] = useState(false);
  const [decision, setDecision] = useState<{ item: ManagedRecipeItem; action: WorkflowAction; label: string } | null>(null);
  const [reason, setReason] = useState("");
  const navigate = useCallback((status: string, pageIndex = 1) => {
    window.history.pushState(null, "", `${reviewQueue ? "/admin/recipes/review" : "/dashboard/recipes"}?status=${status}&page=${pageIndex}`);
    setDecision(null); setNotice(null);
  }, [reviewQueue]);
  useEffect(() => {
    const controller = new AbortController();
    setLoading(true); setError(null); setData(null);
    service.list(tab, page, reviewQueue, controller.signal).then(result => {
      if (!controller.signal.aborted) {
        if (page > Math.max(1, result.totalPages)) navigate(tab, Math.max(1, result.totalPages));
        else setData(result);
      }
    }).catch(err => { if (!controller.signal.aborted) setError(getErrorMessage(err)); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [tab, page, reviewQueue, reload, navigate]);
  async function confirm() {
    if (!decision || busy) return;
    setBusy(true); setError(null);
    try {
      await service.act(decision.item, decision.action, reason);
      setNotice(`${decision.label}: thành công.`); setDecision(null); setReason("");
      setReload(value => value + 1);
    } catch (err) { setError(getErrorMessage(err)); }
    finally { setBusy(false); }
  }
  return <main className="mx-auto max-w-6xl px-4 py-10 space-y-6">
    <div className="flex flex-wrap items-center justify-between gap-4"><div>
      <h1 className="text-3xl font-bold">{reviewQueue ? "Duyệt công thức" : "Món ăn của tôi"}</h1>
      <p className="mt-2 text-stone-500">{reviewQueue ? "Kiểm tra nội dung trước khi công khai. Bài bị từ chối trở về bản nháp kèm lý do." : "Lưu nháp, gửi duyệt và quản lý những công thức của bạn."}</p>
    </div><Button href={reviewQueue ? "/dashboard/recipes" : "/dashboard/recipes/new"}>{reviewQueue ? "Món ăn của tôi" : "Viết món mới"}</Button></div>
    {!reviewQueue && <nav aria-label="Trạng thái công thức" className="flex flex-wrap gap-2">{Object.entries(tabs).map(([key, label]) =>
      <Button key={key} variant={key === tab ? "default" : "outline"} onClick={() => navigate(key)} disabled={busy} aria-current={key === tab ? "page" : undefined}>{label}</Button>)}</nav>}
    {notice && <p role="status" className="rounded-xl bg-green-50 p-4 text-green-800">{notice}</p>}
    {error && <Alert variant="error"><p>{error}</p><Button variant="outline" onClick={() => { setDecision(null); setReload(x => x + 1); }}>Tải lại danh sách</Button></Alert>}
    {decision && <section role="dialog" aria-modal="false" aria-labelledby="decision-title" className="rounded-2xl border border-orange-300 bg-orange-50 p-5 space-y-3 text-stone-900">
      <h2 id="decision-title" className="font-bold">{decision.label}: {decision.item.title}</h2>
      <p>{decision.action === "delete" ? "Công thức sẽ bị ẩn và chuyển vào thùng rác. Dữ liệu và ảnh được giữ lại." : decision.action === "publish" ? "Công thức sẽ được công khai cho mọi người." : "Xác nhận thay đổi trạng thái công thức?"}</p>
      {decision.action === "reject" && <label className="block">Lý do từ chối<textarea className="mt-2 w-full rounded-lg border p-3" value={reason} onChange={e => setReason(e.target.value)} maxLength={1000} rows={3} /></label>}
      <div className="flex gap-3"><Button onClick={confirm} disabled={busy || (decision.action === "reject" && !reason.trim())}>{busy ? "Đang xử lý..." : "Xác nhận"}</Button><Button variant="outline" disabled={busy} onClick={() => setDecision(null)}>Hủy</Button></div>
    </section>}
    {loading ? <p role="status">Đang tải công thức...</p> : !error && data?.items.length === 0 ? <p className="rounded-2xl border p-8">Chưa có công thức trong mục này.</p> :
      <div className="grid gap-4">{data?.items.map(item => {
        const status = item.isDeleted ? "Trash" : recipeStatusName(item.status);
        const buttons: [WorkflowAction, string][] = reviewQueue ? [["publish", "Phê duyệt xuất bản"], ["reject", "Từ chối"]] : actions[status] ?? [];
        return <article key={item.id} className="rounded-2xl border border-surface-border bg-white p-5 dark:bg-stone-900 space-y-3">
          <h2 className="text-xl font-semibold break-words">{item.title}</h2>
          <p className="text-sm text-stone-500">{tabs[status as keyof typeof tabs]} · {new Date(item.createdAt).toLocaleDateString("vi-VN")}</p>
          {item.reviewNote && <p className="rounded-lg bg-amber-50 p-3 text-amber-900">Lý do từ chối: {item.reviewNote}</p>}
          <div className="flex flex-wrap gap-2">
            {!item.isDeleted && <Button variant="outline" href={`/recipes/${encodeURIComponent(item.slug)}`}>Xem {status === "Published" ? "chi tiết" : "trước"}</Button>}
            {status === "Draft" && <Button variant="outline" href={`/dashboard/recipes/${item.id}/edit`}>Chỉnh sửa</Button>}
            {buttons.map(([action, label]) => <Button key={action} variant="outline" disabled={busy} onClick={() => { setDecision({ item, action, label }); setReason(""); setError(null); }}>{label}</Button>)}
          </div>
        </article>;
      })}</div>}
    {data && <Pagination currentPage={page} totalPages={data.totalPages} onPageChange={index => navigate(tab, index)} isLoading={loading || busy} />}
  </main>;
}
