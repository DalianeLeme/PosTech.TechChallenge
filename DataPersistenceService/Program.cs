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

builder.Services.AddDbContext<ContactDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConexaoPadrao")));
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddSingleton<GetRabbitMQConsumer>();
builder.Services.AddSingleton<CreateRabbitMQConsumer>();
builder.Services.AddSingleton<UpdateRabbitMQConsumer>();
builder.Services.AddSingleton<DeleteRabbitMQConsumer>();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Persist Contacts API V1");
    c.RoutePrefix = "swagger"; // Agora o Swagger estar� acess�vel em /swagger
});

// Consumidores
var createConsumer = app.Services.GetRequiredService<CreateRabbitMQConsumer>();
var updateConsumer = app.Services.GetRequiredService<UpdateRabbitMQConsumer>();
var deleteConsumer = app.Services.GetRequiredService<DeleteRabbitMQConsumer>();
var getConsumer = app.Services.GetRequiredService<GetRabbitMQConsumer>();


// Inicializando os consumidores
Task.Run(() => createConsumer.StartConsumingAsync());
Task.Run(() => updateConsumer.StartConsumingAsync());
Task.Run(() => deleteConsumer.StartConsumingAsync());
Task.Run(() => getConsumer.StartConsumingAsync());

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

