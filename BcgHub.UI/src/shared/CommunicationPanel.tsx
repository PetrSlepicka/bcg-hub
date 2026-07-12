import { useEffect, useState } from "react";
import { Trash2 } from "lucide-react";
import { api } from "../api";
import type { Communication } from "../domain";

export function CommunicationPanel({ partnerId, orderId }: { partnerId?: string; orderId?: string }) {
  const [items, setItems] = useState<Communication[]>([]);
  const [subject, setSubject] = useState("");
  const [body, setBody] = useState("");
  const reload = () => api.communications.list(partnerId, orderId).then(result => setItems(result.items));
  useEffect(() => { reload(); }, [partnerId, orderId]);
  return <div className="resource-panel"><form onSubmit={async event => { event.preventDefault(); await api.communications.create({ type: "Note", businessPartnerId: partnerId, orderId, subject, bodyPreview: body, occurredAtUtc: new Date().toISOString(), version: 0 }); setSubject(""); setBody(""); reload(); }}><input required value={subject} onChange={event => setSubject(event.target.value)} placeholder="Předmět" /><textarea value={body} onChange={event => setBody(event.target.value)} placeholder="Poznámka z hovoru, schůzky…" /><button className="primary">Přidat záznam</button></form><div className="data-table-wrap embedded"><table className="data-table"><thead><tr><th>Datum</th><th>Typ</th><th>Předmět</th><th>Obsah</th><th className="actions">Akce</th></tr></thead><tbody>{items.map(item => <tr key={`${item.type}-${item.id}`}><td>{new Date(item.occurredAtUtc).toLocaleString("cs-CZ")}</td><td>{communicationTypeLabel(item.type)}</td><td><strong>{item.subject}</strong></td><td title={item.bodyPreview}>{item.bodyPreview || "—"}</td><td className="actions">{item.type !== "Email" && <button className="icon-button" title="Smazat záznam" onClick={async () => { await api.communications.remove(item.id, item.version); reload(); }}><Trash2 size={15} /></button>}</td></tr>)}</tbody></table></div></div>;
}

function communicationTypeLabel(type: Communication["type"]) { return ({ Email: "E-mail", Phone: "Telefon", Meeting: "Schůzka", Note: "Poznámka" })[type]; }
