import { useEffect, useRef, useState } from "react";
import { Boxes, Building2, ChevronUp, Factory, Handshake, LogOut, Mail, PackageCheck, Settings, Sparkles, Truck } from "lucide-react";
import { ApiError, api } from "./api";
import type { CurrentUser, PartnerType } from "./domain";
import { OrdersWorkspace } from "./features/orders/OrdersWorkspace";
import { PartnerWorkspace } from "./features/partners/PartnerWorkspace";
import { Login } from "./Login";
import { EmailSettingsModal } from "./EmailSettingsModal";
import { EmailsWorkspace } from "./features/emails/EmailsWorkspace";

type View = "orders" | "emails" | "customers" | "leads" | "suppliers" | "warehouses" | "carriers" | "collaborators";
const items = [{ id: "orders", label: "Zakázky", icon: PackageCheck }, { id: "emails", label: "E-maily", icon: Mail }, { id: "customers", label: "Zákazníci", icon: Building2 }, { id: "leads", label: "Leady", icon: Sparkles }, { id: "suppliers", label: "Dodavatelé", icon: Factory }, { id: "warehouses", label: "Sklady", icon: Boxes }, { id: "carriers", label: "Dopravci", icon: Truck }, { id: "collaborators", label: "Spolupracovníci", icon: Handshake }] as const;
const partnerViews: Partial<Record<View, { type: PartnerType; title: string }>> = { customers: { type: "Customer", title: "Zákazníci" }, leads: { type: "Lead", title: "Leady" }, suppliers: { type: "Supplier", title: "Dodavatelé" }, warehouses: { type: "Warehouse", title: "Sklady" }, carriers: { type: "Carrier", title: "Dopravci" }, collaborators: { type: "Collaborator", title: "Spolupracující osoby" } };

export function App() {
  const [user, setUser] = useState<CurrentUser>();
  const [sessionChecked, setSessionChecked] = useState(false);
  const [view, setView] = useState<View>("orders");
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [settingsOpen, setSettingsOpen] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);
  useEffect(() => { const controller = new AbortController(); api.auth.session(controller.signal).then(setUser).catch(error => { if (!(error instanceof ApiError && error.status === 401) && error?.name !== "AbortError") console.error(error); }).finally(() => setSessionChecked(true)); return () => controller.abort(); }, []);
  useEffect(() => { const close = (event: MouseEvent) => { if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) setUserMenuOpen(false); }; document.addEventListener("mousedown", close); return () => document.removeEventListener("mousedown", close); }, []);
  if (!sessionChecked) return <div className="session-loader">Načítám přihlášení…</div>;
  if (!user) return <Login onLogin={setUser} />;
  const partner = partnerViews[view];
  const logout = async () => { await api.auth.logout(); setUser(undefined); };
  const initials = user.fullName.split(" ").filter(Boolean).map(x => x[0]).join("").toUpperCase().slice(0, 2) || "?";
  return <div className="app-shell"><aside className="rail"><div className="brand"><span>BCG</span><small>HUB</small></div><nav>{items.map(item => <button key={item.id} className={view === item.id ? "active" : ""} onClick={() => setView(item.id)} title={item.label} aria-label={item.label}><item.icon size={21} /></button>)}</nav><div className="rail-actions" ref={userMenuRef}><button className="rail-user" onClick={() => setUserMenuOpen(x => !x)} title={user.fullName} aria-label="Uživatelské menu"><span>{initials}</span><ChevronUp size={12} className={userMenuOpen ? "rotated" : ""} /></button>{userMenuOpen && <div className="user-menu"><div className="user-menu-head"><b>{user.fullName}</b><small>{user.email}</small></div><button onClick={() => { setSettingsOpen(true); setUserMenuOpen(false); }}><Settings size={16} /> Nastavení</button><button className="danger" onClick={logout}><LogOut size={16} /> Odhlásit</button></div>}</div></aside><main className="workspace">{view === "orders" && <OrdersWorkspace />}{partner && <PartnerWorkspace type={partner.type} title={partner.title} />}{view === "emails" && <EmailsWorkspace onOpenSettings={() => setSettingsOpen(true)} />}</main>{settingsOpen && <EmailSettingsModal onClose={() => setSettingsOpen(false)} />}</div>;
}
