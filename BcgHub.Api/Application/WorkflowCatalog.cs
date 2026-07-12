using BcgHub.Api.Domain;
using System.Collections.Immutable;

namespace BcgHub.Api.Application;

public sealed record WorkflowDefinition(WorkflowStepType Type, string Title, string Description);

public static class WorkflowCatalog
{
    public static readonly ImmutableArray<WorkflowDefinition> All =
    [
        new(WorkflowStepType.OrderCreatedInPohoda, "Objednávka založena v Pohodě", "Pavel založil objednávku v účetním systému."),
        new(WorkflowStepType.InstructionsPrepared, "Pokyny připraveny", "Interní pokyny k zakázce a balení jsou kompletní."),
        new(WorkflowStepType.OrderIssuedFromPohoda, "Objednávka vydána z Pohody", "Finální objednávka je připravena k odeslání."),
        new(WorkflowStepType.SentToWarehouse, "Objednávka odeslána do skladu", "Sklad obdržel objednávku i pokyny."),
        new(WorkflowStepType.WarehouseReady, "Sklad potvrdil připravení", "Zboží je zabalené a připravené k vyzvednutí."),
        new(WorkflowStepType.TransportQuotesReceived, "Nabídky dopravy přijaty", "Byly získány a porovnány nabídky dopravců."),
        new(WorkflowStepType.TransportSelected, "Doprava objednána", "Vybraný dopravce obdržel adresy a potvrzení."),
        new(WorkflowStepType.InvoiceGenerated, "Faktura vygenerována", "Faktura byla vytvořena v Pohodě."),
        new(WorkflowStepType.GoodsIssuedFromPohoda, "Zboží vyskladněno v Pohodě", "Výdej zboží je zaevidován."),
        new(WorkflowStepType.PickupAnnouncedToWarehouse, "Nakládka oznámena skladu", "Sklad zná datum a čas příjezdu dopravce."),
        new(WorkflowStepType.ExportDocumentsPrepared, "Vývozní doklady zajištěny", "CMR, VDD nebo jiné požadované doklady jsou připraveny."),
        new(WorkflowStepType.DocumentsSentToWarehouse, "Doklady odeslány skladu", "Sklad obdržel potřebné dokumenty e-mailem."),
        new(WorkflowStepType.CustomerInformed, "Zákazník informován", "Zákazník obdržel termín doručení a fakturu."),
        new(WorkflowStepType.PickupConfirmed, "Vyzvednutí potvrzeno", "Sklad potvrdil převzetí zásilky dopravcem."),
        new(WorkflowStepType.ConfirmedExportDocumentsReceived, "Potvrzené doklady přijaty", "Potvrzené vývozní dokumenty se vrátily ze skladu.")
    ];

    public static List<OrderWorkflowStep> CreateSteps(Guid orderId) => All.Select(x => new OrderWorkflowStep { OrderId = orderId, Type = x.Type, Status = WorkflowStepStatus.Pending }).ToList();
}
