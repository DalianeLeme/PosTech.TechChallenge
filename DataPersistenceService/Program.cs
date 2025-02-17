using DataPersistenceService.Messaging;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using TechChallenge.Application.Services;
using TechChallenge.Infrastructure.Context;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://+:80");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers(options =>
{
    var partManager = builder.Services
        .Where(sd => sd.ServiceType == typeof(ApplicationPartManager))
        .Select(sd => sd.ImplementationInstance)
        .FirstOrDefault() as ApplicationPartManager;

    partManager?.ApplicationParts.Clear();
    partManager?.ApplicationParts.Add(new AssemblyPart(typeof(Program).Assembly));
});

// 🔹 Pegando a Connection String das variáveis de ambiente ou do appsettings.json
var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING")
                       ?? builder.Configuration.GetConnectionString("ConexaoPadrao");

builder.Services.AddDbContext<ContactDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddSingleton<GetRabbitMQConsumer>();
builder.Services.AddSingleton<CreateRabbitMQConsumer>();
builder.Services.AddSingleton<UpdateRabbitMQConsumer>();
builder.Services.AddSingleton<DeleteRabbitMQConsumer>();

var app = builder.Build();

// 🔹 Aplicar Migrations Automaticamente e Testar Conexão com o Banco
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ContactDbContext>();
    try
    {
        Console.WriteLine("Aplicando migrations...");
        dbContext.Database.Migrate();
        Console.WriteLine("Migrations aplicadas com sucesso!");

        Console.WriteLine("Testando conexão com o banco...");
        dbContext.Database.OpenConnection();
        Console.WriteLine("Conexão com o banco de dados foi aberta com sucesso!");
        dbContext.Database.CloseConnection();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Falha ao conectar ao banco: {ex.Message}");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Persist Contacts API V1");
    c.RoutePrefix = "swagger"; // Agora o Swagger estará acessível em /swagger
});

// 🔹 Inicializando os consumidores RabbitMQ
var createConsumer = app.Services.GetRequiredService<CreateRabbitMQConsumer>();
var updateConsumer = app.Services.GetRequiredService<UpdateRabbitMQConsumer>();
var deleteConsumer = app.Services.GetRequiredService<DeleteRabbitMQConsumer>();
var getConsumer = app.Services.GetRequiredService<GetRabbitMQConsumer>();

Task.Run(() => createConsumer.StartConsumingAsync());
Task.Run(() => updateConsumer.StartConsumingAsync());
Task.Run(() => deleteConsumer.StartConsumingAsync());
Task.Run(() => getConsumer.StartConsumingAsync());

app.UseAuthorization();
app.MapControllers();
app.Run();
