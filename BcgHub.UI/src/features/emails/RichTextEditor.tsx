import { useEffect } from "react";
import { EditorContent, useEditor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Underline from "@tiptap/extension-underline";
import { Bold, Italic, Link, List, ListOrdered, Redo2, Underline as UnderlineIcon, Undo2 } from "lucide-react";

export function RichTextEditor({ value, onChange }: { value: string; onChange: (html: string) => void }) {
  const editor = useEditor({ extensions: [StarterKit.configure({ link: { openOnClick: false } }), Underline], content: value, onUpdate: ({ editor }) => onChange(editor.getHTML()) });
  useEffect(() => { if (editor && editor.getHTML() !== value) editor.commands.setContent(value); }, [editor, value]);
  if (!editor) return null;
  const setLink = () => { const current = editor.getAttributes("link").href as string | undefined; const href = window.prompt("Adresa odkazu", current ?? "https://"); if (href === null) return; if (!href.trim()) editor.chain().focus().unsetLink().run(); else editor.chain().focus().extendMarkRange("link").setLink({ href: href.trim() }).run(); };
  return <div className="rich-editor"><div className="rich-toolbar"><Tool title="Tučně" active={editor.isActive("bold")} onClick={() => editor.chain().focus().toggleBold().run()}><Bold /></Tool><Tool title="Kurzíva" active={editor.isActive("italic")} onClick={() => editor.chain().focus().toggleItalic().run()}><Italic /></Tool><Tool title="Podtržení" active={editor.isActive("underline")} onClick={() => editor.chain().focus().toggleUnderline().run()}><UnderlineIcon /></Tool><Tool title="Odkaz" active={editor.isActive("link")} onClick={setLink}><Link /></Tool><Tool title="Odrážky" active={editor.isActive("bulletList")} onClick={() => editor.chain().focus().toggleBulletList().run()}><List /></Tool><Tool title="Číslování" active={editor.isActive("orderedList")} onClick={() => editor.chain().focus().toggleOrderedList().run()}><ListOrdered /></Tool><span /><Tool title="Zpět" onClick={() => editor.chain().focus().undo().run()}><Undo2 /></Tool><Tool title="Znovu" onClick={() => editor.chain().focus().redo().run()}><Redo2 /></Tool></div><EditorContent editor={editor} /></div>;
}

function Tool({ title, active, onClick, children }: { title: string; active?: boolean; onClick: () => void; children: React.ReactNode }) { return <button type="button" title={title} aria-label={title} className={active ? "active" : ""} onClick={onClick}>{children}</button>; }
