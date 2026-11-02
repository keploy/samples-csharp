using System.Text.Json;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;

namespace CrmLeads;

// Reads lead events from Pulsar, as their notification services do, and
// records a "welcome-queued" activity on each new lead. The lead page shows
// the activity once it is there.
public sealed class ActivityConsumer(IPulsarClient pulsar, Settings settings, LeadStore store, ILogger<ActivityConsumer> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        await using var consumer = pulsar.NewConsumer(Schema.String)
            .SubscriptionName(settings.PulsarSubscription)
            .SubscriptionType(SubscriptionType.Exclusive)
            .InitialPosition(SubscriptionInitialPosition.Earliest)
            .Topic(settings.PulsarTopic)
            .Create();

        await foreach (var message in consumer.Messages(stop))
        {
            var leadId = "";
            try
            {
                using var doc = JsonDocument.Parse(message.Value());
                if (doc.RootElement.GetProperty("type").GetString() == "lead.created")
                {
                    leadId = doc.RootElement.GetProperty("id").GetString() ?? "";
                    await store.AddActivity(leadId, new Activity("welcome-queued", DateTime.UtcNow.ToString("O")), stop);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogError(e, "lead event {MessageId} (lead {LeadId}) was not applied", message.MessageId, leadId);
            }

            await consumer.Acknowledge(message.MessageId, stop);
        }
    }
}
