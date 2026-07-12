using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class WorkflowCatalogTests
{
    [Fact]
    public void CatalogContainsAllUniqueStepsInBusinessOrder()
    {
        Assert.Equal(15, WorkflowCatalog.All.Length);
        Assert.Equal(15, WorkflowCatalog.All.Select(x => x.Type).Distinct().Count());
        Assert.Equal(Enumerable.Range(1, 15), WorkflowCatalog.All.Select(x => (int)x.Type));
    }

    [Fact]
    public void CreateStepsCreatesIndependentPendingSteps()
    {
        var firstOrderId = Guid.NewGuid();
        var secondOrderId = Guid.NewGuid();
        var first = WorkflowCatalog.CreateSteps(firstOrderId);
        var second = WorkflowCatalog.CreateSteps(secondOrderId);
        Assert.All(first, step => { Assert.Equal(firstOrderId, step.OrderId); Assert.Equal(WorkflowStepStatus.Pending, step.Status); });
        Assert.All(second, step => Assert.Equal(secondOrderId, step.OrderId));
        Assert.Empty(first.Select(x => x.Id).Intersect(second.Select(x => x.Id)));
    }
}
