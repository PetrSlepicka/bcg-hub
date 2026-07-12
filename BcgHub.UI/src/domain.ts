export type OrderStatus = "New" | "InProgress" | "Waiting" | "ReadyForPickup" | "InTransit" | "Completed" | "Cancelled";
export type WorkflowStepStatus = "Pending" | "InProgress" | "Waiting" | "Completed" | "NotRequired";
export type PartnerType = "Customer" | "Lead" | "Supplier" | "Warehouse" | "Carrier" | "CustomsDeclarant" | "Collaborator";
export type ComplaintStatus = "New" | "InProgress" | "Resolved" | "Rejected";

export interface OrderListItem {
  id: string;
  number: string;
  title: string;
  customerName: string;
  status: OrderStatus;
  plannedDeliveryOn?: string;
  valueCzk: number;
  weightKg: number;
  completedSteps: number;
  totalSteps: number;
}

export interface PartnerReference { id: string; name: string }
export interface WorkflowStep { id: string; type: string; title: string; description: string; status: WorkflowStepStatus; dueAtUtc?: string; completedAtUtc?: string; notes?: string; version: number }
export interface TransportQuote { id: string; carrier: PartnerReference; price: number; currency: string; pickupOn?: string; deliveryOn?: string; isSelected: boolean; notes?: string; version: number }

export interface OrderDetail {
  id: string;
  number: string;
  pohodaOrderNumber?: string;
  title: string;
  status: OrderStatus;
  customer: PartnerReference;
  customerContact?: string;
  customerContactId?: string;
  warehouse?: PartnerReference;
  carrier?: PartnerReference;
  customsDeclarant?: PartnerReference;
  orderedOn?: string;
  requestedDeliveryOn?: string;
  plannedPickupOn?: string;
  plannedDeliveryOn?: string;
  valueCzk: number;
  weightKg: number;
  volumeM3: number;
  warehouseInstructions?: string;
  workflowSteps: WorkflowStep[];
  transportQuotes: TransportQuote[];
  version: number;
}

export interface PartnerListItem { id: string; type: PartnerType; name: string; city?: string; countryCode?: string; email?: string; phone?: string; contactCount: number }
export interface ContactPerson { id: string; fullName: string; position?: string; email?: string; phone?: string; isPrimary: boolean; version: number }
export interface PartnerDetail { id: string; type: PartnerType; name: string; companyNumber?: string; vatNumber?: string; email?: string; phone?: string; website?: string; street?: string; city?: string; postalCode?: string; countryCode?: string; notes?: string; transportCapabilities?: string; contacts: ContactPerson[]; version: number }
export type SavePartner = Omit<PartnerDetail, "id" | "contacts">;
export type SaveOrder = Omit<OrderDetail, "id" | "customer" | "customerContact" | "warehouse" | "carrier" | "customsDeclarant" | "workflowSteps" | "transportQuotes"> & { customerId: string; warehouseId?: string; carrierId?: string; customsDeclarantId?: string };
export type ResourceOwnerType = "BusinessPartner" | "ContactPerson" | "Order" | "WorkflowStep" | "TransportQuote" | "Communication" | "EmailMessage" | "Complaint";
export interface ComplaintListItem { id: string; reportedOn: string; status: ComplaintStatus; customer: PartnerReference; orderId: string; orderNumber: string; description?: string }
export interface ComplaintDetail extends ComplaintListItem { version: number }
export interface SaveComplaint { reportedOn: string; status: ComplaintStatus; customerId: string; orderId: string; description?: string; version: number }
export interface CommentItem { id: string; authorName: string; text: string; createdAtUtc: string; version: number }
export interface AttachmentItem { id: string; fileName: string; contentType: string; size: number; createdAtUtc: string; version: number }
export interface Communication { id: string; type: "Email" | "Phone" | "Meeting" | "Note"; businessPartnerId?: string; orderId?: string; subject: string; bodyPreview?: string; sender?: string; recipients?: string; occurredAtUtc: string; version: number }
export interface PagedResult<T> { items: T[]; page: number; pageSize: number; totalCount: number }
export interface CurrentUser { id: string; email: string; fullName: string }
export interface ManagedUser { id: string; fullName: string; email: string; isActive: boolean; createdAtUtc: string; updatedAtUtc: string; isCurrentUser: boolean }
export interface CreatedManagedUser { user: ManagedUser; temporaryPassword: string }
export interface ManagedUserInput { fullName: string; email: string; isActive?: boolean; password?: string }
export interface EmailSettings { imapServer: string; imapPort: number; imapUseSsl: boolean; imapUsername: string; hasImapPassword: boolean; smtpServer: string; smtpPort: number; smtpUseSsl: boolean; smtpUsername: string; hasSmtpPassword: boolean; senderAddress: string; senderName?: string; isActive: boolean }
export interface SaveEmailSettings { imapServer: string; imapPort: number; imapUseSsl: boolean; imapUsername: string; imapPassword: string; smtpServer: string; smtpPort: number; smtpUseSsl: boolean; smtpUsername: string; smtpPassword: string; senderAddress: string; senderName?: string; isActive: boolean }
export interface EmailMessage { id: string; direction: "Inbound" | "Outbound"; fromAddress: string; fromName?: string; toAddress: string; subject: string; bodyText?: string; bodyHtml?: string; occurredAtUtc: string; isRead: boolean; hasAttachments: boolean; businessPartnerId?: string; businessPartnerName?: string; orderId?: string; orderNumber?: string; version: number }
export interface EmailTransportQuoteContext { carrier: PartnerReference; suggestedOrderId?: string; orders: EmailOrderOption[] }
export interface EmailOrderOption { id: string; number: string; title: string; customerName: string }
export interface EmailOrderOptions { suggested: EmailOrderOption[]; other: EmailOrderOption[] }
export type EmailSenderType = "Carrier" | "Warehouse" | "Collaborator" | "Customer" | "Partner" | "Unknown";
export interface EmailActionContext { senderType: EmailSenderType; matchedBy: "Address" | "Domain" | "None"; partner?: PartnerReference }
export interface EmailTemplate { id: string; name: string; subject: string; bodyHtml: string; version: number }
export interface SendEmail { toAddress: string; ccAddress?: string; subject: string; bodyHtml: string; replyToEmailId?: string; businessPartnerId?: string; orderId?: string }
