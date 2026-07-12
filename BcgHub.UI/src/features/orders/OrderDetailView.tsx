import { useState } from "react";
import { Clock3, FileText, MessageSquareText, PackageOpen, Paperclip, Truck } from "lucide-react";
import type { OrderDetail, WorkflowStep, WorkflowStepStatus } from "../../domain";
import { CommunicationPanel } from "../../shared/CommunicationPanel";
import { EntityResourcesPanel } from "../../shared/EntityResourcesPanel";
import { orderStatusLabels } from "./formatters";
import { OrderOverview } from "./OrderOverview";
import { OrderTransport } from "./OrderTransport";
import { Workflow } from "./Workflow";

export function OrderDetailView({ order, updatingSteps, onStepChange, onEdit, onDelete }: { order?: OrderDetail; updatingSteps: ReadonlySet<string>; onStepChange: (step: WorkflowStep, status: WorkflowStepStatus) => void; onEdit: () => void; onDelete: () => void }) {
  const [tab, setTab] = useState<"workflow" | "overview" | "transport" | "communication" | "files" | "comments">("workflow");
  if (!order) return <section className="detail-pane"><div className="detail-placeholder"><PackageOpen size={36} /><p>Vyberte zakázku ze seznamu.</p></div></section>;
  return <section className="detail-pane"><header className="detail-header"><div><p className="eyebrow">{order.number} · {order.pohodaOrderNumber ?? "Bez čísla Pohoda"}</p><h2>{order.title}</h2><p>{order.customer.name}{order.customerContact ? ` · ${order.customerContact}` : ""}</p></div><div><span className={`status large status-${order.status.toLowerCase()}`}>{orderStatusLabels[order.status]}</span><button className="secondary" onClick={onEdit}>Upravit</button><button className="secondary" onClick={onDelete}>Smazat</button></div></header><nav className="tabs"><Tab active={tab === "workflow"} onClick={() => setTab("workflow")} icon={<Clock3 size={16} />} label="Průběh" /><Tab active={tab === "overview"} onClick={() => setTab("overview")} icon={<FileText size={16} />} label="Přehled" /><Tab active={tab === "transport"} onClick={() => setTab("transport")} icon={<Truck size={16} />} label="Doprava" /><Tab active={tab === "communication"} onClick={() => setTab("communication")} icon={<MessageSquareText size={16} />} label="Komunikace" /><Tab active={tab === "files"} onClick={() => setTab("files")} icon={<Paperclip size={16} />} label="Soubory" /><Tab active={tab === "comments"} onClick={() => setTab("comments")} icon={<MessageSquareText size={16} />} label="Komentáře" /></nav><div className="detail-content">{tab === "workflow" && <Workflow steps={order.workflowSteps} updatingSteps={updatingSteps} onChange={onStepChange} />}{tab === "overview" && <OrderOverview order={order} />}{tab === "transport" && <OrderTransport order={order} />}{tab === "communication" && <CommunicationPanel orderId={order.id} />}{tab === "files" && <EntityResourcesPanel ownerType="Order" ownerId={order.id} mode="files" />}{tab === "comments" && <EntityResourcesPanel ownerType="Order" ownerId={order.id} mode="comments" />}</div></section>;
}
function Tab({ active, onClick, icon, label }: { active: boolean; onClick: () => void; icon: React.ReactNode; label: string }) { return <button className={active ? "active" : ""} onClick={onClick}>{icon} {label}</button>; }
