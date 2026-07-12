import { useEffect, useState } from "react";
import { EditorContent, useEditor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Underline from "@tiptap/extension-underline";
import { Bold, Download, Italic, Link, List, Pencil, Trash2, Underline as UnderlineIcon, Upload } from "lucide-react";
import { api } from "../api";
import type { AttachmentItem, CommentItem, ResourceOwnerType } from "../domain";

export function EntityResourcesPanel({ ownerType, ownerId, mode }: { ownerType: ResourceOwnerType; ownerId: string; mode: "comments" | "files" }) {
  const [comments, setComments] = useState<CommentItem[]>([]);
  const [files, setFiles] = useState<AttachmentItem[]>([]);
  const reload = () => mode === "comments" ? api.resources.comments(ownerType, ownerId).then(setComments) : api.resources.attachments(ownerType, ownerId).then(setFiles);
  useEffect(() => { reload(); }, [ownerType, ownerId, mode]);
  if (mode === "comments") return <CommentsPanel ownerType={ownerType} ownerId={ownerId} comments={comments} onReload={reload} />;
  return <div className="resource-panel"><label className="primary file-upload"><Upload size={15} /> Nahrát soubor<input type="file" onChange={async event => { const file = event.target.files?.[0]; if (file) { await api.resources.upload(ownerType, ownerId, file); reload(); } }} /></label>{files.map(file => <article className="info-card wide resource-row" key={file.id}><span><b>{file.fileName}</b><small>{Math.ceil(file.size / 1024)} kB</small></span><a className="icon-button" href={api.resources.downloadUrl(file.id)}><Download size={15} /></a><button className="icon-button" onClick={async () => { await api.resources.removeAttachment(file.id, file.version); reload(); }}><Trash2 size={15} /></button></article>)}</div>;
}

function CommentsPanel({ ownerType, ownerId, comments, onReload }: { ownerType: ResourceOwnerType; ownerId: string; comments: CommentItem[]; onReload: () => void }) {
  const [text, setText] = useState("");
  const [editing, setEditing] = useState<CommentItem>();
  const orderedComments = [...comments].sort((left, right) => new Date(right.createdAtUtc).getTime() - new Date(left.createdAtUtc).getTime());
  const save = async () => { if (isEmptyHtml(text)) return; if (editing) await api.resources.updateComment(editing, text); else await api.resources.addComment(ownerType, ownerId, text); setText(""); setEditing(undefined); onReload(); };
  const startEditing = (comment: CommentItem) => { setEditing(comment); setText(comment.text); };
  return <section className="comments-panel"><header><h3>Komentáře</h3><span>{comments.length}</span></header><div className="comment-list">{orderedComments.map(item => <article className="comment-card" key={item.id}><div className="comment-meta"><b>{item.authorName}</b><time>{new Date(item.createdAtUtc).toLocaleDateString("cs-CZ")}</time><span /><button className="icon-button" title="Upravit komentář" onClick={() => startEditing(item)}><Pencil size={15} /></button><button className="icon-button" title="Smazat komentář" onClick={async () => { await api.resources.removeComment(item.id, item.version); if (editing?.id === item.id) { setEditing(undefined); setText(""); } onReload(); }}><Trash2 size={15} /></button></div><div className="comment-body" dangerouslySetInnerHTML={{ __html: sanitizeCommentHtml(item.text) }} /></article>)}</div><form onSubmit={async event => { event.preventDefault(); await save(); }}><label>{editing ? "Upravit komentář" : "Nový komentář"}</label><CommentEditor value={text} onChange={setText} /><div className="comment-actions">{editing && <button type="button" className="secondary" onClick={() => { setEditing(undefined); setText(""); }}>Zrušit</button>}<button className="primary" disabled={isEmptyHtml(text)}>{editing ? "Uložit změny" : "Přidat komentář"}</button></div></form></section>;
}

function CommentEditor({ value, onChange }: { value: string; onChange: (html: string) => void }) {
  const editor = useEditor({ extensions: [StarterKit.configure({ link: { openOnClick: false } }), Underline], content: value, onUpdate: ({ editor }) => onChange(editor.getHTML()) });
  useEffect(() => { if (editor && editor.getHTML() !== value) editor.commands.setContent(value); }, [editor, value]);
  if (!editor) return null;
  const setLink = () => { const current = editor.getAttributes("link").href as string | undefined; const href = window.prompt("Adresa odkazu", current ?? "https://"); if (href === null) return; if (href.trim()) editor.chain().focus().extendMarkRange("link").setLink({ href: href.trim() }).run(); else editor.chain().focus().unsetLink().run(); };
  return <div className="comment-editor"><div className="comment-toolbar"><EditorTool label="Tučně" active={editor.isActive("bold")} onClick={() => editor.chain().focus().toggleBold().run()}><Bold /></EditorTool><EditorTool label="Kurzíva" active={editor.isActive("italic")} onClick={() => editor.chain().focus().toggleItalic().run()}><Italic /></EditorTool><EditorTool label="Podtržení" active={editor.isActive("underline")} onClick={() => editor.chain().focus().toggleUnderline().run()}><UnderlineIcon /></EditorTool><EditorTool label="Nadpis 1" active={editor.isActive("heading", { level: 1 })} onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}>H₁</EditorTool><EditorTool label="Nadpis 2" active={editor.isActive("heading", { level: 2 })} onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}>H₂</EditorTool><EditorTool label="Odrážky" active={editor.isActive("bulletList")} onClick={() => editor.chain().focus().toggleBulletList().run()}><List /></EditorTool><EditorTool label="Odkaz" active={editor.isActive("link")} onClick={setLink}><Link /></EditorTool></div><EditorContent editor={editor} /></div>;
}

function EditorTool({ label, active, onClick, children }: { label: string; active?: boolean; onClick: () => void; children: React.ReactNode }) { return <button type="button" title={label} aria-label={label} className={active ? "active" : ""} onClick={onClick}>{children}</button>; }

function isEmptyHtml(value: string) { return !value.replace(/<[^>]*>/g, "").replace(/&nbsp;/g, " ").trim(); }

function sanitizeCommentHtml(value: string) {
  if (!value.trim().startsWith("<")) return escapeHtml(value).replace(/\r?\n/g, "<br>");
  const document = new DOMParser().parseFromString(value, "text/html");
  document.querySelectorAll("script,style,iframe,object,embed").forEach(element => element.remove());
  document.body.querySelectorAll("*").forEach(element => [...element.attributes].forEach(attribute => { if (attribute.name.startsWith("on") || (attribute.name === "href" && /^javascript:/i.test(attribute.value))) element.removeAttribute(attribute.name); }));
  return document.body.innerHTML;
}

function escapeHtml(value: string) { const element = document.createElement("div"); element.textContent = value; return element.innerHTML; }
