using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Text.Json;

namespace ProviderDirectoryAPI.Middleware;

public class FhirErrorMiddleware
{
    private readonly RequestDelegate _next;

    public FhirErrorMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async System.Threading.Tasks.Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/fhir+json";
            var outcome = new OperationOutcome();
            outcome.Issue.Add(new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Error,
                Code = OperationOutcome.IssueType.Exception,
                Diagnostics = ex.Message
            });
            var options = new JsonSerializerOptions().ForFhir().Pretty();
            var json = JsonSerializer.Serialize(outcome, options);
            await context.Response.WriteAsync(json);
        }
    }
}
