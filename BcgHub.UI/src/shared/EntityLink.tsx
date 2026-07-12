import type { ReactNode } from "react";

export function EntityLink({ children, href, title }: { children: ReactNode; href: string; title?: string }) {
  return <a className="entity-link" href={href} title={title}>{children}</a>;
}

export const orderHref = (id: string) => `?view=orders&entityId=${encodeURIComponent(id)}`;
export const partnerHref = (id: string, type: string) => `?view=${partnerView(type)}&entityId=${encodeURIComponent(id)}`;

function partnerView(type: string) { return ({ Customer: "customers", Lead: "leads", Supplier: "suppliers", Warehouse: "warehouses", Carrier: "carriers", CustomsDeclarant: "customsDeclarants", Collaborator: "collaborators" } as Record<string, string>)[type] ?? "customers"; }
