import type { OrderStatus, WorkflowStepStatus } from "../../domain";

export const orderStatusLabels: Record<OrderStatus, string> = { New: "Nová", InProgress: "Probíhá", Waiting: "Čeká", ReadyForPickup: "K vyzvednutí", InTransit: "Na cestě", Completed: "Dokončena", Cancelled: "Zrušena" };
export const workflowStatusLabels: Record<WorkflowStepStatus, string> = { Pending: "Čeká", InProgress: "Probíhá", Waiting: "Blokováno", Completed: "Hotovo", NotRequired: "Není třeba" };
export function formatMoney(value: number) { return new Intl.NumberFormat("cs-CZ", { style: "currency", currency: "CZK", maximumFractionDigits: 0 }).format(value); }
export function formatDate(value?: string) { return value ? new Intl.DateTimeFormat("cs-CZ").format(new Date(`${value}T00:00:00`)) : "—"; }
