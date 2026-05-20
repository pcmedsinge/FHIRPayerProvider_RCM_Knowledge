using ProviderDirectoryAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Provider Directory API (DaVinci PDEX Plan-Net)", Version = "v1" });
});

// Register FHIR services
builder.Services.AddSingleton<IProviderService, ProviderService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ProviderDirectoryAPI.Middleware.FhirErrorMiddleware>();
app.MapControllers();

app.Run();
