import { useEffect, useRef, useState } from "react";
import { api } from "../../api";
import type { OrderDetail } from "../../domain";
import { OrderDetailView } from "./OrderDetailView";
import { OrderEditor } from "./OrderEditor";
import { OrderListPane } from "./OrderListPane";
import { PohodaImportModal } from "./PohodaImportModal";
import { useOrdersWorkspace } from "./useOrdersWorkspace";

export function OrdersWorkspace({ initialCustomerId, onInitialCreateHandled, onInitialOrderCreated, onInitialCreateCancelled }: { initialCustomerId?: string; onInitialCreateHandled?: () => void; onInitialOrderCreated?: (order: OrderDetail) => Promise<void>; onInitialCreateCancelled?: () => void }) {
  const state = useOrdersWorkspace();
  const [editing, setEditing] = useState<"new" | "edit" | undefined>(initialCustomerId !== undefined ? "new" : undefined);
  const [automationError, setAutomationError] = useState<string>();
  const [pohodaImportOpen, setPohodaImportOpen] = useState(false);
  const initialCreatePending = useRef(initialCustomerId !== undefined);
  useEffect(() => { if (initialCustomerId !== undefined) onInitialCreateHandled?.(); }, []);
  const saved = async (order: OrderDetail) => { if (editing === "new" && initialCreatePending.current && onInitialOrderCreated) { initialCreatePending.current = false; try { await onInitialOrderCreated(order); } catch { setAutomationError("Zakázka byla založena, ale e-mail se k ní nepodařilo automaticky přiřadit. Přiřaďte jej ručně v E-mailech."); } } setEditing(undefined); state.refresh(); };
  const closeEditor = () => { if (initialCreatePending.current) { initialCreatePending.current = false; onInitialCreateCancelled?.(); } setEditing(undefined); };
  return <div className="split-view"><OrderListPane {...state} error={automationError ?? state.error} onImport={() => setPohodaImportOpen(true)} onCreate={() => { initialCreatePending.current = false; setAutomationError(undefined); setEditing("new"); }} />{editing ? <OrderEditor order={editing === "edit" ? state.detail : undefined} initialCustomerId={initialCustomerId} onClose={closeEditor} onSaved={saved} /> : <OrderDetailView order={state.detail} onEdit={() => setEditing("edit")} onDelete={async () => { if (state.detail && confirm("Opravdu zakázku smazat?")) { await api.orders.remove(state.detail.id, state.detail.version); state.refresh(); } }} />}{pohodaImportOpen && <PohodaImportModal onClose={() => setPohodaImportOpen(false)} onImported={state.refresh} />}</div>;
}
