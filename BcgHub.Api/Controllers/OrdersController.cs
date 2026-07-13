using BcgHub.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace BcgHub.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderQueryService queries, IOrderCommandService commands, IPohodaOrderImportService pohodaImport, IPohodaSyncService pohodaSync, ITransportInquiryService transportInquiries) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<OrderListItem>> GetList([FromQuery] string? search = null, [FromQuery] string sortBy = "number", [FromQuery] bool descending = true, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] Guid? customerId = null, [FromQuery] OrderSalesChannel salesChannel = OrderSalesChannel.All, CancellationToken cancellationToken = default) => queries.GetListAsync(search, sortBy, descending, page, pageSize, customerId, salesChannel, cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDetailDto>> GetDetail(Guid id, CancellationToken cancellationToken) => await queries.GetDetailAsync(id, cancellationToken) is { } order ? Ok(order) : NotFound();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<OrderDetailDto>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await commands.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetDetail), new { id = order.Id }, order);
    }

    [HttpPut("{id:guid}"), ValidateAntiForgeryToken]
    public async Task<ActionResult<OrderDetailDto>> Update(Guid id, UpdateOrderRequest request, CancellationToken cancellationToken) => await commands.UpdateAsync(id, request, cancellationToken) is { } order ? Ok(order) : NotFound();

    [HttpDelete("{id:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] uint version, CancellationToken cancellationToken) => await commands.DeleteAsync(id, version, cancellationToken) ? NoContent() : NotFound();

    [HttpPatch("{orderId:guid}/workflow/{stepId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<WorkflowStepDto>> UpdateStep(Guid orderId, Guid stepId, UpdateWorkflowStepRequest request, CancellationToken cancellationToken) => await commands.UpdateStepAsync(orderId, stepId, request, cancellationToken) is { } step ? Ok(step) : NotFound();

    [HttpPost("{orderId:guid}/quotes"), ValidateAntiForgeryToken]
    public async Task<ActionResult<TransportQuoteDto>> AddQuote(Guid orderId, SaveTransportQuoteRequest request, CancellationToken cancellationToken) => await commands.AddQuoteAsync(orderId, request, cancellationToken) is { } quote ? Ok(quote) : NotFound();
    [HttpPut("{orderId:guid}/quotes/{quoteId:guid}"), ValidateAntiForgeryToken]
    public async Task<ActionResult<TransportQuoteDto>> UpdateQuote(Guid orderId, Guid quoteId, SaveTransportQuoteRequest request, CancellationToken cancellationToken) => await commands.UpdateQuoteAsync(orderId, quoteId, request, cancellationToken) is { } quote ? Ok(quote) : NotFound();
    [HttpDelete("{orderId:guid}/quotes/{quoteId:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuote(Guid orderId, Guid quoteId, [FromQuery] uint version, CancellationToken cancellationToken) => await commands.DeleteQuoteAsync(orderId, quoteId, version, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("{orderId:guid}/transport-inquiry")]
    public async Task<ActionResult<TransportInquiryContextDto>> GetTransportInquiry(Guid orderId, [FromQuery] string transportType, CancellationToken cancellationToken) => await transportInquiries.GetContextAsync(orderId, transportType, cancellationToken) is { } context ? Ok(context) : NotFound();

    [HttpPost("{orderId:guid}/transport-inquiry"), ValidateAntiForgeryToken]
    public async Task<ActionResult<SendTransportInquiryResult>> SendTransportInquiry(Guid orderId, SendTransportInquiryRequest request, CancellationToken cancellationToken) => await transportInquiries.SendAsync(orderId, request, cancellationToken) is { } result ? Ok(result) : NotFound();

    [HttpPost("pohoda/preview"), ValidateAntiForgeryToken, RequestSizeLimit(200 * 1024 * 1024)]
    public async Task<ActionResult<PohodaImportPreview>> PreviewPohoda(IFormFile file, CancellationToken cancellationToken)
    {
        ValidatePohodaFile(file);
        await using var stream = file.OpenReadStream();
        return Ok(await pohodaImport.PreviewAsync(stream, cancellationToken));
    }

    [HttpPost("pohoda/import"), ValidateAntiForgeryToken, RequestSizeLimit(200 * 1024 * 1024)]
    public async Task<ActionResult<PohodaImportResult>> ImportPohoda(IFormFile file, CancellationToken cancellationToken)
    {
        ValidatePohodaFile(file);
        await using var stream = file.OpenReadStream();
        return Ok(await pohodaImport.ImportAsync(stream, cancellationToken));
    }

    [HttpGet("pohoda/sync/status")]
    public async Task<ActionResult<PohodaSyncStatus>> GetPohodaSyncStatus(CancellationToken cancellationToken) => Ok(await pohodaSync.GetStatusAsync(cancellationToken));

    [HttpPost("pohoda/sync"), ValidateAntiForgeryToken]
    public async Task<ActionResult<PohodaSyncResult>> SyncPohoda(CancellationToken cancellationToken) => Ok(await pohodaSync.SyncAsync("manual", cancellationToken));

    private static void ValidatePohodaFile(IFormFile file)
    {
        if (file.Length == 0) throw new DomainValidationException("Vyberte neprázdný XML soubor.");
        if (file.Length > 200 * 1024 * 1024) throw new DomainValidationException("XML soubor může mít nejvýše 200 MB.");
        if (!string.Equals(Path.GetExtension(file.FileName), ".xml", StringComparison.OrdinalIgnoreCase)) throw new DomainValidationException("Vyberte soubor ve formátu XML.");
    }
}
