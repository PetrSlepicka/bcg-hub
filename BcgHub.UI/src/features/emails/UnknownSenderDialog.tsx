import { useState } from "react";
import type { PartnerType } from "../../domain";

const options: { type: PartnerType; label: string }[] = [{ type: "Customer", label: "Zákazník" }, { type: "Carrier", label: "Dopravce" }, { type: "Warehouse", label: "Sklad" }, { type: "Collaborator", label: "Spolupracující osoba" }, { type: "Supplier", label: "Dodavatel" }, { type: "Lead", label: "Lead" }];

export function UnknownSenderDialog({ address, onClose, onConfirm }: { address: string; onClose: () => void; onConfirm: (type: PartnerType) => void }) {
  const [type, setType] = useState<PartnerType>("Customer");
  return <div className="modal-backdrop"><section className="user-modal sender-type-modal"><header><div><p className="eyebrow">NEZNÁMÝ ODESÍLATEL</p><h2>Založit partnera</h2></div><button onClick={onClose} aria-label="Zavřít">×</button></header><form onSubmit={event => { event.preventDefault(); onConfirm(type); }}><p>Adresa <b>{address}</b> zatím není v evidenci. Vyberte, jaký typ záznamu chcete založit.</p><label><span>Typ partnera</span><select value={type} onChange={event => setType(event.target.value as PartnerType)}>{options.map(option => <option key={option.type} value={option.type}>{option.label}</option>)}</select></label><footer><button type="button" className="secondary" onClick={onClose}>Zrušit</button><button className="primary">Pokračovat</button></footer></form></section></div>;
}
