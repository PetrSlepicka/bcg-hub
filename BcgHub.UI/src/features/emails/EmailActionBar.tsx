import { useState } from "react";
import { Building2, Link2, LoaderCircle, PackagePlus, Truck } from "lucide-react";
import type { EmailActionContext, EmailMessage, PartnerType } from "../../domain";
import { UnknownSenderDialog } from "./UnknownSenderDialog";

const labels: Record<EmailActionContext["senderType"], string> = { Carrier: "Dopravce", Warehouse: "Sklad", Collaborator: "Spolupracující osoba", Customer: "Zákazník", Partner: "Obchodní partner", Unknown: "Neznámý odesílatel" };

export function EmailActionBar({ email, context, transportAvailable, onAssign, onTransportQuote, onCreateOrder, onCreatePartner }: { email: EmailMessage; context?: EmailActionContext; transportAvailable: boolean; onAssign: () => void; onTransportQuote: () => void; onCreateOrder: (customerId?: string) => void; onCreatePartner: (type: PartnerType) => void }) {
  const [senderDialogOpen, setSenderDialogOpen] = useState(false);
  if (email.direction !== "Inbound") return null;
  return <div className={`email-context-bar sender-${(context?.senderType ?? "loading").toLowerCase()}`}>
    <div className="email-context-identity">{context ? <><span className="sender-badge">{context.senderType === "Carrier" ? <Truck size={15} /> : <Building2 size={15} />}{labels[context.senderType]}</span><span><b>{context.partner?.name ?? email.fromName ?? email.fromAddress}</b><small>{context.matchedBy === "Domain" ? "Rozpoznáno podle domény" : context.matchedBy === "Address" ? "Rozpoznáno podle adresy" : context.matchedBy === "Manual" ? "Ručně přiřazený partner" : context.matchedBy === "Ambiguous" ? "Doména odpovídá více záznamům – vyberte partnera ručně" : "Adresa zatím není v evidenci"}</small></span></> : <><LoaderCircle className="spin" size={16} /><span><b>Rozpoznávám odesílatele…</b></span></>}</div>
    {context && <div className="email-context-actions">{context.senderType === "Carrier" && <button className="primary" disabled={!transportAvailable} title={transportAvailable ? undefined : "Načítám zakázky pro nabídku dopravy"} onClick={onTransportQuote}><Truck size={15} /> Přiřadit jako nabídku dopravy</button>}<button className="secondary" onClick={onAssign}><Link2 size={15} /> Přiřadit k zakázce</button>{context.senderType === "Customer" && <button className="secondary" onClick={() => onCreateOrder(context.partner?.id)}><PackagePlus size={15} /> Založit zakázku</button>}{context.senderType === "Unknown" && context.matchedBy !== "Ambiguous" && <button className="secondary" onClick={() => setSenderDialogOpen(true)}><Building2 size={15} /> Založit partnera</button>}</div>}
    {senderDialogOpen && <UnknownSenderDialog address={email.fromAddress} onClose={() => setSenderDialogOpen(false)} onConfirm={type => { setSenderDialogOpen(false); onCreatePartner(type); }} />}
  </div>;
}
