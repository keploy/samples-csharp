using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace CrmLeads;

// Leads in one DynamoDB table, keyed by Id, through the low-level client the
// AWS SDK for .NET gives (SigV4-signed JSON over HTTP, CRC32-checked replies).
public sealed class LeadStore(IAmazonDynamoDB dynamo, Settings settings)
{
    public async Task<Lead> Create(NewLead body, CancellationToken ct)
    {
        var lead = new Lead(
            Guid.NewGuid().ToString(),
            body.Name!.Trim(),
            body.Email!.Trim(),
            body.Phone?.Trim() ?? "",
            body.Source?.Trim() ?? "",
            body.Budget,
            body.Custom ?? new Dictionary<string, string>(),
            "new",
            DateTime.UtcNow.ToString("O"),
            null,
            null,
            []);

        var item = new Dictionary<string, AttributeValue>
        {
            ["Id"] = new() { S = lead.Id },
            ["Name"] = new() { S = lead.Name },
            ["Email"] = new() { S = lead.Email },
            ["Phone"] = new() { S = lead.Phone },
            ["Source"] = new() { S = lead.Source },
            ["Custom"] = new() { M = lead.Custom.ToDictionary(kv => kv.Key, kv => new AttributeValue { S = kv.Value }) },
            ["Status"] = new() { S = lead.Status },
            ["CreatedAt"] = new() { S = lead.CreatedAt },
            ["Activity"] = new() { L = [], IsLSet = true },
        };
        if (lead.Budget is { } budget)
        {
            item["Budget"] = new() { N = budget.ToString(CultureInfo.InvariantCulture) };
        }

        await dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = settings.LeadsTable,
            Item = item,
            ConditionExpression = "attribute_not_exists(Id)",
        }, ct);
        return lead;
    }

    public async Task<Lead?> Get(string id, CancellationToken ct)
    {
        var got = await dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = settings.LeadsTable,
            Key = Key(id),
            ConsistentRead = true,
        }, ct);
        return got.IsItemSet && got.Item.Count > 0 ? FromItem(got.Item) : null;
    }

    public async Task<List<Lead>> List(CancellationToken ct)
    {
        var leads = new List<Lead>();
        Dictionary<string, AttributeValue>? start = null;
        do
        {
            var page = await dynamo.ScanAsync(new ScanRequest
            {
                TableName = settings.LeadsTable,
                ExclusiveStartKey = start,
                ConsistentRead = true,
            }, ct);
            leads.AddRange(page.Items.Select(FromItem));
            start = page.LastEvaluatedKey is { Count: > 0 } next ? next : null;
        }
        while (start is not null);

        return leads.OrderByDescending(l => l.CreatedAt, StringComparer.Ordinal).ToList();
    }

    public Task SetPayment(string id, Payment p, CancellationToken ct) =>
        dynamo.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = settings.LeadsTable,
            Key = Key(id),
            UpdateExpression = "SET Payment = :p, #s = :s",
            ExpressionAttributeNames = new() { ["#s"] = "Status" },
            ExpressionAttributeValues = new()
            {
                [":p"] = new() { M = new() {
                    ["ChargeId"] = new() { S = p.ChargeId },
                    ["Status"] = new() { S = p.Status },
                    ["Amount"] = new() { N = p.Amount.ToString(CultureInfo.InvariantCulture) },
                    ["Currency"] = new() { S = p.Currency },
                    ["ChargedAt"] = new() { S = p.ChargedAt },
                } },
                [":s"] = new() { S = p.Status == "succeeded" ? "paid" : "payment-" + p.Status },
            },
            ConditionExpression = "attribute_exists(Id)",
        }, ct);

    public Task SetEnrollment(string id, Enrollment e, CancellationToken ct) =>
        dynamo.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = settings.LeadsTable,
            Key = Key(id),
            UpdateExpression = "SET Enrollment = :e",
            ExpressionAttributeValues = new()
            {
                [":e"] = new() { M = new() {
                    ["EnrollmentId"] = new() { S = e.EnrollmentId },
                    ["CourseId"] = new() { S = e.CourseId },
                    ["Status"] = new() { S = e.Status },
                    ["EnrolledAt"] = new() { S = e.EnrolledAt },
                } },
            },
            ConditionExpression = "attribute_exists(Id)",
        }, ct);

    public Task AddActivity(string id, Activity a, CancellationToken ct) =>
        dynamo.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = settings.LeadsTable,
            Key = Key(id),
            UpdateExpression = "SET Activity = list_append(if_not_exists(Activity, :none), :a)",
            ExpressionAttributeValues = new()
            {
                [":none"] = new() { L = [], IsLSet = true },
                [":a"] = new() { L = [new() { M = new() {
                    ["Type"] = new() { S = a.Type },
                    ["At"] = new() { S = a.At },
                } }] },
            },
            ConditionExpression = "attribute_exists(Id)",
        }, ct);

    private static Dictionary<string, AttributeValue> Key(string id) => new() { ["Id"] = new() { S = id } };

    private static Lead FromItem(Dictionary<string, AttributeValue> i)
    {
        string S(string k) => i.TryGetValue(k, out var v) && v.S is not null ? v.S : "";
        Payment? payment = i.TryGetValue("Payment", out var p) && p.IsMSet
            ? new Payment(p.M["ChargeId"].S, p.M["Status"].S, long.Parse(p.M["Amount"].N, CultureInfo.InvariantCulture), p.M["Currency"].S, p.M["ChargedAt"].S)
            : null;
        Enrollment? enrollment = i.TryGetValue("Enrollment", out var e) && e.IsMSet
            ? new Enrollment(e.M["EnrollmentId"].S, e.M["CourseId"].S, e.M["Status"].S, e.M["EnrolledAt"].S)
            : null;
        var activity = i.TryGetValue("Activity", out var a) && a.IsLSet
            ? a.L.Select(x => new Activity(x.M["Type"].S, x.M["At"].S)).ToList()
            : [];
        return new Lead(
            S("Id"),
            S("Name"),
            S("Email"),
            S("Phone"),
            S("Source"),
            i.TryGetValue("Budget", out var b) && b.N is not null ? decimal.Parse(b.N, CultureInfo.InvariantCulture) : null,
            i.TryGetValue("Custom", out var c) && c.IsMSet ? c.M.ToDictionary(kv => kv.Key, kv => kv.Value.S) : [],
            S("Status"),
            S("CreatedAt"),
            payment,
            enrollment,
            activity);
    }
}
