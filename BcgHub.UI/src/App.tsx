import { Fragment, lazy, Suspense, useEffect, useRef, useState } from "react";
import { AlertTriangle, Boxes, ChevronUp, Factory, Handshake, LogOut, Mail, PackageCheck, Settings, Sparkles, Truck, UserRound } from "lucide-react";
import { ApiError, api, unauthorizedEvent } from "./api";
import type { CurrentUser, EmailMessage, PartnerType, SavePartner } from "./domain";
import { Login } from "./Login";
import { navigate, partnerView, readRoute, type AppRoute, type View } from "./routing";

const OrdersWorkspace = lazy(() => import("./features/orders/OrdersWorkspace").then(module => ({ default: module.OrdersWorkspace })));
const PartnerWorkspace = lazy(() => import("./features/partners/PartnerWorkspace").then(module => ({ default: module.PartnerWorkspace })));
const EmailsWorkspace = lazy(() => import("./features/emails/EmailsWorkspace").then(module => ({ default: module.EmailsWorkspace })));
const ComplaintsWorkspace = lazy(() => import("./features/complaints/ComplaintsWorkspace").then(module => ({ default: module.ComplaintsWorkspace })));
const SettingsPage = lazy(() => import("./features/settings/SettingsPage").then(module => ({ default: module.SettingsPage })));

const itemGroups = [
  [{ id: "emails", label: "E-maily", icon: Mail }, { id: "orders", label: "Zakázky", icon: PackageCheck }, { id: "customers", label: "Zákazníci", icon: UserRound }, { id: "leads", label: "Leady", icon: Sparkles }],
  [{ id: "warehouses", label: "Sklady", icon: Boxes }, { id: "collaborators", label: "Spolupracovníci", icon: Handshake }, { id: "suppliers", label: "Dodavatelé", icon: Factory }, { id: "carriers", label: "Dopravci", icon: Truck }],
  [{ id: "complaints", label: "Reklamace", icon: AlertTriangle }]
] as const;
const partnerViews: Partial<Record<View, { type: PartnerType; title: string }>> = { customers: { type: "Customer", title: "Zákazníci" }, leads: { type: "Lead", title: "Leady" }, suppliers: { type: "Supplier", title: "Dodavatelé" }, warehouses: { type: "Warehouse", title: "Sklady" }, carriers: { type: "Carrier", title: "Dopravci" }, customsDeclarants: { type: "CustomsDeclarant", title: "Celní deklaranti" }, collaborators: { type: "Collaborator", title: "Spolupracující osoby" } };

