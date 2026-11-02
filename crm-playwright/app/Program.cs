using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using CrmLeads;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;

var builder = WebApplication.CreateBuilder(args);
var settings = Settings.From(builder.Configuration);

var credentials = new BasicAWSCredentials(settings.AwsAccessKey, settings.AwsSecretKey);
var dynamo = new AmazonDynamoDBClient(credentials, new AmazonDynamoDBConfig
{
    ServiceURL = settings.DynamoDbUrl,
    AuthenticationRegion = settings.AwsRegion,
});
var sqs = new AmazonSQSClient(credentials, new AmazonSQSConfig
{
    ServiceURL = settings.SqsUrl,
    AuthenticationRegion = settings.AwsRegion,
});

if (args.Contains("--bootstrap"))
{
    // Creates the table and the queue the app expects, as an operator would
    // before the app starts. The app itself never creates them.
    await Bootstrap.Run(dynamo, sqs, settings);
    return;
}

builder.Services.AddSingleton<IAmazonDynamoDB>(dynamo);
builder.Services.AddSingleton<IAmazonSQS>(sqs);
builder.Services.AddSingleton(settings);
builder.Services.AddSingleton<IPulsarClient>(_ =>
    PulsarClient.Builder().ServiceUrl(new Uri(settings.PulsarUrl)).Build());
builder.Services.AddSingleton<IProducer<string>>(sp =>
    sp.GetRequiredService<IPulsarClient>().NewProducer(Schema.String).Topic(settings.PulsarTopic).Create());
builder.Services.AddSingleton<LeadStore>();
builder.Services.AddHostedService<ActivityConsumer>();
builder.Services.AddHttpClient("payments", c =>
{
    c.BaseAddress = new Uri(settings.PaymentsUrl);
    c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.PaymentsKey);
});
builder.Services.AddHttpClient("lms", c =>
{
    c.BaseAddress = new Uri(settings.LmsUrl);
    c.DefaultRequestHeaders.Add("X-Api-Key", settings.LmsKey);
});

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/leads", async (NewLead body, LeadStore store, IAmazonSQS queue, IProducer<string> events, CancellationToken ct) =>
{
    var problem = body.Validate();
    if (problem is not null)
    {
        return Results.BadRequest(new { error = problem });
    }

    var lead = await store.Create(body, ct);
    var created = JsonSerializer.Serialize(new { type = "lead.created", id = lead.Id, email = lead.Email, at = lead.CreatedAt });
    await queue.SendMessageAsync(new SendMessageRequest { QueueUrl = settings.LeadEventsQueueUrl, MessageBody = created }, ct);
    await events.Send(created, ct);
    return Results.Created($"/api/leads/{lead.Id}", lead);
});

app.MapGet("/api/leads", async (LeadStore store, CancellationToken ct) => Results.Ok(await store.List(ct)));

app.MapGet("/api/leads/{id}", async (string id, LeadStore store, CancellationToken ct) =>
    await store.Get(id, ct) is { } lead ? Results.Ok(lead) : Results.NotFound(new { error = $"no lead {id}" }));

app.MapPost("/api/leads/{id}/payments", async (string id, NewPayment body, LeadStore store, IHttpClientFactory http, CancellationToken ct) =>
{
    if (await store.Get(id, ct) is not { } lead)
    {
        return Results.NotFound(new { error = $"no lead {id}" });
    }

    using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/charges")
    {
        Content = JsonContent(new { amount = body.Amount, currency = body.Currency, customer_email = lead.Email, description = $"Lead {lead.Id}" }),
    };
    request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
    using var response = await http.CreateClient("payments").SendAsync(request, ct);
    var charge = await ReadJson(response, ct);
    if (!response.IsSuccessStatusCode)
    {
        return Results.Json(new { error = "the payment provider refused the charge", provider = charge }, statusCode: 502);
    }

    var payment = new Payment(
        (string)charge["id"]!,
        (string)charge["status"]!,
        body.Amount,
        body.Currency,
        DateTimeOffset.FromUnixTimeSeconds((long)charge["created"]!).UtcDateTime.ToString("O"));
    await store.SetPayment(id, payment, ct);
    return Results.Ok(payment);
});

app.MapPost("/api/leads/{id}/enrollments", async (string id, NewEnrollment body, LeadStore store, IHttpClientFactory http, CancellationToken ct) =>
{
    if (await store.Get(id, ct) is not { } lead)
    {
        return Results.NotFound(new { error = $"no lead {id}" });
    }

    using var response = await http.CreateClient("lms").PostAsync(
        "/api/v2/enrollments",
        JsonContent(new { course_id = body.CourseId, learner = new { email = lead.Email, name = lead.Name } }),
        ct);
    var enrolled = await ReadJson(response, ct);
    if (!response.IsSuccessStatusCode)
    {
        return Results.Json(new { error = "the LMS refused the enrollment", provider = enrolled }, statusCode: 502);
    }

    var enrollment = new Enrollment((string)enrolled["enrollment_id"]!, body.CourseId, (string)enrolled["status"]!, (string)enrolled["enrolled_at"]!);
    await store.SetEnrollment(id, enrollment, ct);
    return Results.Ok(enrollment);
});

// The server's clock as the app reads it (DateTime.UtcNow), for checking
// keploy's time freeze on a .NET process.
app.MapGet("/api/time", () => Results.Ok(new { utcNow = DateTime.UtcNow.ToString("O") }));

app.Run();

static StringContent JsonContent(object value) =>
    new(JsonSerializer.Serialize(value), System.Text.Encoding.UTF8, "application/json");

static async Task<JsonObject> ReadJson(HttpResponseMessage response, CancellationToken ct)
{
    var text = await response.Content.ReadAsStringAsync(ct);
    return JsonNode.Parse(text) as JsonObject
        ?? throw new InvalidOperationException($"{response.RequestMessage?.RequestUri} answered {(int)response.StatusCode} with a body that is not a JSON object: {text}");
}
