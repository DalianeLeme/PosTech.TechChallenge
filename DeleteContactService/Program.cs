using DeleteContactService.Messaging;
using TechChallenge.Infrastructure.Messaging;
using Prometheus; // Adicionar Prometheus

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddControllers();

builder.Services.AddSingleton<IRabbitMQPublisher, RabbitMQDeletePublisher>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Delete Contact Service API V1");
    c.RoutePrefix = string.Empty; // Define a rota raiz como o Swagger UI
});

// Adicionando suporte ao Prometheus
app.UseRouting();
app.UseAuthorization();
app.UseHttpMetrics(); // Middleware para coletar métricas HTTP
app.MapControllers();
app.MapMetrics(); // Expõe /metrics para Prometheus

app.Run();
