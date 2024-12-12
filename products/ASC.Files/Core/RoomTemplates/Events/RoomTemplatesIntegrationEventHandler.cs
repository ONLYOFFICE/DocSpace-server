namespace ASC.Files.Core.RoomTemplates.Events;

[Scope]
public class RoomTemplatesIntegrationEventHandler(RoomTemplatesWorker worker)
    : IIntegrationEventHandler<CreateRoomTemplateIntegrationEvent>,
      IIntegrationEventHandler<CreateRoomFromTemplateIntegrationEvent>
{
    public async Task Handle(CreateRoomTemplateIntegrationEvent @event)
    {
        await worker.StartCreateTemplateAsync(@event.TenantId, @event.CreateBy, @event.RoomId, @event.Title, @event.Emails, @event.Logo, @event.Tags);
    }

    public async Task Handle(CreateRoomFromTemplateIntegrationEvent @event)
    {
        await worker.StartCreateRoomAsync(@event.TenantId, @event.CreateBy, @event.TemplateId, @event.Title, @event.Logo, @event.Tags);
    }
}
