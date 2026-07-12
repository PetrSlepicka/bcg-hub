import type { PartnerType } from "./domain";

export type View = "orders" | "complaints" | "emails" | "customers" | "leads" | "suppliers" | "warehouses" | "carriers" | "customsDeclarants" | "collaborators" | "settings";
export interface AppRoute { view: View; entityId?: string }

const basePath = "/bcg-hub";
const entityRoutes: Partial<Record<View, string>> = { orders: "order", customers: "customer", leads: "lead", suppliers: "supplier", warehouses: "warehouse", carriers: "carrier", customsDeclarants: "customs-declarant", collaborators: "collaborator" };
const viewsByEntityRoute = Object.fromEntries(Object.entries(entityRoutes).map(([view, segment]) => [segment, view])) as Record<string, View>;
const knownViews = new Set<View>(["orders", "complaints", "emails", "customers", "leads", "suppliers", "warehouses", "carriers", "customsDeclarants", "collaborators", "settings"]);

export function readRoute(): AppRoute {
  const segments = window.location.pathname.replace(/^\/+|\/+$/g, "").split("/");
  const baseIndex = segments.indexOf("bcg-hub");
  const entityView = viewsByEntityRoute[segments[baseIndex + 1]];
  if (entityView && segments[baseIndex + 2]) return { view: entityView, entityId: decodeURIComponent(segments[baseIndex + 2]) };
  const query = new URLSearchParams(window.location.search);
  if (query.has("emailConnection")) return { view: "settings" };
  const queryView = query.get("view") as View | null;
  return { view: queryView && knownViews.has(queryView) ? queryView : "orders", entityId: query.get("entityId") ?? undefined };
}

export function routeHref(route: AppRoute) {
  const entitySegment = entityRoutes[route.view];
  if (route.entityId && entitySegment) return `${basePath}/${entitySegment}/${encodeURIComponent(route.entityId)}`;
  return route.view === "orders" ? `${basePath}/` : `${basePath}/?view=${encodeURIComponent(route.view)}`;
}

export function navigate(route: AppRoute, replace = false) {
  const href = routeHref(route);
  if (`${window.location.pathname}${window.location.search}` === href) return;
  window.history[replace ? "replaceState" : "pushState"]({}, "", href);
}

export const partnerView = (type: PartnerType): View => ({ Customer: "customers", Lead: "leads", Supplier: "suppliers", Warehouse: "warehouses", Carrier: "carriers", CustomsDeclarant: "customsDeclarants", Collaborator: "collaborators" })[type] as View;
