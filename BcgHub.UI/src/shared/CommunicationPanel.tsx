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
  return <div className="resource-panel"><form onSubmit={async event => { event.preventDefault(); await api.communications.create({ type: "Note", businessPartnerId: partnerId, orderId, subject, bodyPreview: body, occurredAtUtc: new Date().toISOString(), version: 0 }); setSubject(""); setBody(""); reload(); }}><input required value={subject} onChange={event => setSubject(event.target.value)} placeholder="Předmět" /><textarea value={body} onChange={event => setBody(event.target.value)} placeholder="Poznámka z hovoru, schůzky…" /><button className="primary">Přidat záznam</button></form>{items.map(item => <article className="info-card wide" key={item.id}><b>{item.subject}</b><small>{item.type} · {new Date(item.occurredAtUtc).toLocaleString("cs-CZ")}</small><p>{item.bodyPreview}</p><button className="icon-button" onClick={async () => { await api.communications.remove(item.id, item.version); reload(); }}><Trash2 size={15} /></button></article>)}</div>;
}
