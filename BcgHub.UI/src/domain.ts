export type OrderStatus = "New" | "InProgress" | "Waiting" | "ReadyForPickup" | "InTransit" | "Completed" | "Cancelled";
export type WorkflowStepStatus = "Pending" | "InProgress" | "Waiting" | "Completed" | "NotRequired";
export type PartnerType = "Customer" | "Lead" | "Supplier" | "Warehouse" | "Carrier" | "CustomsDeclarant" | "Collaborator";

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
export interface TransportQuote { id: string; carrier: PartnerReference; price: number; currency: string; pickupOn?: string; deliveryOn?: string; isSelected: boolean; notes?: string }

export interface OrderDetail {
  id: string;
  number: string;
  pohodaOrderNumber?: string;
  title: string;
  status: OrderStatus;
  customer: PartnerReference;
  customerContact?: string;
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
}

export interface PartnerListItem { id: string; type: PartnerType; name: string; city?: string; countryCode?: string; email?: string; phone?: string; contactCount: number }
export interface PagedResult<T> { items: T[]; page: number; pageSize: number; totalCount: number }
export interface CurrentUser { id: string; email: string; fullName: string }
export interface EmailSettings { imapServer: string; imapPort: number; imapUseSsl: boolean; imapUsername: string; hasPassword: boolean; isActive: boolean }
export interface SaveEmailSettings { imapServer: string; imapPort: number; imapUseSsl: boolean; imapUsername: string; imapPassword: string; isActive: boolean }
export interface EmailMessage { id: string; direction: "Inbound" | "Outbound"; fromAddress: string; fromName?: string; toAddress: string; subject: string; bodyText?: string; bodyHtml?: string; occurredAtUtc: string; isRead: boolean; hasAttachments: boolean; businessPartnerId?: string; businessPartnerName?: string; orderId?: string; orderNumber?: string }
