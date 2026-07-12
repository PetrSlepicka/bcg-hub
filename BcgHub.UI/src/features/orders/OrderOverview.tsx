import { Truck } from "lucide-react";
import type { OrderDetail } from "../../domain";
import { formatDate, formatMoney } from "./formatters";

export function OrderOverview({ order }: { order: OrderDetail }) {
  return <div className="overview-grid"><Info title="Termíny" rows={[["Objednáno", formatDate(order.orderedOn)], ["Požadované doručení", formatDate(order.requestedDeliveryOn)], ["Nakládka", formatDate(order.plannedPickupOn)], ["Doručení", formatDate(order.plannedDeliveryOn)]]} /><Info title="Objem zakázky" rows={[["Hodnota", formatMoney(order.valueCzk)], ["Hmotnost", `${order.weightKg.toLocaleString("cs-CZ")} kg`], ["Objem", `${order.volumeM3.toLocaleString("cs-CZ")} m³`]]} /><Info title="Partneři" rows={[["Sklad", order.warehouse?.name ?? "—"], ["Dopravce", order.carrier?.name ?? "—"], ["Celní deklarant", order.customsDeclarant?.name ?? "—"]]} /><div className="info-card wide"><h3>Pokyny skladu</h3><p>{order.warehouseInstructions ?? "Zatím nebyly zadány žádné pokyny."}</p></div>{order.transportQuotes.length > 0 && <div className="info-card wide"><h3><Truck size={17} /> Vybraná doprava</h3>{order.transportQuotes.map(quote => <div className="quote" key={quote.id}><b>{quote.carrier.name}</b><span>{quote.price.toLocaleString("cs-CZ")} {quote.currency}</span><small>{quote.notes}</small></div>)}</div>}</div>;
}

function Info({ title, rows }: { title: string; rows: string[][] }) { return <div className="info-card"><h3>{title}</h3>{rows.map(([label, value]) => <div className="info-row" key={label}><span>{label}</span><b>{value}</b></div>)}</div>; }
