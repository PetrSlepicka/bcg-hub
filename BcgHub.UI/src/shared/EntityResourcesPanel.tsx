import { useEffect, useState } from "react";
import { Download, Pencil, Trash2, Upload } from "lucide-react";
import { api } from "../api";
import type { AttachmentItem, CommentItem, ResourceOwnerType } from "../domain";
import { RichTextEditor, sanitizeRichText } from "./RichTextEditor";

export function EntityResourcesPanel({ ownerType, ownerId, mode }: { ownerType: ResourceOwnerType; ownerId: string; mode: "comments" | "files" }) {
  const [comments, setComments] = useState<CommentItem[]>([]);
  const [files, setFiles] = useState<AttachmentItem[]>([]);
  const reload = () => mode === "comments" ? api.resources.comments(ownerType, ownerId).then(setComments) : api.resources.attachments(ownerType, ownerId).then(setFiles);
  useEffect(() => { reload(); }, [ownerType, ownerId, mode]);
  if (mode === "comments") return <CommentsPanel ownerType={ownerType} ownerId={ownerId} comments={comments} onReload={reload} />;
  return <div className="resource-panel"><label className="primary file-upload"><Upload size={15} /> Nahrát soubor<input type="file" onChange={async event => { const file = event.target.files?.[0]; if (file) { await api.resources.upload(ownerType, ownerId, file); reload(); } }} /></label><div className="data-table-wrap embedded"><table className="data-table"><thead><tr><th>Název souboru</th><th>Typ</th><th>Velikost</th><th>Nahráno</th><th className="actions">Akce</th></tr></thead><tbody>{files.map(file => <tr key={file.id}><td><strong>{file.fileName}</strong></td><td>{file.contentType || "—"}</td><td>{formatFileSize(file.size)}</td><td>{new Date(file.createdAtUtc).toLocaleDateString("cs-CZ")}</td><td className="actions"><a className="icon-button" title="Stáhnout" href={api.resources.downloadUrl(file.id)}><Download size={15} /></a><button className="icon-button" title="Smazat soubor" onClick={async () => { await api.resources.removeAttachment(file.id, file.version); reload(); }}><Trash2 size={15} /></button></td></tr>)}</tbody></table></div></div>;
}

function formatFileSize(size: number) { return size < 1024 * 1024 ? `${Math.ceil(size / 1024)} kB` : `${(size / 1024 / 1024).toLocaleString("cs-CZ", { maximumFractionDigits: 1 })} MB`; }

function CommentsPanel({ ownerType, ownerId, comments, onReload }: { ownerType: ResourceOwnerType; ownerId: string; comments: CommentItem[]; onReload: () => void }) {
  const [text, setText] = useState("");
  const [editing, setEditing] = useState<CommentItem>();
  const orderedComments = [...comments].sort((left, right) => new Date(right.createdAtUtc).getTime() - new Date(left.createdAtUtc).getTime());
  const save = async () => { if (isEmptyHtml(text)) return; if (editing) await api.resources.updateComment(editing, text); else await api.resources.addComment(ownerType, ownerId, text); setText(""); setEditing(undefined); onReload(); };
  const startEditing = (comment: CommentItem) => { setEditing(comment); setText(comment.text); };
  return <section className="comments-panel"><header><h3>Komentáře</h3><span>{comments.length}</span></header><div className="comment-list">{orderedComments.map(item => <article className="comment-card" key={item.id}><div className="comment-meta"><b>{item.authorName}</b><time>{new Date(item.createdAtUtc).toLocaleDateString("cs-CZ")}</time><span /><button className="icon-button" title="Upravit komentář" onClick={() => startEditing(item)}><Pencil size={15} /></button><button className="icon-button" title="Smazat komentář" onClick={async () => { await api.resources.removeComment(item.id, item.version); if (editing?.id === item.id) { setEditing(undefined); setText(""); } onReload(); }}><Trash2 size={15} /></button></div><div className="comment-body" dangerouslySetInnerHTML={{ __html: sanitizeRichText(item.text) }} /></article>)}</div><form onSubmit={async event => { event.preventDefault(); await save(); }}><label>{editing ? "Upravit komentář" : "Nový komentář"}</label><RichTextEditor value={text} onChange={setText} variant="comment" /><div className="comment-actions">{editing && <button type="button" className="secondary" onClick={() => { setEditing(undefined); setText(""); }}>Zrušit</button>}<button className="primary" disabled={isEmptyHtml(text)}>{editing ? "Uložit změny" : "Přidat komentář"}</button></div></form></section>;
}

function isEmptyHtml(value: string) { return !value.replace(/<[^>]*>/g, "").replace(/&nbsp;/g, " ").trim(); }
