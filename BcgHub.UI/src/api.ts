import type { CurrentUser, EmailMessage, EmailSettings, OrderDetail, OrderListItem, PagedResult, PartnerListItem, PartnerType, SaveEmailSettings, WorkflowStep, WorkflowStepStatus } from "./domain";

const apiRoot = (import.meta.env.VITE_API_BASE ?? "/api").replace(/\/$/, "");
let csrfTokenPromise: Promise<string> | undefined;

export class ApiError extends Error {
  constructor(message: string, readonly status: number) { super(message); }
}

async function getCsrfToken(): Promise<string> {
  csrfTokenPromise ??= fetch(`${apiRoot}/auth/csrf`, { credentials: "include" }).then(async response => {
    if (!response.ok) throw new ApiError("Nepodařilo se získat bezpečnostní token.", response.status);
    return (await response.json() as { token: string }).token;
  }).catch(error => { csrfTokenPromise = undefined; throw error; });
  return csrfTokenPromise;
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const method = (init.method ?? "GET").toUpperCase();
  const headers = new Headers(init.headers);
  if (init.body) headers.set("Content-Type", "application/json");
  if (!["GET", "HEAD", "OPTIONS"].includes(method)) headers.set("X-CSRF-TOKEN", await getCsrfToken());
  const response = await fetch(`${apiRoot}${path}`, { ...init, headers, credentials: "include" });
  if (!response.ok)
  {
    const payload = await response.json().catch(() => undefined) as { message?: string } | undefined;
    throw new ApiError(payload?.message ?? `HTTP ${response.status}`, response.status);
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export const api = {
  auth: {
    session: (signal?: AbortSignal) => request<CurrentUser>("/auth/session", { signal }),
    login: async (email: string, password: string) => { const user = await request<CurrentUser>("/auth/login", { method: "POST", body: JSON.stringify({ email, password }) }); csrfTokenPromise = undefined; return user; },
    logout: async () => { await request<void>("/auth/logout", { method: "POST" }); csrfTokenPromise = undefined; }
  },
  orders: {
    list: (search: string, sortBy: string, descending: boolean, signal?: AbortSignal) => request<PagedResult<OrderListItem>>(`/orders?search=${encodeURIComponent(search)}&sortBy=${encodeURIComponent(sortBy)}&descending=${descending}&page=1&pageSize=50`, { signal }),
    detail: (id: string, signal?: AbortSignal) => request<OrderDetail>(`/orders/${id}`, { signal }),
    updateStep: (orderId: string, step: WorkflowStep, status: WorkflowStepStatus) => request<WorkflowStep>(`/orders/${orderId}/workflow/${step.id}`, { method: "PATCH", body: JSON.stringify({ status, notes: step.notes, version: step.version }) })
  },
  partners: {
    list: (type: PartnerType, search: string, signal?: AbortSignal) => request<PagedResult<PartnerListItem>>(`/partners?type=${type}&search=${encodeURIComponent(search)}&page=1&pageSize=50`, { signal })
  },
  emails: {
    list: (search: string, signal?: AbortSignal) => request<EmailMessage[]>(`/emails?q=${encodeURIComponent(search)}`, { signal }),
    detail: (id: string, signal?: AbortSignal) => request<EmailMessage>(`/emails/${id}`, { signal }),
    sync: () => request<{ importedCount: number }>("/emails/sync", { method: "POST" }),
    link: (id: string, businessPartnerId?: string, orderId?: string) => request<EmailMessage>(`/emails/${id}/link`, { method: "PUT", body: JSON.stringify({ businessPartnerId: businessPartnerId || null, orderId: orderId || null }) })
  },
  settings: {
    getEmail: () => request<EmailSettings>("/settings/email"),
    saveEmail: (settings: SaveEmailSettings) => request<EmailSettings>("/settings/email", { method: "PUT", body: JSON.stringify(settings) })
  }
};
