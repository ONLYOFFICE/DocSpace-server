namespace ASC.Files.Core.RoomTemplates.Events;

[Scope]
public class RoomTemplatesIntegrationEventConsumer(RoomTemplatesWorker worker) : 
    IConsumer<CreateRoomTemplateIntegrationEvent>,
    IConsumer<CreateRoomFromTemplateIntegrationEvent>
{
    public async Task Consume(ConsumeContext<CreateRoomTemplateIntegrationEvent> context)
    {
        var @event = context.Message;
        await worker.StartCreateTemplateAsync(@event.TenantId, @event.CreateBy, @event.RoomId, @event.Title, @event.Emails, @event.Logo, @event.CopyLogo, @event.Tags, @event.Groups, @event.Cover, @event.Color, true, @event.TaskId);
    }

    public async Task Consume(ConsumeContext<CreateRoomFromTemplateIntegrationEvent> context)
    {
        var @event = context.Message;
        await worker.StartCreateRoomAsync(@event.TenantId, @event.CreateBy, @event.TemplateId, @event.Title, @event.Logo, @event.CopyLogo, @event.Tags, @event.Cover, @event.Color, true, @event.TaskId);
    }
}
