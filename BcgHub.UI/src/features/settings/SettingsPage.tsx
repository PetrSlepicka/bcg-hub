import { useState } from "react";
import { FileText, Mail, Users } from "lucide-react";
import type { CurrentUser } from "../../domain";
import { EmailSettingsPanel } from "../../EmailSettingsModal";
import { EmailTemplatesPanel } from "../emails/EmailTemplatesModal";
import { UsersPage } from "../users/UsersPage";

const superadminEmail = "petr.slepicka@radixal.net";
type SettingsTab = "email" | "templates" | "users";

export function SettingsPage({ user }: { user: CurrentUser }) {
  const isSuperadmin = user.email.toLowerCase() === superadminEmail;
  const [tab, setTab] = useState<SettingsTab>("email");
  return <section className="settings-page"><header><div><p className="eyebrow">NASTAVENÍ</p><h1>Nastavení</h1></div></header><nav className="settings-tabs"><button className={tab === "email" ? "active" : ""} onClick={() => setTab("email")}><Mail size={16} /> Nastavení e-mailů</button><button className={tab === "templates" ? "active" : ""} onClick={() => setTab("templates")}><FileText size={16} /> Nastavení šablon e-mailů</button>{isSuperadmin && <button className={tab === "users" ? "active" : ""} onClick={() => setTab("users")}><Users size={16} /> Nastavení uživatelů</button>}</nav><div className={`settings-content ${tab === "templates" ? "templates" : ""}`}>{tab === "email" && <div className="settings-card email-settings-modal"><EmailSettingsPanel /></div>}{tab === "templates" && <div className="settings-card template-modal"><EmailTemplatesPanel /></div>}{tab === "users" && isSuperadmin && <UsersPage />}</div></section>;
}
