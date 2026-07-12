import { useEffect } from "react";
import { EditorContent, useEditor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Underline from "@tiptap/extension-underline";
import { Bold, Italic, Link, List, ListOrdered, Redo2, Underline as UnderlineIcon, Undo2 } from "lucide-react";

type Variant = "email" | "comment";

export function RichTextEditor({ value, onChange, variant = "email" }: { value: string; onChange: (html: string) => void; variant?: Variant }) {
  const editor = useEditor({ extensions: [StarterKit.configure({ link: { openOnClick: false, protocols: ["http", "https", "mailto", "tel"] } }), Underline], content: value, onUpdate: ({ editor }) => onChange(editor.getHTML()) });
  useEffect(() => { if (editor && editor.getHTML() !== value) editor.commands.setContent(value); }, [editor, value]);
  if (!editor) return null;
  const setLink = () => {
    const current = editor.getAttributes("link").href as string | undefined;
    const entered = window.prompt("Adresa odkazu", current ?? "https://");
    if (entered === null) return;
    if (!entered.trim()) { editor.chain().focus().unsetLink().run(); return; }
    const href = normalizeLink(entered);
    if (!href) { window.alert("Odkaz musí používat http, https, mailto nebo tel."); return; }
    editor.chain().focus().extendMarkRange("link").setLink({ href }).run();
  };
  return <div className={variant === "comment" ? "comment-editor" : "rich-editor"}><div className={variant === "comment" ? "comment-toolbar" : "rich-toolbar"}><Tool title="Tučně" active={editor.isActive("bold")} onClick={() => editor.chain().focus().toggleBold().run()}><Bold /></Tool><Tool title="Kurzíva" active={editor.isActive("italic")} onClick={() => editor.chain().focus().toggleItalic().run()}><Italic /></Tool><Tool title="Podtržení" active={editor.isActive("underline")} onClick={() => editor.chain().focus().toggleUnderline().run()}><UnderlineIcon /></Tool>{variant === "comment" && <><Tool title="Nadpis 1" active={editor.isActive("heading", { level: 1 })} onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}>H₁</Tool><Tool title="Nadpis 2" active={editor.isActive("heading", { level: 2 })} onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}>H₂</Tool></>}<Tool title="Odkaz" active={editor.isActive("link")} onClick={setLink}><Link /></Tool><Tool title="Odrážky" active={editor.isActive("bulletList")} onClick={() => editor.chain().focus().toggleBulletList().run()}><List /></Tool>{variant === "email" && <><Tool title="Číslování" active={editor.isActive("orderedList")} onClick={() => editor.chain().focus().toggleOrderedList().run()}><ListOrdered /></Tool><span /><Tool title="Zpět" onClick={() => editor.chain().focus().undo().run()}><Undo2 /></Tool><Tool title="Znovu" onClick={() => editor.chain().focus().redo().run()}><Redo2 /></Tool></>}</div><EditorContent editor={editor} /></div>;
}

export function sanitizeRichText(value: string) {
  if (!value.trim().startsWith("<")) return escapeHtml(value).replace(/\r?\n/g, "<br>");
  const document = new DOMParser().parseFromString(value, "text/html");
  const allowedTags = new Set(["A", "B", "BLOCKQUOTE", "BR", "CODE", "EM", "H1", "H2", "H3", "I", "LI", "OL", "P", "PRE", "S", "STRONG", "U", "UL"]);
  [...document.body.querySelectorAll("*")].reverse().forEach(element => {
    if (!allowedTags.has(element.tagName)) { element.replaceWith(...element.childNodes); return; }
    const linkHref = element.tagName === "A" ? element.getAttribute("href") : null;
    [...element.attributes].forEach(attribute => element.removeAttribute(attribute.name));
    if (element.tagName !== "A") return;
    const href = normalizeLink(linkHref ?? "");
    if (!href) { element.replaceWith(...element.childNodes); return; }
    element.setAttribute("href", href);
    element.setAttribute("target", "_blank");
    element.setAttribute("rel", "noopener noreferrer");
  });
  return document.body.innerHTML;
}

function Tool({ title, active, onClick, children }: { title: string; active?: boolean; onClick: () => void; children: React.ReactNode }) { return <button type="button" title={title} aria-label={title} className={active ? "active" : ""} onClick={onClick}>{children}</button>; }

function normalizeLink(value: string) {
  const href = value.trim();
  if (!href || /[\u0000-\u001f\u007f]/.test(href)) return undefined;
  if (/^(\/|#|\.\.?(\/|$))/i.test(href)) return href;
  try { const url = new URL(href); return ["http:", "https:", "mailto:", "tel:"].includes(url.protocol) ? href : undefined; }
  catch { return undefined; }
}

function escapeHtml(value: string) { const element = document.createElement("div"); element.textContent = value; return element.innerHTML; }
