import type { ReactNode } from "react";
import type { OrderDetail } from "../../domain";
import { EntityLink, partnerHref } from "../../shared/EntityLink";
import { formatDate, formatMoney } from "./formatters";
import { TransportQuoteManager } from "./OrderTransport";

export function OrderBasicInfo({ order }: { order: OrderDetail }) {
  return <div className="order-basic-info"><div className="order-summary-grid"><Info title="Zakázka" rows={[["Zákazník", <EntityLink href={partnerHref(order.customer.id, "Customer")}>{order.customer.name}</EntityLink>], ["Kontakt", order.customerContact ?? "—"], ["Hodnota", formatMoney(order.valueCzk)], ["Objednáno", formatDate(order.orderedOn)], ["Požadované doručení", formatDate(order.requestedDeliveryOn)]]} /><Info title="Zásilka" rows={[["Hmotnost", `${order.weightKg.toLocaleString("cs-CZ")} kg`], ["Objem", `${order.volumeM3.toLocaleString("cs-CZ")} m³`], ["Nakládka", formatDate(order.plannedPickupOn)], ["Plán doručení", formatDate(order.plannedDeliveryOn)]]} /><Info title="Logistika" rows={[["Sklad", order.warehouse ? <EntityLink href={partnerHref(order.warehouse.id, "Warehouse")}>{order.warehouse.name}</EntityLink> : "—"], ["Dopravce", order.carrier ? <EntityLink href={partnerHref(order.carrier.id, "Carrier")}>{order.carrier.name}</EntityLink> : "—"], ["Celní deklarant", order.customsDeclarant ? <EntityLink href={partnerHref(order.customsDeclarant.id, "CustomsDeclarant")}>{order.customsDeclarant.name}</EntityLink> : "—"]]} /></div><div className="info-card order-instructions"><h3>Pokyny skladu</h3><p>{order.warehouseInstructions ?? "Zatím nebyly zadány žádné pokyny."}</p></div><TransportQuoteManager order={order} /></div>;
}

function Info({ title, rows }: { title: string; rows: [string, ReactNode][] }) { return <section className="info-card"><h3>{title}</h3>{rows.map(([label, value]) => <div className="info-row" key={label}><span>{label}</span><b>{value}</b></div>)}</section>; }
