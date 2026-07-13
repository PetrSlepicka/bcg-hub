import { useEffect, useState } from "react";
import { api } from "../../api";
import type { OrderDetail, PartnerListItem, SaveOrder } from "../../domain";
import { SearchableSelect } from "../../shared/SearchableSelect";

export function OrderEditor({ order, initialCustomerId, onClose, onSaved }: { order?: OrderDetail; initialCustomerId?: string; onClose: () => void; onSaved: (saved: OrderDetail) => void | Promise<void> }) {
  const parsedNumber = /^BCG_(\d{4})(\d{4})$/i.exec(order?.number ?? "");
  const initialYear = parsedNumber?.[1] ?? String(new Date().getFullYear());
  const initialSequence = parsedNumber ? String(Number(parsedNumber[2])) : "";
  const [customerOptions, setCustomerOptions] = useState<{ id: string; label: string }[]>(order ? [{ id: order.customer.id, label: order.customer.name }] : []);
  const [customerSearch, setCustomerSearch] = useState("");
  const [warehouses, setWarehouses] = useState<PartnerListItem[]>([]);
  const [carriers, setCarriers] = useState<PartnerListItem[]>([]);
  const [year, setYear] = useState(initialYear); const [sequence, setSequence] = useState(initialSequence); const [numberError, setNumberError] = useState("");
  const [form, setForm] = useState<SaveOrder>({ number: order?.number ?? "", pohodaOrderNumber: order?.pohodaOrderNumber, title: order?.title ?? "", status: order?.status ?? "New", customerId: order?.customer.id ?? initialCustomerId ?? "", customerContactId: order?.customerContactId, warehouseId: order?.warehouse?.id, carrierId: order?.carrier?.id, customsDeclarantId: order?.customsDeclarant?.id, orderedOn: order?.orderedOn, requestedDeliveryOn: order?.requestedDeliveryOn, plannedPickupOn: order?.plannedPickupOn, plannedDeliveryOn: order?.plannedDeliveryOn, valueCzk: order?.valueCzk ?? 0, weightKg: order?.weightKg ?? 0, volumeM3: order?.volumeM3 ?? 0, warehouseInstructions: order?.warehouseInstructions, version: order?.version ?? 0 });

  useEffect(() => {
    api.partners.list("Warehouse", "").then(x => setWarehouses(x.items));
    api.partners.list("Carrier", "").then(x => setCarriers(x.items));
    if (initialCustomerId && !order) api.partners.detail(initialCustomerId).then(customer => setCustomerOptions([{ id: customer.id, label: customer.email ? `${customer.name} · ${customer.email}` : customer.name }])).catch(() => undefined);
    if (!order) api.orders.list("", "number", true).then(result => { const sequences = result.items.map(x => /^BCG_(\d{4})(\d{4})$/i.exec(x.number)).filter((match): match is RegExpExecArray => !!match && match[1] === initialYear).map(match => Number(match[2])); setSequence(String(Math.max(0, ...sequences) + 1)); });
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    const timeout = window.setTimeout(() => { api.partners.list("Customer", customerSearch, controller.signal).then(result => { const loaded = result.items.map(customer => ({ id: customer.id, label: customer.email ? `${customer.name} · ${customer.email}` : customer.name })); setCustomerOptions(current => [...loaded, ...current.filter(option => option.id === form.customerId && !loaded.some(item => item.id === option.id))]); }).catch(caught => { if (caught?.name !== "AbortError") console.error(caught); }); }, 180);
    return () => { window.clearTimeout(timeout); controller.abort(); };
  }, [customerSearch, form.customerId]);

  const set = (key: keyof SaveOrder, value: unknown) => setForm(current => ({ ...current, [key]: value || undefined }));
  const label = (text: string, control: React.ReactNode, wide = false) => <label className={wide ? "wide" : undefined}><span>{text}</span>{control}</label>;
  const numberIsValid = /^\d{4}$/.test(year) && Number(sequence) >= 1 && Number(sequence) <= 9999;
  const submit = async () => { if (!/^\d{4}$/.test(year)) { setNumberError("Rok musí mít přesně 4 číslice."); return; } if (!/^\d{1,4}$/.test(sequence) || Number(sequence) < 1) { setNumberError("Číslo zakázky musí být v rozsahu 1 až 9999."); return; } const value = { ...form, number: `BCG_${year}${sequence.padStart(4, "0")}` }; setNumberError(""); const saved = order ? await api.orders.update(order.id, value) : await api.orders.create(value); await onSaved(saved); };

  return <section className="detail-pane partner-form-pane"><form className="partner-form" onSubmit={async event => { event.preventDefault(); await submit(); }}>
    <header><div><p className="eyebrow">{order ? "ÚPRAVA ZAKÁZKY" : "NOVÁ ZAKÁZKA"}</p><h2>{order ? "Upravit zakázku" : "Přidat novou zakázku"}</h2><p>Základní údaje, logistika a termíny zakázky.</p></div><div className="form-header-actions"><button type="button" className="secondary" onClick={onClose}>Zavřít</button><button className="primary" disabled={!numberIsValid || !form.title.trim() || !form.customerId}>Uložit</button></div></header>
    <div className="partner-form-content"><section className="form-card"><h3>Základní údaje</h3><p>Identifikace zakázky a zákazník</p><div className="form-grid">
      <label className="wide"><span>Číslo zakázky</span><div className="order-number-fields"><strong>BCG</strong><span>_</span><input aria-label="Rok zakázky" inputMode="numeric" maxLength={4} value={year} onChange={event => { setYear(event.target.value.replace(/\D/g, "")); setNumberError(""); }} /><input aria-label="Pořadové číslo zakázky" inputMode="numeric" min="1" max="9999" value={sequence} onChange={event => { setSequence(event.target.value.replace(/\D/g, "")); setNumberError(""); }} /></div>{numberError && <small className="inline-error">{numberError}</small>}<small className="field-hint">Prefix BCG je pevný, následuje rok a čtyřmístné pořadové číslo.</small></label>
      {label("Název", <input required placeholder="Název zakázky" value={form.title} onChange={event => set("title", event.target.value)} />, true)}
      {label("Zákazník", <SearchableSelect value={form.customerId} options={customerOptions} placeholder="Vyberte zákazníka" searchPlaceholder="Hledat název, e-mail, IČ nebo kontakt…" onChange={value => set("customerId", value)} onSearchChange={setCustomerSearch} />, true)}
    </div></section>
    <section className="form-card"><h3>Logistika</h3><p>Místa, doprava a parametry zásilky</p><div className="form-grid order-logistics-grid">{label("Sklad", <select value={form.warehouseId ?? ""} onChange={event => set("warehouseId", event.target.value)}><option value="">Nevybrán</option>{warehouses.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select>)}{label("Dopravce", <select value={form.carrierId ?? ""} onChange={event => set("carrierId", event.target.value)}><option value="">Nevybrán</option>{carriers.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select>)}{label("Hodnota (Kč)", <input type="number" min="0" value={form.valueCzk} onChange={event => set("valueCzk", Number(event.target.value))} />)}{label("Hmotnost (kg)", <input type="number" min="0" value={form.weightKg} onChange={event => set("weightKg", Number(event.target.value))} />)}{label("Objem (m³)", <input type="number" min="0" value={form.volumeM3} onChange={event => set("volumeM3", Number(event.target.value))} />)}</div></section>
    <section className="form-card"><h3>Termíny a pokyny</h3><p>Časový plán a informace pro sklad</p><div className="form-grid">{label("Objednáno", <input type="date" value={form.orderedOn ?? ""} onChange={event => set("orderedOn", event.target.value)} />)}{label("Plán doručení", <input type="date" value={form.plannedDeliveryOn ?? ""} onChange={event => set("plannedDeliveryOn", event.target.value)} />)}{label("Pokyny skladu", <textarea placeholder="Doplňující instrukce pro sklad…" value={form.warehouseInstructions ?? ""} onChange={event => set("warehouseInstructions", event.target.value)} />, true)}</div></section></div>
  </form></section>;
}
