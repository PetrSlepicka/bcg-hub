import { useEffect, useState } from "react";
import { Download, Trash2, Upload } from "lucide-react";
import { api } from "../api";
import type { AttachmentItem, CommentItem, ResourceOwnerType } from "../domain";

export function EntityResourcesPanel({ ownerType, ownerId, mode }: { ownerType: ResourceOwnerType; ownerId: string; mode: "comments" | "files" }) {
  const [comments, setComments] = useState<CommentItem[]>([]);
  const [files, setFiles] = useState<AttachmentItem[]>([]);
  const [text, setText] = useState("");
  const reload = () => mode === "comments" ? api.resources.comments(ownerType, ownerId).then(setComments) : api.resources.attachments(ownerType, ownerId).then(setFiles);
  useEffect(() => { reload(); }, [ownerType, ownerId, mode]);
  if (mode === "comments") return <div className="resource-panel"><form onSubmit={async event => { event.preventDefault(); if (!text.trim()) return; await api.resources.addComment(ownerType, ownerId, text); setText(""); reload(); }}><textarea value={text} onChange={event => setText(event.target.value)} placeholder="Nový komentář…" /><button className="primary">Přidat komentář</button></form>{comments.map(item => <article className="info-card wide" key={item.id}><b>{item.authorName}</b><small>{new Date(item.createdAtUtc).toLocaleString("cs-CZ")}</small><p>{item.text}</p><button className="icon-button" onClick={async () => { await api.resources.removeComment(item.id, item.version); reload(); }}><Trash2 size={15} /></button></article>)}</div>;
  return <div className="resource-panel"><label className="primary file-upload"><Upload size={15} /> Nahrát soubor<input type="file" onChange={async event => { const file = event.target.files?.[0]; if (file) { await api.resources.upload(ownerType, ownerId, file); reload(); } }} /></label>{files.map(file => <article className="info-card wide resource-row" key={file.id}><span><b>{file.fileName}</b><small>{Math.ceil(file.size / 1024)} kB</small></span><a className="icon-button" href={api.resources.downloadUrl(file.id)}><Download size={15} /></a><button className="icon-button" onClick={async () => { await api.resources.removeAttachment(file.id, file.version); reload(); }}><Trash2 size={15} /></button></article>)}</div>;
}
