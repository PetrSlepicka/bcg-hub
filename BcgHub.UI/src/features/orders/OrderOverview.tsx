import type { OrderDetail } from "../../domain";
import { formatDate, formatMoney } from "./formatters";

export function OrderOverview({ order }: { order: OrderDetail }) { return <div className="overview-grid"><Info title="Termíny zakázky" rows={[["Objednáno", formatDate(order.orderedOn)], ["Požadované doručení", formatDate(order.requestedDeliveryOn)]]} /><Info title="Obchodní údaje" rows={[["Hodnota", formatMoney(order.valueCzk)], ["Zákazník", order.customer.name], ["Kontakt", order.customerContact ?? "—"]]} /></div>; }

function Info({ title, rows }: { title: string; rows: string[][] }) { return <div className="info-card"><h3>{title}</h3>{rows.map(([label, value]) => <div className="info-row" key={label}><span>{label}</span><b>{value}</b></div>)}</div>; }
