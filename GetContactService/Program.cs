using GetContactService.Messaging;
using TechChallenge.Infrastructure.Messaging;
using Prometheus; // Adicionar Prometheus

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddSingleton<IRabbitMQPublisher, RabbitMQGetPublisher>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Create Contact Service API V1");
    c.RoutePrefix = string.Empty;
});

// Adicionando suporte ao Prometheus
app.UseRouting(); // Necess�rio para MapMetrics()
app.UseHttpMetrics(); // Middleware para coletar m�tricas HTTP

app.UseAuthorization();
app.MapControllers();
app.MapMetrics(); // Exp�e /metrics para Prometheus

app.Run();
