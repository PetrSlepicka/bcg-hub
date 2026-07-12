import { useEffect, useState } from "react";
import { Plus, Trash2 } from "lucide-react";
import { api } from "../../api";
import type { EmailTemplate } from "../../domain";
import { RichTextEditor } from "./RichTextEditor";

const emptyTemplate = (): EmailTemplate => ({ id: "", name: "", subject: "", bodyHtml: "<p></p>", version: 0 });

export function EmailTemplatesModal({ onClose }: { onClose: () => void }) {
  return <div className="modal-backdrop"><section className="settings-modal template-modal"><header><div><p className="eyebrow">E-MAILY</p><h2>Šablony odpovědí</h2></div><button className="secondary" onClick={onClose}>Zavřít</button></header><EmailTemplatesPanel onClose={onClose} showCancel /></section></div>;
}

export function EmailTemplatesPanel({ onClose, showCancel = false }: { onClose?: () => void; showCancel?: boolean }) {
  const [templates, setTemplates] = useState<EmailTemplate[]>([]); const [selected, setSelected] = useState<EmailTemplate>(emptyTemplate()); const [loading, setLoading] = useState(true); const [saving, setSaving] = useState(false); const [error, setError] = useState<string>();
  useEffect(() => { api.emailTemplates.list().then(items => { setTemplates(items); if (items[0]) setSelected(items[0]); }).catch(() => setError("Šablony se nepodařilo načíst.")).finally(() => setLoading(false)); }, []);
  const save = async (event: React.FormEvent) => { event.preventDefault(); setSaving(true); setError(undefined); try { const saved = selected.id ? await api.emailTemplates.update(selected) : await api.emailTemplates.create({ name: selected.name, subject: selected.subject, bodyHtml: selected.bodyHtml }); setTemplates(current => [...current.filter(x => x.id !== saved.id), saved].sort((a, b) => a.name.localeCompare(b.name, "cs"))); setSelected(saved); } catch (caught) { setError(caught instanceof Error ? caught.message : "Šablonu se nepodařilo uložit."); } finally { setSaving(false); } };
  const remove = async () => { if (!selected.id || !window.confirm(`Smazat šablonu „${selected.name}“?`)) return; try { await api.emailTemplates.remove(selected); const remaining = templates.filter(x => x.id !== selected.id); setTemplates(remaining); setSelected(remaining[0] ?? emptyTemplate()); } catch (caught) { setError(caught instanceof Error ? caught.message : "Šablonu se nepodařilo smazat."); } };
  return loading ? <div className="loader">Načítám šablony…</div> : <div className="template-layout"><aside><button className="primary" onClick={() => setSelected(emptyTemplate())}><Plus size={15} /> Nová šablona</button>{templates.map(template => <button key={template.id} className={selected.id === template.id ? "selected" : ""} onClick={() => setSelected(template)}>{template.name}</button>)}</aside><form onSubmit={save}>{error && <div className="error-banner">{error}</div>}<label><span>Název šablony</span><input required maxLength={200} value={selected.name} onChange={e => setSelected(x => ({ ...x, name: e.target.value }))} /></label><label><span>Výchozí předmět</span><input maxLength={1000} value={selected.subject} onChange={e => setSelected(x => ({ ...x, subject: e.target.value }))} /></label><label><span>Obsah</span><RichTextEditor value={selected.bodyHtml} onChange={bodyHtml => setSelected(x => ({ ...x, bodyHtml }))} /></label><footer>{selected.id && <button type="button" className="danger secondary" onClick={remove}><Trash2 size={15} /> Smazat</button>}<span />{showCancel && <button type="button" className="secondary" onClick={onClose}>Zrušit</button>}<button className="primary" disabled={saving}>{saving ? "Ukládám…" : "Uložit šablonu"}</button></footer></form></div>;
}
