import { useEffect, useMemo, useState } from "react";
import { Copy, Pencil, Plus, Search, Trash2, UserRound } from "lucide-react";
import { ApiError, api } from "../../api";
import type { CreatedManagedUser, ManagedUser, ManagedUserInput } from "../../domain";
import { SortableHeader } from "../../shared/SortableHeader";

export function UsersPage() {
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState<ManagedUser | null | undefined>(undefined);
  const [created, setCreated] = useState<CreatedManagedUser>();
  const [search, setSearch] = useState("");
  const [sortBy, setSortBy] = useState<"name" | "email" | "created" | "status">("name");
  const [descending, setDescending] = useState(false);

  const load = async (signal?: AbortSignal) => { setError(""); try { setUsers(await api.users.list(signal)); } catch (caught) { if ((caught as Error).name !== "AbortError") setError(messageOf(caught)); } finally { setLoading(false); } };
  useEffect(() => { const controller = new AbortController(); void load(controller.signal); return () => controller.abort(); }, []);

  const save = async (input: ManagedUserInput) => {
    setSaving(true); setError("");
    try {
      if (editing) await api.users.update(editing.id, { ...input, isActive: editing.isActive });
      else setCreated(await api.users.create(input));
      setEditing(undefined);
      await load();
    } catch (caught) { setError(messageOf(caught)); } finally { setSaving(false); }
  };

  const setActive = async (user: ManagedUser, isActive: boolean) => {
    setSaving(true); setError("");
    try { await api.users.update(user.id, { fullName: user.fullName, email: user.email, isActive }); await load(); } catch (caught) { setError(messageOf(caught)); } finally { setSaving(false); }
  };

  const deactivate = async (user: ManagedUser) => {
    if (!window.confirm(`Opravdu chcete deaktivovat uživatele ${user.fullName}?`)) return;
    setSaving(true); setError("");
    try { await api.users.deactivate(user.id); await load(); } catch (caught) { setError(messageOf(caught)); } finally { setSaving(false); }
  };

  const visibleUsers = useMemo(() => { const query = search.trim().toLocaleLowerCase("cs"); const filtered = query ? users.filter(user => [user.fullName, user.email, user.isActive ? "aktivní" : "neaktivní"].some(value => value.toLocaleLowerCase("cs").includes(query))) : users; const value = (user: ManagedUser) => ({ name: user.fullName, email: user.email, created: user.createdAtUtc, status: user.isActive ? "1" : "0" })[sortBy]; return [...filtered].sort((left, right) => value(left).localeCompare(value(right), "cs", { sensitivity: "base" }) * (descending ? -1 : 1)); }, [users, search, sortBy, descending]);
  const sort = (key: typeof sortBy) => { if (sortBy === key) setDescending(value => !value); else { setSortBy(key); setDescending(false); } };

  return <section className="users-page">
    <header className="users-header"><div><p className="eyebrow">ADMINISTRACE</p><h1>Správa uživatelů</h1><span>{users.filter(user => user.isActive).length} aktivních uživatelů</span></div><button className="primary" type="button" onClick={() => setEditing(null)}><Plus size={16} /> Přidat uživatele</button></header>
    {error && <div className="error-banner users-error">{error}</div>}
    <div className="toolbar users-toolbar"><label className="search"><Search size={16} /><input value={search} onChange={event => setSearch(event.target.value)} placeholder="Hledat podle jména, e-mailu nebo stavu…" /></label></div>
    <div className="users-table" aria-busy={loading || saving}>
      <table className="data-table users-data-table"><thead><tr><SortableHeader label="Uživatel" active={sortBy === "name"} descending={descending} onSort={() => sort("name")} /><SortableHeader label="E-mail" active={sortBy === "email"} descending={descending} onSort={() => sort("email")} /><SortableHeader label="Vytvořen" active={sortBy === "created"} descending={descending} onSort={() => sort("created")} /><SortableHeader label="Stav" active={sortBy === "status"} descending={descending} onSort={() => sort("status")} /><th className="actions">Akce</th></tr></thead><tbody>{visibleUsers.map(user => <tr className={user.isActive ? "" : "inactive"} key={user.id}>
        <td><div className="users-name"><span className="users-avatar"><UserRound size={16} /></span><div><b>{user.fullName}</b>{user.isCurrentUser && <small>Váš účet</small>}</div></div></td>
        <td className="users-email">{user.email}</td><td>{formatDate(user.createdAtUtc)}</td>
        <td><label className="users-toggle"><input type="checkbox" checked={user.isActive} disabled={saving || user.isCurrentUser} onChange={event => void setActive(user, event.target.checked)} /><span>{user.isActive ? "Aktivní" : "Neaktivní"}</span></label></td>
        <td className="users-actions actions"><button type="button" title="Upravit uživatele" disabled={saving} onClick={() => setEditing(user)}><Pencil size={15} /></button><button className="danger" type="button" title="Deaktivovat uživatele" disabled={saving || user.isCurrentUser || !user.isActive} onClick={() => void deactivate(user)}><Trash2 size={15} /></button></td>
      </tr>)}</tbody></table>
      {!visibleUsers.length && <div className="users-empty">{loading ? "Načítám uživatele…" : search ? "Žádní odpovídající uživatelé." : "Žádní uživatelé."}</div>}
    </div>
    {editing !== undefined && <UserModal user={editing} saving={saving} onClose={() => setEditing(undefined)} onSave={save} />}
    {created && <CredentialsModal created={created} onClose={() => setCreated(undefined)} />}
  </section>;
}

