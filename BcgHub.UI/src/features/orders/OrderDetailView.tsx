import { useState } from "react";
import { FileText, MessageSquareText, PackageOpen, Paperclip, Clock3 } from "lucide-react";
import type { OrderDetail, WorkflowStep, WorkflowStepStatus } from "../../domain";
import { orderStatusLabels } from "./formatters";
import { OrderOverview } from "./OrderOverview";
import { Workflow } from "./Workflow";

export function OrderDetailView({ order, updatingSteps, onStepChange }: { order?: OrderDetail; updatingSteps: ReadonlySet<string>; onStepChange: (step: WorkflowStep, status: WorkflowStepStatus) => void }) {
  const [tab, setTab] = useState<"workflow" | "overview" | "files" | "comments">("workflow");
  if (!order) return <section className="detail-pane"><div className="detail-placeholder"><PackageOpen size={36} /><p>Vyberte zakázku ze seznamu.</p></div></section>;
  return <section className="detail-pane"><header className="detail-header"><div><p className="eyebrow">{order.number} · {order.pohodaOrderNumber ?? "Bez čísla Pohoda"}</p><h2>{order.title}</h2><p>{order.customer.name}{order.customerContact ? ` · ${order.customerContact}` : ""}</p></div><span className={`status large status-${order.status.toLowerCase()}`}>{orderStatusLabels[order.status]}</span></header><nav className="tabs"><Tab active={tab === "workflow"} onClick={() => setTab("workflow")} icon={<Clock3 size={16} />} label="Průběh" /><Tab active={tab === "overview"} onClick={() => setTab("overview")} icon={<FileText size={16} />} label="Přehled" /><Tab active={tab === "files"} onClick={() => setTab("files")} icon={<Paperclip size={16} />} label="Soubory" /><Tab active={tab === "comments"} onClick={() => setTab("comments")} icon={<MessageSquareText size={16} />} label="Komentáře" /></nav><div className="detail-content">{tab === "workflow" && <Workflow steps={order.workflowSteps} updatingSteps={updatingSteps} onChange={onStepChange} />}{tab === "overview" && <OrderOverview order={order} />}{tab === "files" && <TabPlaceholder icon={<Paperclip />} text="Přiložené objednávky, faktury, CMR a VDD budou dostupné zde." />}{tab === "comments" && <TabPlaceholder icon={<MessageSquareText />} text="Komentáře k zakázce budou sdílené pro všechny zapojené osoby." />}</div></section>;
}

function Tab({ active, onClick, icon, label }: { active: boolean; onClick: () => void; icon: React.ReactNode; label: string }) { return <button className={active ? "active" : ""} onClick={onClick}>{icon} {label}</button>; }
function TabPlaceholder({ icon, text }: { icon: React.ReactNode; text: string }) { return <div className="tab-placeholder">{icon}<p>{text}</p><button className="secondary">Přidat</button></div>; }
