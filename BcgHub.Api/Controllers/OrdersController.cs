using BcgHub.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace BcgHub.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderQueryService queries, IOrderCommandService commands) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<OrderListItem>> GetList([FromQuery] string? search = null, [FromQuery] string sortBy = "number", [FromQuery] bool descending = true, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) => queries.GetListAsync(search, sortBy, descending, page, pageSize, cancellationToken);

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
}
