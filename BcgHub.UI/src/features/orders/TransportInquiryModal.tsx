import { useEffect, useState } from "react";
import { LoaderCircle, Send } from "lucide-react";
import { api } from "../../api";
import type { EmailTemplate, OrderDetail, TransportInquiryCarrier, TransportType } from "../../domain";
import { RichTextEditor } from "../emails/RichTextEditor";

const transportTypes: { value: TransportType; label: string }[] = [{ value: "Road", label: "Pozemní" }, { value: "Sea", label: "Námořní" }, { value: "Air", label: "Letecká" }];

export function TransportInquiryModal({ order, onClose }: { order: OrderDetail; onClose: () => void }) {
  const [transportType, setTransportType] = useState<TransportType>("Road");
  const [carriers, setCarriers] = useState<TransportInquiryCarrier[]>([]);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [templates, setTemplates] = useState<EmailTemplate[]>([]);
  const [subject, setSubject] = useState(`[${order.number}] Poptávka dopravy`);
  const [bodyHtml, setBodyHtml] = useState(`<p>Dobrý den,</p><p>prosíme o nabídku dopravy pro zakázku <strong>${order.number}</strong>.</p><p>Děkujeme.</p>`);
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string>();

  useEffect(() => { api.emailTemplates.list().then(setTemplates).catch(() => setError("Šablony se nepodařilo načíst.")); }, []);
  useEffect(() => { const controller = new AbortController(); setLoading(true); setError(undefined); api.orders.transportInquiry(order.id, transportType, controller.signal).then(context => { setCarriers(context.carriers); setSelectedIds(new Set(context.carriers.map(x => x.id))); }).catch(caught => { if ((caught as Error).name !== "AbortError") setError(caught instanceof Error ? caught.message : "Dopravce se nepodařilo načíst."); }).finally(() => setLoading(false)); return () => controller.abort(); }, [order.id, transportType]);

  const applyTemplate = (templateId: string) => { const template = templates.find(x => x.id === templateId); if (!template) return; setBodyHtml(replaceTokens(template.bodyHtml, order)); setSubject(ensureOrderNumber(replaceTokens(template.subject || "Poptávka dopravy", order), order.number)); };
  const send = async (event: React.FormEvent) => { event.preventDefault(); setSending(true); setError(undefined); try { const result = await api.orders.sendTransportInquiry(order.id, { transportType, carrierIds: [...selectedIds], subject: ensureOrderNumber(subject, order.number), bodyHtml }); window.alert(`Poptávka byla odeslána ${result.sentCount} dopravcům.`); onClose(); } catch (caught) { setError(caught instanceof Error ? caught.message : "Poptávku se nepodařilo odeslat."); } finally { setSending(false); } };

  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget && !sending) onClose(); }}><section className="settings-modal transport-inquiry-modal" role="dialog" aria-modal="true"><header><div><p className="eyebrow">DOPRAVA</p><h2>Poptat dopravu</h2></div><button type="button" className="secondary" onClick={onClose} disabled={sending}>Zavřít</button></header><form onSubmit={send}>{error && <div className="error-banner">{error}</div>}<div className="transport-inquiry-fields"><label><span>Typ dopravy</span><select value={transportType} onChange={event => setTransportType(event.target.value as TransportType)}>{transportTypes.map(type => <option key={type.value} value={type.value}>{type.label}</option>)}</select></label><label><span>E-mailová šablona</span><select defaultValue="" onChange={event => { applyTemplate(event.target.value); event.currentTarget.value = ""; }}><option value="">Vyberte šablonu…</option>{templates.map(template => <option key={template.id} value={template.id}>{template.name}</option>)}</select></label><label className="wide"><span>Předmět</span><input required value={subject} onChange={event => setSubject(ensureOrderNumber(event.target.value, order.number))} /><small>ID zakázky {order.number} zůstává v předmětu kvůli automatickému přiřazení odpovědí.</small></label></div><fieldset className="carrier-selection"><legend>Dopravci pro vybraný typ</legend>{loading ? <div className="loader"><LoaderCircle className="spin" /> Načítám dopravce…</div> : carriers.length ? carriers.map(carrier => <label key={carrier.id}><input type="checkbox" checked={selectedIds.has(carrier.id)} onChange={event => setSelectedIds(current => { const next = new Set(current); if (event.target.checked) next.add(carrier.id); else next.delete(carrier.id); return next; })} /><span><b>{carrier.name}</b><small>{carrier.email}</small></span></label>) : <p>Pro tento typ dopravy není evidován žádný dopravce s e-mailovou adresou.</p>}</fieldset><label className="transport-inquiry-body"><span>Text e-mailu</span><RichTextEditor value={bodyHtml} onChange={setBodyHtml} /></label><footer><button type="button" className="secondary" onClick={onClose} disabled={sending}>Zrušit</button><button className="primary" disabled={sending || selectedIds.size === 0 || loading}><Send size={15} /> {sending ? "Odesílám jednotlivé e-maily…" : `Odeslat (${selectedIds.size})`}</button></footer></form></section></div>;
}

function ensureOrderNumber(subject: string, orderNumber: string) { return subject.toLocaleUpperCase("cs-CZ").includes(orderNumber.toLocaleUpperCase("cs-CZ")) ? subject : `[${orderNumber}] ${subject.replace(/^\s+/, "")}`; }
function replaceTokens(value: string, order: OrderDetail) { return value.replaceAll("{{orderNumber}}", order.number).replaceAll("{{orderTitle}}", order.title).replaceAll("{{customerName}}", order.customer.name); }
