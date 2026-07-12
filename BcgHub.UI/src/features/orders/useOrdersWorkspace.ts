import { useEffect, useMemo, useState } from "react";
import { ApiError, api } from "../../api";
import type { OrderDetail, OrderListItem, WorkflowStep, WorkflowStepStatus } from "../../domain";

export function useOrdersWorkspace() {
  const [orders, setOrders] = useState<OrderListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [selectedId, setSelectedId] = useState<string>();
  const [detail, setDetail] = useState<OrderDetail>();
  const [search, setSearch] = useState("");
  const [sortBy, setSortBy] = useState("number");
  const [descending, setDescending] = useState(true);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();
  const [updatingSteps, setUpdatingSteps] = useState<ReadonlySet<string>>(new Set());
  const [refreshToken, setRefreshToken] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      setLoading(true);
      api.orders.list(search, sortBy, descending, controller.signal).then(result => { setOrders(result.items); setTotalCount(result.totalCount); setSelectedId(current => current && result.items.some(x => x.id === current) ? current : result.items[0]?.id); setError(undefined); }).catch(caught => { if (caught?.name !== "AbortError") setError("Zakázky se nepodařilo načíst."); }).finally(() => { if (!controller.signal.aborted) setLoading(false); });
    }, 180);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [search, sortBy, descending, refreshToken]);

  useEffect(() => {
    if (!selectedId) { setDetail(undefined); return; }
    const controller = new AbortController();
    const requestedId = selectedId;
    api.orders.detail(requestedId, controller.signal).then(order => setDetail(current => requestedId === selectedId || current?.id === requestedId ? order : current)).catch(caught => { if (caught?.name !== "AbortError") setError("Detail zakázky se nepodařilo načíst."); });
    return () => controller.abort();
  }, [selectedId]);

  const updateStep = async (step: WorkflowStep, status: WorkflowStepStatus) => {
    const orderId = detail?.id;
    if (!orderId || updatingSteps.has(step.id)) return;
    setUpdatingSteps(current => new Set(current).add(step.id));
    try
    {
      const updated = await api.orders.updateStep(orderId, step, status);
      setDetail(current => current?.id === orderId ? { ...current, workflowSteps: current.workflowSteps.map(item => item.id === updated.id ? updated : item) } : current);
      const wasCompleted = ["Completed", "NotRequired"].includes(step.status);
      const isCompleted = ["Completed", "NotRequired"].includes(updated.status);
      const delta = Number(isCompleted) - Number(wasCompleted);
      setOrders(current => current.map(order => order.id === orderId ? { ...order, completedSteps: Math.max(0, Math.min(order.totalSteps, order.completedSteps + delta)) } : order));
    }
    catch (caught)
    {
      setError(caught instanceof ApiError && caught.status === 409 ? caught.message : "Stav kroku se nepodařilo uložit.");
      if (caught instanceof ApiError && caught.status === 409) api.orders.detail(orderId).then(current => setDetail(detailState => detailState?.id === orderId ? current : detailState));
    }
    finally { setUpdatingSteps(current => { const next = new Set(current); next.delete(step.id); return next; }); }
  };

  return { orders, totalCount, selectedId, detail, search, sortBy, descending, loading, error, updatingSteps, totalValue: useMemo(() => orders.reduce((sum, order) => sum + order.valueCzk, 0), [orders]), setSelectedId, setSearch, setSortBy, setDescending, updateStep, refresh: () => setRefreshToken(x => x + 1) };
}
