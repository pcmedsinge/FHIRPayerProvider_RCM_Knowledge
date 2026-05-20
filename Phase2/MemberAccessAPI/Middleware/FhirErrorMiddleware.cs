using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Text.Json;

namespace MemberAccessAPI.Middleware;

public class FhirErrorMiddleware
{
    private readonly RequestDelegate _next;

    public FhirErrorMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async System.Threading.Tasks.Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Convert standard HTTP errors on FHIR endpoints to OperationOutcome
        if (context.Request.Path.StartsWithSegments("/api/fhir") && 
            context.Response.StatusCode >= 400 &&
            !context.Response.HasStarted)
        {
            var outcome = new OperationOutcome();
            var issue = new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Error,
                Code = context.Response.StatusCode switch
                {
                    401 => OperationOutcome.IssueType.Login,
                    403 => OperationOutcome.IssueType.Forbidden,
                    404 => OperationOutcome.IssueType.NotFound,
                    _ => OperationOutcome.IssueType.Processing
                },
                Diagnostics = context.Response.StatusCode switch
                {
                    401 => "Authentication required. Use POST /api/auth/token to get a token.",
                    403 => "Access denied. You can only access your own data.",
                    404 => "Resource not found.",
                    _ => "An error occurred processing the request."
                }
            };
            outcome.Issue.Add(issue);

            context.Response.ContentType = "application/fhir+json";
            var options = new JsonSerializerOptions().ForFhir().Pretty();
            var json = JsonSerializer.Serialize(outcome, options);
            await context.Response.WriteAsync(json);
        }
    }
}