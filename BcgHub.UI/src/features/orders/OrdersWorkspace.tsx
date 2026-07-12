import { useState } from "react";
import { api } from "../../api";
import { OrderDetailView } from "./OrderDetailView";
import { OrderEditor } from "./OrderEditor";
import { OrderListPane } from "./OrderListPane";
import { useOrdersWorkspace } from "./useOrdersWorkspace";

export function OrdersWorkspace() { const state = useOrdersWorkspace(); const [editing, setEditing] = useState<"new" | "edit">(); const saved = () => { setEditing(undefined); state.refresh(); }; return <div className="split-view"><OrderListPane {...state} onCreate={() => setEditing("new")} />{editing ? <OrderEditor order={editing === "edit" ? state.detail : undefined} onClose={() => setEditing(undefined)} onSaved={saved} /> : <OrderDetailView order={state.detail} updatingSteps={state.updatingSteps} onStepChange={state.updateStep} onEdit={() => setEditing("edit")} onDelete={async () => { if (state.detail && confirm("Opravdu zakázku smazat?")) { await api.orders.remove(state.detail.id, state.detail.version); state.refresh(); } }} />}</div>; }
