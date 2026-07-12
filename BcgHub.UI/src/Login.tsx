import { FormEvent, useState } from "react";
import { LockKeyhole } from "lucide-react";
import { api } from "./api";
import type { CurrentUser } from "./domain";

export function Login({ onLogin }: { onLogin: (user: CurrentUser) => void }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string>();
  const [submitting, setSubmitting] = useState(false);
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    try { onLogin(await api.auth.login(email, password)); }
    catch (caught) { setError(caught instanceof Error ? caught.message : "Přihlášení se nezdařilo."); }
    finally { setSubmitting(false); }
  };
  return <main className="login-page"><form className="login-card" onSubmit={submit}><div className="login-mark"><LockKeyhole size={24} /></div><p className="eyebrow">BOHEMI CRYSTALL GLASS</p><h1>BCG HUB</h1><p className="muted">Přihlaste se do interního systému.</p><label>E-mail<input type="email" autoComplete="username" value={email} onChange={event => setEmail(event.target.value)} required /></label><label>Heslo<input type="password" autoComplete="current-password" value={password} onChange={event => setPassword(event.target.value)} minLength={8} required /></label>{error && <div className="error-banner">{error}</div>}<button className="primary" disabled={submitting}>{submitting ? "Přihlašuji…" : "Přihlásit"}</button></form></main>;
}
