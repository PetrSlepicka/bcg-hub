import { useEffect, useState } from "react";
import { Link2, Reply } from "lucide-react";
import { api } from "../../api";
import type { EmailMessage, EmailOrderOptions } from "../../domain";
import { EntityResourcesPanel } from "../../shared/EntityResourcesPanel";
import { EmailComposer } from "./EmailComposer";

export function EmailDetail({ email, onChange, onSent }: { email: EmailMessage; onChange: (email: EmailMessage) => void; onSent: (email: EmailMessage) => void }) {
  const [partnerId, setPartnerId] = useState(email.businessPartnerId ?? "");
  const [orderId, setOrderId] = useState(email.orderId ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string>();
  const [partners, setPartners] = useState<{ id: string; name: string }[]>([]);
  const [orderOptions, setOrderOptions] = useState<EmailOrderOptions>({ suggested: [], other: [] });
  const [composing, setComposing] = useState(false);

  useEffect(() => { setPartnerId(email.businessPartnerId ?? ""); setOrderId(email.orderId ?? ""); }, [email.id, email.businessPartnerId, email.orderId]);
  useEffect(() => {
    const controller = new AbortController();
    Promise.all([api.emails.orderOptions(email.id, controller.signal), ...(["Customer", "Lead", "Supplier", "Warehouse", "Carrier", "CustomsDeclarant", "Collaborator"] as const).map(type => api.partners.list(type, "", controller.signal))]).then(([options, ...partnerResults]) => {
      setOrderOptions(options);
      setPartners(partnerResults.flatMap(result => result.items).map(partner => ({ id: partner.id, name: partner.name })).sort((a, b) => a.name.localeCompare(b.name, "cs")));
    }).catch(caught => { if (caught?.name !== "AbortError") console.error(caught); });
    return () => controller.abort();
  }, [email.id]);

  const saveLink = async () => { setSaving(true); setError(undefined); try { onChange(await api.emails.link(email, partnerId, orderId)); } catch (caught) { setError(caught instanceof Error ? caught.message : "Vazbu se nepodařilo uložit."); } finally { setSaving(false); } };
  const option = (order: EmailOrderOptions["other"][number]) => <option key={order.id} value={order.id}>{order.number} · {order.title} · {order.customerName}</option>;

  return <>
    <header className="detail-header"><div><p className="eyebrow">{email.direction === "Inbound" ? "PŘÍCHOZÍ E-MAIL" : "ODESLANÝ E-MAIL"}</p><h2>{email.subject}</h2><p>{email.fromName ? `${email.fromName} · ${email.fromAddress}` : email.fromAddress} → {email.toAddress}</p></div><button className="primary" onClick={() => setComposing(true)}><Reply size={15} /> Odpovědět</button></header>
    <div className="email-link-bar"><Link2 size={16} /><label>Partner <select value={partnerId} onChange={event => setPartnerId(event.target.value)}><option value="">Bez přiřazení</option>{partners.map(partner => <option key={partner.id} value={partner.id}>{partner.name}</option>)}</select></label><label>Zakázka <select value={orderId} onChange={event => setOrderId(event.target.value)}><option value="">Bez přiřazení</option>{orderOptions.suggested.length > 0 && <optgroup label="Pravděpodobné zakázky">{orderOptions.suggested.map(option)}</optgroup>}<optgroup label="──────── Ostatní zakázky ────────">{orderOptions.other.map(option)}</optgroup></select></label><button className="secondary" onClick={saveLink} disabled={saving}>Uložit vazbu</button>{error && <span className="inline-error">{error}</span>}</div>
    <div className="email-detail-content"><div className="email-envelope"><span><b>Od:</b> {email.fromAddress}</span><span><b>Komu:</b> {email.toAddress}</span><span><b>Datum:</b> {new Intl.DateTimeFormat("cs-CZ", { dateStyle: "long", timeStyle: "short" }).format(new Date(email.occurredAtUtc))}</span></div>{email.bodyHtml ? <iframe title="Obsah e-mailu" sandbox="" srcDoc={email.bodyHtml} /> : <pre>{email.bodyText || "E-mail nemá textový obsah."}</pre>}{email.hasAttachments && <><h3>Přílohy</h3><EntityResourcesPanel ownerType="EmailMessage" ownerId={email.id} mode="files" /></>}</div>
    {composing && <EmailComposer email={email} onClose={() => setComposing(false)} onSent={sent => { setComposing(false); onSent(sent); }} />}
  </>;
}
