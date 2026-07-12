import { ChevronDown, Search, X } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";

export interface SearchableOption { id: string; label: string; group?: string }

export function SearchableSelect({ value, options, placeholder, searchPlaceholder, inputRef, onChange, onSearchChange }: { value: string; options: SearchableOption[]; placeholder: string; searchPlaceholder: string; inputRef?: React.RefObject<HTMLInputElement | null>; onChange: (value: string) => void; onSearchChange?: (query: string) => void }) {
  const [open, setOpen] = useState(false); const [query, setQuery] = useState(""); const rootRef = useRef<HTMLDivElement>(null);
  const selected = options.find(option => option.id === value);
  const filtered = useMemo(() => { const normalized = query.trim().toLocaleLowerCase("cs"); return normalized ? options.filter(option => option.label.toLocaleLowerCase("cs").includes(normalized)) : options; }, [options, query]);
  useEffect(() => { const close = (event: MouseEvent) => { if (rootRef.current && !rootRef.current.contains(event.target as Node)) setOpen(false); }; document.addEventListener("mousedown", close); return () => document.removeEventListener("mousedown", close); }, []);
  useEffect(() => { onSearchChange?.(query); }, [query, onSearchChange]);
  const choose = (id: string) => { onChange(id); setQuery(""); setOpen(false); };
  const groups = [...new Set(filtered.map(option => option.group ?? ""))];
  return <div className="searchable-select" ref={rootRef}><div className="searchable-select-control"><Search size={14} /><input ref={inputRef} role="combobox" aria-expanded={open} aria-autocomplete="list" value={open ? query : selected?.label ?? ""} placeholder={open ? searchPlaceholder : placeholder} onFocus={() => { setOpen(true); setQuery(""); }} onChange={event => { if (!open) setOpen(true); setQuery(event.target.value); }} onKeyDown={event => { if (event.key === "Escape") setOpen(false); if (event.key === "Enter" && open && filtered[0]) { event.preventDefault(); choose(filtered[0].id); } }} />{value && <button type="button" className="searchable-clear" aria-label="Zrušit výběr" onMouseDown={event => event.preventDefault()} onClick={() => choose("")}><X size={13} /></button>}<ChevronDown size={14} /></div>{open && <div className="searchable-options" role="listbox"><button type="button" className={!value ? "selected" : ""} onClick={() => choose("")}>{placeholder}</button>{groups.map(group => <div key={group || "all"}>{group && <small>{group}</small>}{filtered.filter(option => (option.group ?? "") === group).map(option => <button type="button" role="option" aria-selected={option.id === value} className={option.id === value ? "selected" : ""} key={option.id} onClick={() => choose(option.id)}>{option.label}</button>)}</div>)}{filtered.length === 0 && <p>Žádné výsledky</p>}</div>}</div>;
}
