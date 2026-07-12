import { useRef, useState } from "react";
import { CheckCircle2, FileUp, LoaderCircle, TriangleAlert, XCircle } from "lucide-react";
import { ApiError, api } from "../../api";
import type { PohodaImportPreview } from "../../domain";
import { formatDate, formatMoney } from "./formatters";

export function PohodaImportModal({ onClose, onImported }: { onClose: () => void; onImported: () => void }) {
  const input = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File>();
  const [preview, setPreview] = useState<PohodaImportPreview>();
  const [busy, setBusy] = useState(false);
  const [dragging, setDragging] = useState(false);
  const [error, setError] = useState<string>();

  const selectFile = async (selected?: File) => {
    if (!selected) return;
    if (!selected.name.toLowerCase().endsWith(".xml")) { setError("Vyberte XML soubor exportovaný z Pohody."); return; }
    setFile(selected); setPreview(undefined); setError(undefined); setBusy(true);
    try { setPreview(await api.orders.previewPohoda(selected)); }
    catch (caught) { setError(caught instanceof ApiError ? caught.message : "XML soubor se nepodařilo načíst."); }
    finally { setBusy(false); }
  };
  const importFile = async () => {
    if (!file || !preview?.newCount) return;
    setBusy(true); setError(undefined);
    try { await api.orders.importPohoda(file); onImported(); onClose(); }
    catch (caught) { setError(caught instanceof ApiError ? caught.message : "Objednávky se nepodařilo importovat."); }
    finally { setBusy(false); }
  };

  return <div className="modal-backdrop"><section className="pohoda-import-modal"><header><div><p className="eyebrow">POHODA XML</p><h2>Import objednávek</h2></div><button onClick={onClose} aria-label="Zavřít">×</button></header><div className="pohoda-import-body"><input ref={input} type="file" accept=".xml,text/xml,application/xml" hidden onChange={event => void selectFile(event.target.files?.[0])} /><button className={`pohoda-drop-zone ${dragging ? "dragging" : ""}`} onClick={() => input.current?.click()} onDragEnter={event => { event.preventDefault(); setDragging(true); }} onDragOver={event => event.preventDefault()} onDragLeave={() => setDragging(false)} onDrop={event => { event.preventDefault(); setDragging(false); void selectFile(event.dataTransfer.files[0]); }}><FileUp size={28} /><strong>{file?.name ?? "Přetáhněte sem XML soubor z Pohody"}</strong><span>{file ? "Kliknutím můžete vybrat jiný soubor" : "nebo klikněte a vyberte soubor"}</span></button>{busy && <div className="loader"><LoaderCircle className="spin" /> Zpracovávám XML…</div>}{error && <div className="error-banner">{error}</div>}{preview && <><div className="pohoda-import-summary"><span className="new"><CheckCircle2 /> {preview.newCount} nových</span><span className="duplicate"><TriangleAlert /> {preview.duplicateCount} již importovaných</span><span className="invalid"><XCircle /> {preview.errorCount} chyb</span></div><div className="pohoda-preview-table"><table><thead><tr><th>Objednávka</th><th>Zákazník</th><th>Datum</th><th>Hodnota</th><th>Výsledek</th></tr></thead><tbody>{preview.rows.map((row, index) => <tr key={`${row.externalId}-${index}`}><td><strong>{row.pohodaOrderNumber ?? row.externalId}</strong><small>{row.title}</small></td><td>{row.customerName}<small>{row.companyNumber ? `IČO ${row.companyNumber}` : "Bez IČO"}</small></td><td>{formatDate(row.orderedOn)}</td><td>{formatMoney(row.valueCzk)}</td><td><span className={`pohoda-row-status ${row.status.toLowerCase()}`}>{row.status === "New" ? "Nová" : row.status === "Duplicate" ? "Přeskočena" : "Chyba"}</span>{row.message && <small>{row.message}</small>}</td></tr>)}</tbody></table></div></>}</div><footer><button className="secondary" onClick={onClose}>Zrušit</button><button className="primary" disabled={busy || !preview?.newCount} onClick={() => void importFile()}>{busy ? "Importuji…" : `Importovat ${preview?.newCount ?? 0} zakázek`}</button></footer></section></div>;
}
