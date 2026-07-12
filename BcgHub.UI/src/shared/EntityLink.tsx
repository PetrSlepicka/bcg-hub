import type { ReactNode } from "react";
import type { PartnerType } from "../domain";
import { partnerView, routeHref } from "../routing";

export function EntityLink({ children, href, title }: { children: ReactNode; href: string; title?: string }) {
  return <a className="entity-link" href={href} title={title}>{children}</a>;
}

export const orderHref = (id: string) => routeHref({ view: "orders", entityId: id });
export const partnerHref = (id: string, type: string) => routeHref({ view: partnerView(type as PartnerType), entityId: id });