function UserModal({ user, saving, onClose, onSave }: { user: ManagedUser | null; saving: boolean; onClose: () => void; onSave: (input: ManagedUserInput) => Promise<void> }) {
  const [fullName, setFullName] = useState(user?.fullName ?? "");
  const [email, setEmail] = useState(user?.email ?? "");
  const [password, setPassword] = useState("");
  const valid = fullName.trim().length > 0 && email.includes("@") && (!password || password.length >= 12);
  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget && !saving) onClose(); }}><section className="user-modal" role="dialog" aria-modal="true"><header><div><p className="eyebrow">UŽIVATEL</p><h2>{user ? "Upravit uživatele" : "Přidat uživatele"}</h2></div><button type="button" onClick={onClose}>×</button></header><form onSubmit={event => { event.preventDefault(); void onSave({ fullName, email, password: password || undefined }); }}><label>Jméno<input value={fullName} onChange={event => setFullName(event.target.value)} autoFocus required maxLength={200} /></label><label>E-mail<input type="email" value={email} onChange={event => setEmail(event.target.value)} required maxLength={320} /></label><label>{user ? "Nové heslo (ponechte prázdné beze změny)" : "Heslo (ponechte prázdné pro automatické vytvoření)"}<input type="password" value={password} onChange={event => setPassword(event.target.value)} minLength={12} maxLength={200} autoComplete="new-password" /></label><footer><button className="secondary" type="button" onClick={onClose} disabled={saving}>Zrušit</button><button className="primary" type="submit" disabled={saving || !valid}>{saving ? "Ukládám…" : "Uložit"}</button></footer></form></section></div>;
}

function CredentialsModal({ created, onClose }: { created: CreatedManagedUser; onClose: () => void }) {
  const invitation = `Dobrý den,\n\nbyl vám vytvořen účet v BCG Hub.\n\nUživatelské jméno: ${created.user.email}\nDočasné heslo: ${created.temporaryPassword}\n\nPo přihlášení si heslo bezpečně uložte.`;
  const [copied, setCopied] = useState(false);
  const copy = async () => { await navigator.clipboard.writeText(invitation); setCopied(true); };
  return <div className="modal-backdrop"><section className="user-modal credentials-modal" role="dialog" aria-modal="true"><header><div><p className="eyebrow">ÚČET VYTVOŘEN</p><h2>Přihlašovací údaje</h2></div><button type="button" onClick={onClose}>×</button></header><div className="credentials-content"><p>Dočasné heslo se zobrazí pouze nyní. Zkopírujte pozvánku a předejte ji uživateli bezpečnou cestou.</p><textarea readOnly value={invitation} /><footer><button className="secondary" type="button" onClick={() => void copy()}><Copy size={15} /> {copied ? "Zkopírováno" : "Kopírovat pozvánku"}</button><button className="primary" type="button" onClick={onClose}>Hotovo</button></footer></div></section></div>;
}

function messageOf(error: unknown) { return error instanceof ApiError || error instanceof Error ? error.message : "Operace se nezdařila."; }
function formatDate(value: string) { return new Intl.DateTimeFormat("cs-CZ", { day: "2-digit", month: "2-digit", year: "numeric" }).format(new Date(value)); }