export function App() {
  const [user, setUser] = useState<CurrentUser>();
  const [sessionChecked, setSessionChecked] = useState(false);
  const [route, setRoute] = useState<AppRoute>(readRoute);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [newOrderRequest, setNewOrderRequest] = useState<{ customerId: string; email: EmailMessage; handled: boolean }>();
  const [newPartnerRequest, setNewPartnerRequest] = useState<{ draft: SavePartner; email: EmailMessage; handled: boolean }>();
  const userMenuRef = useRef<HTMLDivElement>(null);
  useEffect(() => { const controller = new AbortController(); api.auth.session(controller.signal).then(setUser).catch(error => { if (!(error instanceof ApiError && error.status === 401) && error?.name !== "AbortError") console.error(error); }).finally(() => setSessionChecked(true)); return () => controller.abort(); }, []);
  useEffect(() => { const clearSession = () => setUser(undefined); window.addEventListener(unauthorizedEvent, clearSession); return () => window.removeEventListener(unauthorizedEvent, clearSession); }, []);
  useEffect(() => { const close = (event: MouseEvent) => { if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) setUserMenuOpen(false); }; document.addEventListener("mousedown", close); return () => document.removeEventListener("mousedown", close); }, []);
  useEffect(() => { const onPopState = () => setRoute(readRoute()); window.addEventListener("popstate", onPopState); return () => window.removeEventListener("popstate", onPopState); }, []);
  if (!sessionChecked) return <div className="session-loader">Načítám přihlášení…</div>;
  if (!user) return <Login onLogin={setUser} />;
  const view = route.view;
  const selectRoute = (next: AppRoute) => { navigate(next); setRoute(next); };
  const selectView = (nextView: View) => selectRoute({ view: nextView });
  const selectEntity = (entityId?: string) => { if (!entityId || entityId === route.entityId) return; const next = { view, entityId }; navigate(next); setRoute(next); };
  const partner = partnerViews[view];
  const logout = async () => { await api.auth.logout(); setUser(undefined); };
  const initials = user.fullName.split(" ").filter(Boolean).map(x => x[0]).join("").toUpperCase().slice(0, 2) || "?";
  return <div className="app-shell"><aside className="rail"><div className="brand"><span>BCG</span><small>HUB</small></div><nav>{itemGroups.map((items, groupIndex) => <Fragment key={items[0].id}>{groupIndex > 0 && <div className="rail-separator" />}{items.map(item => <button key={item.id} className={view === item.id ? "active" : ""} onClick={() => selectView(item.id)} title={item.label} aria-label={item.label}><item.icon size={21} /></button>)}</Fragment>)}</nav><div className="rail-actions" ref={userMenuRef}><button className="rail-user" onClick={() => setUserMenuOpen(x => !x)} title={user.fullName} aria-label="Uživatelské menu"><span>{initials}</span><ChevronUp size={12} className={userMenuOpen ? "rotated" : ""} /></button>{userMenuOpen && <div className="user-menu"><div className="user-menu-head"><b>{user.fullName}</b><small>{user.email}</small></div><button onClick={() => { selectView("settings"); setUserMenuOpen(false); }}><Settings size={16} /> Nastavení</button><button className="danger" onClick={logout}><LogOut size={16} /> Odhlásit</button></div>}</div></aside><main className="workspace"><Suspense fallback={<div className="session-loader">Načítám modul…</div>}>{view === "orders" && <OrdersWorkspace requestedEntityId={route.entityId} onSelectedEntityIdChange={selectEntity} initialCustomerId={newOrderRequest && !newOrderRequest.handled ? newOrderRequest.customerId : undefined} onInitialCreateHandled={() => setNewOrderRequest(current => current ? { ...current, handled: true } : undefined)} onInitialCreateCancelled={() => setNewOrderRequest(undefined)} onInitialOrderCreated={newOrderRequest ? async order => { await api.emails.link(newOrderRequest.email, newOrderRequest.email.businessPartnerId ?? newOrderRequest.customerId, order.id); setNewOrderRequest(undefined); } : undefined} />}{view === "complaints" && <ComplaintsWorkspace />}{partner && <PartnerWorkspace type={partner.type} title={partner.title} requestedEntityId={route.entityId} onSelectedEntityIdChange={selectEntity} initialDraft={newPartnerRequest && !newPartnerRequest.handled && newPartnerRequest.draft.type === partner.type ? newPartnerRequest.draft : undefined} onInitialDraftHandled={() => setNewPartnerRequest(current => current ? { ...current, handled: true } : undefined)} onInitialDraftCancelled={() => setNewPartnerRequest(undefined)} onInitialDraftSaved={newPartnerRequest ? async saved => { await api.emails.link(newPartnerRequest.email, saved.id, newPartnerRequest.email.orderId); setNewPartnerRequest(undefined); } : undefined} />}{view === "emails" && <EmailsWorkspace onOpenSettings={() => selectView("settings")} onCreateOrder={(customerId, email) => { setNewOrderRequest({ customerId: customerId ?? "", email, handled: false }); selectView("orders"); }} onCreatePartner={(type, email) => { const targetView = partnerView(type); setNewPartnerRequest({ draft: { type, name: email.fromName || email.fromAddress.split("@")[0], email: email.fromAddress, version: 0 }, email, handled: false }); selectView(targetView); }} />}{view === "settings" && <SettingsPage user={user} />}</Suspense></main></div>;
}
