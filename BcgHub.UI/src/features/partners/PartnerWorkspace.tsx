import { useEffect, useMemo, useState } from "react";
import { ArrowUpDown, Building2, Mail, MapPin, Phone, Search, UserRound } from "lucide-react";
import { api } from "../../api";
import type { PartnerListItem, PartnerType } from "../../domain";

export function PartnerWorkspace({ type, title }: { type: PartnerType; title: string }) {
  const [items, setItems] = useState<PartnerListItem[]>([]);
  const [selectedId, setSelectedId] = useState<string>();
  const [search, setSearch] = useState("");
  const [descending, setDescending] = useState(false);
  const [error, setError] = useState<string>();

  useEffect(() => {
    const controller = new AbortController();
    const timer = window.setTimeout(() => api.partners.list(type, search, controller.signal).then(result => { setItems(result.items); setSelectedId(current => current && result.items.some(item => item.id === current) ? current : result.items[0]?.id); setError(undefined); }).catch(caught => { if (caught?.name !== "AbortError") setError("Seznam se nepodařilo načíst."); }), 180);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [type, search]);

  const sorted = useMemo(() => [...items].sort((a, b) => a.name.localeCompare(b.name, "cs") * (descending ? -1 : 1)), [items, descending]);
  const selected = items.find(item => item.id === selectedId);
  return <div className="split-view"><PartnerList title={title} items={sorted} selectedId={selectedId} search={search} error={error} onSearch={setSearch} onSort={() => setDescending(current => !current)} onSelect={setSelectedId} /><PartnerDetail title={title} partner={selected} /></div>;
}

function PartnerList({ title, items, selectedId, search, error, onSearch, onSort, onSelect }: { title: string; items: PartnerListItem[]; selectedId?: string; search: string; error?: string; onSearch: (value: string) => void; onSort: () => void; onSelect: (id: string) => void }) {
  return <section className="list-pane"><header className="pane-header"><div><p className="eyebrow">CRM A KONTAKTY</p><h1>{title}</h1></div><button className="primary">+ Přidat</button></header><div className="toolbar"><label className="search"><Search size={16} /><input value={search} onChange={event => onSearch(event.target.value)} placeholder={`Hledat v ${title.toLowerCase()}…`} /></label><button className="icon-button" onClick={onSort} title="Řadit podle názvu"><ArrowUpDown size={17} /></button></div>{error && <div className="error-banner">{error}</div>}<div className="partner-list">{items.map(item => <button key={item.id} className={`partner-row ${item.id === selectedId ? "selected" : ""}`} onClick={() => onSelect(item.id)}><span className="partner-icon"><Building2 size={18} /></span><span><b>{item.name}</b><small>{[item.city, item.countryCode].filter(Boolean).join(", ") || "Adresa není vyplněna"}</small></span><em>{item.contactCount}</em></button>)}{items.length === 0 && <div className="no-results">Zatím zde nejsou žádné záznamy.</div>}</div></section>;
}

function PartnerDetail({ title, partner }: { title: string; partner?: PartnerListItem }) {
  if (!partner) return <section className="detail-pane"><div className="detail-placeholder"><Building2 size={36} /><p>Vyberte záznam ze seznamu.</p></div></section>;
  return <section className="detail-pane"><header className="detail-header"><div><p className="eyebrow">{title.toUpperCase()}</p><h2>{partner.name}</h2><p>{partner.city}{partner.countryCode ? ` · ${partner.countryCode}` : ""}</p></div></header><div className="detail-content"><div className="contact-cards"><Contact icon={<MapPin />} label="Adresa" value={[partner.city, partner.countryCode].filter(Boolean).join(", ")} /><Contact icon={<Mail />} label="E-mail" value={partner.email} /><Contact icon={<Phone />} label="Telefon" value={partner.phone} /><Contact icon={<UserRound />} label="Kontaktní osoby" value={`${partner.contactCount}`} /></div><div className="info-card wide"><h3>Historie komunikace</h3><p className="muted">E-maily, hovory a osobní kontakty budou na jednom místě, včetně komunikace z jednotlivých zakázek.</p></div></div></section>;
}

function Contact({ icon, label, value }: { icon: React.ReactNode; label: string; value?: string }) { return <div className="contact-card"><span>{icon}</span><small>{label}</small><b>{value || "—"}</b></div>; }
