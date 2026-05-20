using FormularyAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Drug Formulary API (DaVinci Drug Formulary)", Version = "v1" });
});

// Register services
builder.Services.AddSingleton<IFormularyService, FormularyService>();
builder.Services.AddSingleton<IFhirFormularyService, FhirFormularyService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<FormularyAPI.Middleware.FhirErrorMiddleware>();
app.MapControllers();

app.Run();
