using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace CrmLeads;

// `dotnet CrmLeads.dll --bootstrap`: creates the Leads table and the
// lead-events queue on the local DynamoDB and SQS before a record. It runs
// outside keploy; the app itself only reads and writes them.
public static class Bootstrap
{
    public static async Task Run(IAmazonDynamoDB dynamo, IAmazonSQS sqs, Settings settings)
    {
        var tables = await dynamo.ListTablesAsync();
        if (!tables.TableNames.Contains(settings.LeadsTable))
        {
            await dynamo.CreateTableAsync(new CreateTableRequest
            {
                TableName = settings.LeadsTable,
                KeySchema = [new KeySchemaElement("Id", KeyType.HASH)],
                AttributeDefinitions = [new AttributeDefinition("Id", ScalarAttributeType.S)],
                BillingMode = BillingMode.PAY_PER_REQUEST,
            });
        }

        var queue = await sqs.CreateQueueAsync(new CreateQueueRequest { QueueName = settings.LeadEventsQueue });
        // The queue's host is the SQS endpoint's choice (localhost, a container
        // name); its path names the account and the queue the app sends to.
        if (new Uri(queue.QueueUrl).AbsolutePath != new Uri(settings.LeadEventsQueueUrl).AbsolutePath)
        {
            throw new InvalidOperationException(
                $"the queue's URL is {queue.QueueUrl}, but the app sends to {settings.LeadEventsQueueUrl}: set Aws:LeadEventsQueueUrl");
        }

        Console.WriteLine($"bootstrap: table {settings.LeadsTable} and queue {queue.QueueUrl} are ready");
    }
}
