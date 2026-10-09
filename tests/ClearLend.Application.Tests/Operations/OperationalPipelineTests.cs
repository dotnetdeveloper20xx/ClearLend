using ClearLend.Application;
using ClearLend.Application.Abstractions;
using ClearLend.Application.Operations.Staff;
using ClearLend.Application.Operations.WorkItems;
using ClearLend.Domain.Common;
using ClearLend.Domain.Identity;
using ClearLend.Domain.Operations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ClearLend.Application.Tests.Operations;

public sealed class OperationalPipelineTests
{
    [Fact]
    public void ApplicationOwnerHasEveryPermission()
    {
        Assert.All(Enum.GetValues<PermissionCode>(), permission =>
            Assert.True(ClearLend.Application.Behaviors.StaffPermissionPolicy.Allows([StaffRole.ApplicationOwner], permission)));
    }

    [Fact]
    public async Task AuthorizationRejectsPlainDomainResultCommandThroughMediator()
    {
        using var provider = BuildProvider();
        var command = new AddStaffRoleCommand(new StaffMemberId(Guid.NewGuid()), StaffRole.SupportAgent, new StaffMemberId(Guid.NewGuid()));

        var result = await provider.GetRequiredService<ISender>().Send(command);

        Assert.False(result.IsSuccess);
        Assert.Equal("authorization.forbidden", result.Error?.Code);
    }

    [Fact]
    public async Task AuthorizationRejectsGenericDomainResultCommandThroughMediator()
    {
        using var provider = BuildProvider();
        var command = new OpenWorkItemCommand(WorkItemType.Registration, "synthetic-profile", WorkItemPriority.Normal,
            new WorkQueueId(Guid.NewGuid()), new StaffMemberId(Guid.NewGuid()));

        var result = await provider.GetRequiredService<ISender>().Send(command);

        Assert.False(result.IsSuccess);
        Assert.Equal("authorization.forbidden", result.Error?.Code);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddClearLendApplication();
        services.AddSingleton<IStaffMemberRepository, StaffRepository>();
        services.AddSingleton<IWorkItemRepository, WorkItemRepository>();
        services.AddSingleton<IWorkQueueRepository, QueueRepository>();
        services.AddSingleton<IAuthorizationService, DenyAuthorization>();
        services.AddSingleton<IAuditEventWriter, AuditWriter>();
        services.AddSingleton<IUnitOfWork, UnitOfWork>();
        return services.BuildServiceProvider();
    }

    private sealed class StaffRepository : IStaffMemberRepository
    {
        public Task AddAsync(StaffMember staffMember, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<StaffMember?> GetAsync(StaffMemberId id, CancellationToken cancellationToken) => Task.FromResult<StaffMember?>(null);
        public Task<int> CountActiveApplicationOwnersAsync(StaffMemberId? excluding, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<int> CountApplicationOwnersAsync(CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<bool> ExistsForAccountAsync(UserAccountId accountId, CancellationToken cancellationToken) => Task.FromResult(false);
    }
    private sealed class WorkItemRepository : IWorkItemRepository
    {
        public Task AddAsync(WorkItem workItem, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<WorkItem?> GetAsync(WorkItemId id, CancellationToken cancellationToken) => Task.FromResult<WorkItem?>(null);
        public Task<WorkItem?> FindActiveAsync(WorkItemType type, string subjectReference, CancellationToken cancellationToken) => Task.FromResult<WorkItem?>(null);
        public Task<bool> HasActiveAssignmentAsync(WorkQueueId queueId, StaffMemberId staffMemberId, CancellationToken cancellationToken) => Task.FromResult(false);
        Task<bool> IWorkItemRepository.HasAnyActiveAssignmentAsync(StaffMemberId staffMemberId, CancellationToken cancellationToken) => Task.FromResult(false);
    }
    private sealed class QueueRepository : IWorkQueueRepository
    {
        public Task AddAsync(WorkQueueDefinition queue, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<WorkQueueDefinition?> GetAsync(WorkQueueId id, CancellationToken cancellationToken) => Task.FromResult<WorkQueueDefinition?>(null);
    }
    private sealed class DenyAuthorization : IAuthorizationService
    { public Task<bool> HasPermissionAsync(StaffMemberId staffMemberId, PermissionCode permission, CancellationToken cancellationToken) => Task.FromResult(false); }
    private sealed class AuditWriter : IAuditEventWriter
    { public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken) => Task.CompletedTask; }
    private sealed class UnitOfWork : IUnitOfWork
    { public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask; }
}
