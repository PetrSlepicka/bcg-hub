import { useCallback, useEffect, useRef, useState } from "react";
import { ApiError, api } from "../../api";
import type { EmailMessage } from "../../domain";

export function useEmailsWorkspace(onOpenSettings: () => void) {
  const [emails, setEmails] = useState<EmailMessage[]>([]);
  const [selectedId, setSelectedId] = useState<string>();
  const [detail, setDetail] = useState<EmailMessage>();
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState(false);
  const [error, setError] = useState<string>();
  const searchRef = useRef(search);
  const listRequestVersion = useRef(0);
  useEffect(() => { searchRef.current = search; }, [search]);

  const applyList = useCallback((items: EmailMessage[]) => { setEmails(items); setSelectedId(current => current && items.some(email => email.id === current) ? current : items[0]?.id); }, []);
  useEffect(() => {
    const controller = new AbortController();
    const requestVersion = ++listRequestVersion.current;
    const timeout = window.setTimeout(() => { setLoading(true); api.emails.list(search, controller.signal).then(result => { if (requestVersion === listRequestVersion.current) { applyList(result.items); setError(undefined); } }).catch(caught => { if (caught?.name !== "AbortError") setError("E-maily se nepodařilo načíst."); }).finally(() => { if (!controller.signal.aborted && requestVersion === listRequestVersion.current) setLoading(false); }); }, 180);
    return () => { window.clearTimeout(timeout); controller.abort(); };
  }, [search, applyList]);

  useEffect(() => {
    if (!selectedId) { setDetail(undefined); return; }
    const controller = new AbortController();
    api.emails.detail(selectedId, controller.signal).then(next => { setDetail(next); setEmails(current => current.map(email => email.id === next.id ? next : email)); }).catch(caught => { if (caught?.name !== "AbortError") setError("Detail e-mailu se nepodařilo načíst."); });
    return () => controller.abort();
  }, [selectedId]);

  const sync = async () => {
    setSyncing(true);
    setError(undefined);
    try { await api.emails.sync(); const requestVersion = ++listRequestVersion.current; const result = await api.emails.list(searchRef.current); if (requestVersion === listRequestVersion.current) applyList(result.items); }
    catch (caught) { if (caught instanceof ApiError && (caught.status === 400 || caught.status === 404)) { setError(caught.message); onOpenSettings(); } else setError("Synchronizace e-mailů se nepodařila."); }
    finally { setSyncing(false); }
  };
  const updateEmail = (next: EmailMessage) => { setDetail(current => current?.id === next.id ? next : current); setEmails(current => current.map(email => email.id === next.id ? next : email)); };
  const addEmail = (next: EmailMessage) => { setEmails(current => [next, ...current]); setSelectedId(next.id); setDetail(next); };
  return { emails, selectedId, detail, search, loading, syncing, error, setSelectedId, setSearch, sync, updateEmail, addEmail };
}
