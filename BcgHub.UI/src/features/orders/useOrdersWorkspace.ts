import { useEffect, useState } from "react";
import { ApiError, api } from "../../api";
import type { OrderDetail, OrderListItem, OrderSalesChannel, WorkflowStep, WorkflowStepStatus } from "../../domain";

export function useOrdersWorkspace(requestedEntityId?: string, onSelectedEntityIdChange?: (id?: string) => void) {
  const [orders, setOrders] = useState<OrderListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const pageSize = 50;
  const [selectedId, setSelectedId] = useState<string | undefined>(requestedEntityId);
  const [detail, setDetail] = useState<OrderDetail>();
  const [search, setSearch] = useState("");
  const [salesChannel, setSalesChannel] = useState<OrderSalesChannel>("All");
  const [sortBy, setSortBy] = useState("number");
  const [descending, setDescending] = useState(true);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();
  const [updatingSteps, setUpdatingSteps] = useState<ReadonlySet<string>>(new Set());
  const [refreshToken, setRefreshToken] = useState(0);

  useEffect(() => { if (requestedEntityId) setSelectedId(requestedEntityId); }, [requestedEntityId]);

  useEffect(() => {
    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      setLoading(true);
      api.orders.list(search, sortBy, descending, controller.signal, page, pageSize, undefined, salesChannel).then(result => { setOrders(result.items); setTotalCount(result.totalCount); setSelectedId(current => requestedEntityId && current === requestedEntityId ? current : current && result.items.some(x => x.id === current) ? current : result.items[0]?.id); setError(undefined); }).catch(caught => { if (caught?.name !== "AbortError") setError("Zakázky se nepodařilo načíst."); }).finally(() => { if (!controller.signal.aborted) setLoading(false); });
    }, 180);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [search, salesChannel, sortBy, descending, page, refreshToken]);

  useEffect(() => {
    if (!selectedId) { setDetail(undefined); return; }
    const controller = new AbortController();
    const requestedId = selectedId;
    api.orders.detail(requestedId, controller.signal).then(order => setDetail(current => requestedId === selectedId || current?.id === requestedId ? order : current)).catch(caught => { if (caught?.name !== "AbortError") setError("Detail zakázky se nepodařilo načíst."); });
    return () => controller.abort();
  }, [selectedId]);

  useEffect(() => { onSelectedEntityIdChange?.(selectedId); }, [selectedId]);

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

  const changeSearch = (value: string) => { setSearch(value); setPage(1); };
  const changeSalesChannel = (value: OrderSalesChannel) => { setSalesChannel(value); setPage(1); };
  const changeSortBy = (value: string) => { setSortBy(value); setPage(1); };
  return { orders, totalCount, page, pageSize, selectedId, detail, search, salesChannel, sortBy, descending, loading, error, updatingSteps, setSelectedId, setSearch: changeSearch, setSalesChannel: changeSalesChannel, setSortBy: changeSortBy, setDescending, setPage, updateStep, refresh: () => setRefreshToken(x => x + 1) };
}
