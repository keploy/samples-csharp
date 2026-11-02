namespace CrmLeads;

public sealed record NewLead(string? Name, string? Email, string? Phone, string? Source, decimal? Budget, Dictionary<string, string>? Custom)
{
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return "name is required";
        }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            return "a valid email is required";
        }

        return null;
    }
}

public sealed record NewPayment(long Amount, string Currency);

public sealed record NewEnrollment(string CourseId);

public sealed record Payment(string ChargeId, string Status, long Amount, string Currency, string ChargedAt);

public sealed record Enrollment(string EnrollmentId, string CourseId, string Status, string EnrolledAt);

public sealed record Activity(string Type, string At);

public sealed record Lead(
    string Id,
    string Name,
    string Email,
    string Phone,
    string Source,
    decimal? Budget,
    Dictionary<string, string> Custom,
    string Status,
    string CreatedAt,
    Payment? Payment,
    Enrollment? Enrollment,
    List<Activity> Activity);

public sealed record Settings(
    string AwsRegion,
    string AwsAccessKey,
    string AwsSecretKey,
    string DynamoDbUrl,
    string SqsUrl,
    string LeadsTable,
    string LeadEventsQueue,
    string LeadEventsQueueUrl,
    string PulsarUrl,
    string PulsarTopic,
    string PulsarSubscription,
    string PaymentsUrl,
    string PaymentsKey,
    string LmsUrl,
    string LmsKey)
{
    public static Settings From(IConfiguration c)
    {
        string Need(string key) =>
            c[key] is { Length: > 0 } value ? value : throw new InvalidOperationException($"the setting {key} is not set");

        var sqsUrl = Need("Aws:SqsUrl");
        var queue = Need("Aws:LeadEventsQueue");
        return new Settings(
            Need("Aws:Region"),
            Need("Aws:AccessKey"),
            Need("Aws:SecretKey"),
            Need("Aws:DynamoDbUrl"),
            sqsUrl,
            Need("Aws:LeadsTable"),
            queue,
            // ElasticMQ's and LocalStack's queue URL for the default account.
            c["Aws:LeadEventsQueueUrl"] is { Length: > 0 } url ? url : $"{sqsUrl.TrimEnd('/')}/000000000000/{queue}",
            Need("Pulsar:Url"),
            Need("Pulsar:Topic"),
            Need("Pulsar:Subscription"),
            Need("Payments:BaseUrl"),
            Need("Payments:ApiKey"),
            Need("Lms:BaseUrl"),
            Need("Lms:ApiKey"));
    }
}
