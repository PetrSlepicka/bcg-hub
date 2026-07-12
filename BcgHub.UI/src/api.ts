import type { AttachmentItem, CommentItem, Communication, ComplaintDetail, ComplaintListItem, ContactPerson, CreatedManagedUser, CurrentUser, EmailActionContext, EmailMessage, EmailOrderOptions, EmailSettings, EmailTemplate, EmailTransportQuoteContext, ManagedUser, ManagedUserInput, OrderDetail, OrderListItem, PagedResult, PartnerDetail, PartnerListItem, PartnerType, PohodaImportPreview, PohodaImportResult, ResourceOwnerType, SaveComplaint, SaveEmailSettings, SendEmail, SaveOrder, SavePartner, TransportInquiryContext, TransportQuote, TransportType, WorkflowStep, WorkflowStepStatus } from "./domain";

const apiRoot = "https://dev.radixal.net/bcg-hub/api";
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
  if (init.body && !(init.body instanceof FormData)) headers.set("Content-Type", "application/json");
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
  users: {
    list: (signal?: AbortSignal) => request<ManagedUser[]>("/users", { signal }),
    create: (input: ManagedUserInput) => request<CreatedManagedUser>("/users", { method: "POST", body: JSON.stringify(input) }),
    update: (id: string, input: ManagedUserInput) => request<ManagedUser>(`/users/${id}`, { method: "PUT", body: JSON.stringify(input) }),
    deactivate: (id: string) => request<void>(`/users/${id}`, { method: "DELETE" })
  },
  orders: {
    list: (search: string, sortBy: string, descending: boolean, signal?: AbortSignal, page = 1, pageSize = 50, customerId?: string) => request<PagedResult<OrderListItem>>(`/orders?search=${encodeURIComponent(search)}&sortBy=${encodeURIComponent(sortBy)}&descending=${descending}&page=${page}&pageSize=${pageSize}${customerId ? `&customerId=${encodeURIComponent(customerId)}` : ""}`, { signal }),
    detail: (id: string, signal?: AbortSignal) => request<OrderDetail>(`/orders/${id}`, { signal }),
    create: (value: SaveOrder) => request<OrderDetail>("/orders", { method: "POST", body: JSON.stringify(value) }),
    update: (id: string, value: SaveOrder) => request<OrderDetail>(`/orders/${id}`, { method: "PUT", body: JSON.stringify(value) }),
    remove: (id: string, version: number) => request<void>(`/orders/${id}?version=${version}`, { method: "DELETE" }),
    previewPohoda: (file: File) => { const body = new FormData(); body.append("file", file); return request<PohodaImportPreview>("/orders/pohoda/preview", { method: "POST", body }); },
    importPohoda: (file: File) => { const body = new FormData(); body.append("file", file); return request<PohodaImportResult>("/orders/pohoda/import", { method: "POST", body }); },
    addQuote: (orderId: string, value: object) => request<TransportQuote>(`/orders/${orderId}/quotes`, { method: "POST", body: JSON.stringify(value) }),
    updateQuote: (orderId: string, quoteId: string, value: object) => request<TransportQuote>(`/orders/${orderId}/quotes/${quoteId}`, { method: "PUT", body: JSON.stringify(value) }),
    removeQuote: (orderId: string, quoteId: string, version: number) => request<void>(`/orders/${orderId}/quotes/${quoteId}?version=${version}`, { method: "DELETE" }),
    transportInquiry: (orderId: string, transportType: TransportType, signal?: AbortSignal) => request<TransportInquiryContext>(`/orders/${orderId}/transport-inquiry?transportType=${transportType}`, { signal }),
    sendTransportInquiry: (orderId: string, value: { transportType: TransportType; carrierIds: string[]; subject: string; bodyHtml: string }) => request<{ sentCount: number }>(`/orders/${orderId}/transport-inquiry`, { method: "POST", body: JSON.stringify(value) }),
    updateStep: (orderId: string, step: WorkflowStep, status: WorkflowStepStatus) => request<WorkflowStep>(`/orders/${orderId}/workflow/${step.id}`, { method: "PATCH", body: JSON.stringify({ status, notes: step.notes, version: step.version }) })
  },
  complaints: {
    list: () => request<ComplaintListItem[]>("/complaints"),
    detail: (id: string, signal?: AbortSignal) => request<ComplaintDetail>(`/complaints/${id}`, { signal }),
    create: (value: SaveComplaint) => request<ComplaintDetail>("/complaints", { method: "POST", body: JSON.stringify(value) }),
    update: (id: string, value: SaveComplaint) => request<ComplaintDetail>(`/complaints/${id}`, { method: "PUT", body: JSON.stringify(value) }),
    remove: (id: string, version: number) => request<void>(`/complaints/${id}?version=${version}`, { method: "DELETE" })
  },
  partners: {
    list: (type: PartnerType | undefined, search: string, signal?: AbortSignal, page = 1, pageSize = 50) => request<PagedResult<PartnerListItem>>(`/partners?${type ? `type=${type}&` : ""}search=${encodeURIComponent(search)}&page=${page}&pageSize=${pageSize}`, { signal }),
    detail: (id: string, signal?: AbortSignal) => request<PartnerDetail>(`/partners/${id}`, { signal }),
    create: (value: SavePartner) => request<PartnerDetail>("/partners", { method: "POST", body: JSON.stringify(value) }),
    update: (id: string, value: SavePartner) => request<PartnerDetail>(`/partners/${id}`, { method: "PUT", body: JSON.stringify(value) }),
    remove: (id: string, version: number) => request<void>(`/partners/${id}?version=${version}`, { method: "DELETE" }),
    addContact: (partnerId: string, value: object) => request<ContactPerson>(`/partners/${partnerId}/contacts`, { method: "POST", body: JSON.stringify(value) }),
    updateContact: (partnerId: string, id: string, value: object) => request<ContactPerson>(`/partners/${partnerId}/contacts/${id}`, { method: "PUT", body: JSON.stringify(value) }),
    removeContact: (partnerId: string, id: string, version: number) => request<void>(`/partners/${partnerId}/contacts/${id}?version=${version}`, { method: "DELETE" })
  },
  resources: {
    comments: (ownerType: ResourceOwnerType, ownerId: string) => request<CommentItem[]>(`/resources/${ownerType}/${ownerId}/comments`),
    addComment: (ownerType: ResourceOwnerType, ownerId: string, text: string) => request<CommentItem>(`/resources/${ownerType}/${ownerId}/comments`, { method: "POST", body: JSON.stringify({ text, version: 0 }) }),
    updateComment: (comment: CommentItem, text: string) => request<CommentItem>(`/resources/comments/${comment.id}`, { method: "PUT", body: JSON.stringify({ text, version: comment.version }) }),
    removeComment: (id: string, version: number) => request<void>(`/resources/comments/${id}?version=${version}`, { method: "DELETE" }),
    attachments: (ownerType: ResourceOwnerType, ownerId: string) => request<AttachmentItem[]>(`/resources/${ownerType}/${ownerId}/attachments`),
    upload: (ownerType: ResourceOwnerType, ownerId: string, file: File) => { const body = new FormData(); body.append("file", file); return request<AttachmentItem>(`/resources/${ownerType}/${ownerId}/attachments`, { method: "POST", body }); },
    downloadUrl: (id: string) => `${apiRoot}/resources/attachments/${id}/content`,
    removeAttachment: (id: string, version: number) => request<void>(`/resources/attachments/${id}?version=${version}`, { method: "DELETE" })
  },
  communications: {
    list: (partnerId?: string, orderId?: string) => request<PagedResult<Communication>>(`/communications?${partnerId ? `partnerId=${partnerId}` : `orderId=${orderId}`}&page=1&pageSize=50`),
    create: (value: object) => request<Communication>("/communications", { method: "POST", body: JSON.stringify(value) }),
    remove: (id: string, version: number) => request<void>(`/communications/${id}?version=${version}`, { method: "DELETE" })
  },
  emails: {
    list: (search: string, signal?: AbortSignal, page = 1, pageSize = 50) => request<PagedResult<EmailMessage>>(`/emails?q=${encodeURIComponent(search)}&page=${page}&pageSize=${pageSize}`, { signal }),
    detail: (id: string, signal?: AbortSignal) => request<EmailMessage>(`/emails/${id}`, { signal }),
    actionContext: (id: string, signal?: AbortSignal) => request<EmailActionContext>(`/emails/${id}/action-context`, { signal }),
    orderOptions: (id: string, signal?: AbortSignal) => request<EmailOrderOptions>(`/emails/${id}/order-options`, { signal }),
    transportQuoteContext: (id: string, signal?: AbortSignal) => request<EmailTransportQuoteContext>(`/emails/${id}/transport-quote`, { signal }),
    createTransportQuote: (id: string, value: object) => request<TransportQuote>(`/emails/${id}/transport-quote`, { method: "POST", body: JSON.stringify(value) }),
    sync: () => request<{ importedCount: number }>("/emails/sync", { method: "POST" }),
    link: (email: EmailMessage, businessPartnerId?: string, orderId?: string) => request<EmailMessage>(`/emails/${email.id}/link`, { method: "PUT", body: JSON.stringify({ businessPartnerId: businessPartnerId || null, orderId: orderId || null, version: email.version }) }),
    send: (value: SendEmail) => request<EmailMessage>("/emails/send", { method: "POST", body: JSON.stringify(value) })
  },
  emailTemplates: {
    list: () => request<EmailTemplate[]>("/email-templates"),
    create: (value: Omit<EmailTemplate, "id" | "version">) => request<EmailTemplate>("/email-templates", { method: "POST", body: JSON.stringify({ ...value, version: 0 }) }),
    update: (value: EmailTemplate) => request<EmailTemplate>(`/email-templates/${value.id}`, { method: "PUT", body: JSON.stringify(value) }),
    remove: (value: EmailTemplate) => request<void>(`/email-templates/${value.id}?version=${value.version}`, { method: "DELETE" })
  },
  settings: {
    getEmail: () => request<EmailSettings>("/settings/email"),
    saveEmail: (settings: SaveEmailSettings) => request<EmailSettings>("/settings/email", { method: "PUT", body: JSON.stringify(settings) }),
    setEmailProvider: (provider: "ImapSmtp" | "MicrosoftGraph") => request<EmailSettings>("/settings/email/provider", { method: "PUT", body: JSON.stringify({ provider }) }),
    connectMicrosoft: () => { const returnUrl = `${window.location.origin}${window.location.pathname}`; window.location.assign(`${apiRoot}/settings/email/microsoft/connect?returnUrl=${encodeURIComponent(returnUrl)}`); },
    disconnectMicrosoft: () => request<void>("/settings/email/microsoft", { method: "DELETE" })
  }
};
