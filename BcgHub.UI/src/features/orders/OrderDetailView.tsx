import { useState } from "react";
import { FileText, MessageSquareText, PackageOpen, Paperclip } from "lucide-react";
import type { OrderDetail } from "../../domain";
import { CommunicationPanel } from "../../shared/CommunicationPanel";
import { EntityResourcesPanel } from "../../shared/EntityResourcesPanel";
import { orderStatusLabels } from "./formatters";
import { OrderBasicInfo } from "./OrderBasicInfo";

export function OrderDetailView({ order, onEdit, onDelete }: { order?: OrderDetail; onEdit: () => void; onDelete: () => void }) {
  const [tab, setTab] = useState<"basic" | "communication" | "files">("basic");
  if (!order) return <section className="detail-pane"><div className="detail-placeholder"><PackageOpen size={36} /><p>Vyberte zakázku ze seznamu.</p></div></section>;
  return <section className="detail-pane"><header className="detail-header"><div><div className="order-heading-line"><p className="eyebrow">{order.number} · {order.pohodaOrderNumber ?? "Bez čísla Pohoda"}</p><h2>{order.title}</h2></div><p>{order.customer.name}{order.customerContact ? ` · ${order.customerContact}` : ""}</p></div><div><span className={`status large status-${order.status.toLowerCase()}`}>{orderStatusLabels[order.status]}</span><button className="secondary" onClick={onEdit}>Upravit</button><button className="secondary" onClick={onDelete}>Smazat</button></div></header><nav className="tabs"><Tab active={tab === "basic"} onClick={() => setTab("basic")} icon={<FileText size={16} />} label="Základní údaje" /><Tab active={tab === "communication"} onClick={() => setTab("communication")} icon={<MessageSquareText size={16} />} label="Komunikace" /><Tab active={tab === "files"} onClick={() => setTab("files")} icon={<Paperclip size={16} />} label="Soubory" /></nav><div className="detail-body"><div className="detail-content">{tab === "basic" && <OrderBasicInfo order={order} />}{tab === "communication" && <CommunicationPanel orderId={order.id} />}{tab === "files" && <EntityResourcesPanel ownerType="Order" ownerId={order.id} mode="files" />}</div><aside className="detail-comments"><EntityResourcesPanel ownerType="Order" ownerId={order.id} mode="comments" /></aside></div></section>;
}
function Tab({ active, onClick, icon, label }: { active: boolean; onClick: () => void; icon: React.ReactNode; label: string }) { return <button className={active ? "active" : ""} onClick={onClick}>{icon} {label}</button>; }
