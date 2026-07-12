import { ArrowDownAZ, LoaderCircle, Search } from "lucide-react";
import type { OrderListItem } from "../../domain";
import { formatDate, formatMoney, orderStatusLabels } from "./formatters";

interface Props {
  orders: OrderListItem[];
  totalCount: number;
  totalValue: number;
  selectedId?: string;
  search: string;
  sortBy: string;
  descending: boolean;
  loading: boolean;
  error?: string;
  setSelectedId: (id: string) => void;
  setSearch: (value: string) => void;
  setSortBy: (value: string) => void;
  setDescending: (update: (current: boolean) => boolean) => void;
}

export function OrderListPane(props: Props) {
  return <section className="list-pane"><header className="pane-header"><div><p className="eyebrow">OBCHOD A LOGISTIKA</p><h1>Zakázky</h1></div><button className="primary">+ Nová zakázka</button></header><div className="summary-strip"><div><small>Celkem zakázek</small><b>{props.totalCount}</b></div><div><small>Hodnota zobrazených</small><b>{formatMoney(props.totalValue)}</b></div></div><div className="toolbar"><label className="search"><Search size={16} /><input value={props.search} onChange={event => props.setSearch(event.target.value)} placeholder="Hledat zakázku, zákazníka…" /></label><select value={props.sortBy} onChange={event => props.setSortBy(event.target.value)}><option value="number">Číslo</option><option value="customer">Zákazník</option><option value="delivery">Doručení</option><option value="value">Hodnota</option></select><button className="icon-button" onClick={() => props.setDescending(current => !current)} title="Obrátit řazení"><ArrowDownAZ size={17} className={props.descending ? "flip" : ""} /></button></div>{props.error && <div className="error-banner">{props.error}</div>}<div className="order-list">{props.loading ? <div className="loader"><LoaderCircle className="spin" /> Načítám zakázky…</div> : props.orders.map(order => <OrderCard key={order.id} order={order} selected={props.selectedId === order.id} onSelect={() => props.setSelectedId(order.id)} />)}</div></section>;
}

function OrderCard({ order, selected, onSelect }: { order: OrderListItem; selected: boolean; onSelect: () => void }) {
  return <button className={`order-card ${selected ? "selected" : ""}`} onClick={onSelect}><div className="order-top"><b>{order.number}</b><span className={`status status-${order.status.toLowerCase()}`}>{orderStatusLabels[order.status]}</span></div><strong>{order.title}</strong><span className="customer">{order.customerName}</span><div className="order-meta"><span>{formatMoney(order.valueCzk)}</span><span>{order.weightKg.toLocaleString("cs-CZ")} kg</span><span>{formatDate(order.plannedDeliveryOn)}</span></div><div className="progress"><i style={{ width: `${order.totalSteps ? order.completedSteps / order.totalSteps * 100 : 0}%` }} /></div><small>{order.completedSteps} z {order.totalSteps} kroků dokončeno</small></button>;
}
