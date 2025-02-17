using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TechChallenge.Application.Services;
using TechChallenge.Domain.Models.Requests;
using TechChallenge.Domain.Models.Responses;

namespace DataPersistenceService.Messaging
{
    public class UpdateRabbitMQConsumer : IAsyncDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UpdateRabbitMQConsumer> _logger;
        private readonly string _hostname = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq-service.default.svc.cluster.local";
        private readonly int _port = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var port) ? port : 5672;
        private readonly string _username = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
        private readonly string _password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
        private readonly string _queueName = "update_contact_queue";

        private IConnection _connection;
        private IChannel _channel;
        private static UpdateContactResponse? _lastUpdatedContact; // Variável para armazenar o último contato atualizado

        public UpdateRabbitMQConsumer(IServiceProvider serviceProvider, ILogger<UpdateRabbitMQConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartConsumingAsync()
        {
            var retryPolicy = Policy.Handle<Exception>().RetryAsync(3, onRetry: (exception, retryCount) =>
            {
                _logger.LogWarning($"Retry {retryCount} devido a erro: {exception.Message}");
            });

            var circuitBreakerPolicy = Policy.Handle<Exception>().CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 2,
                durationOfBreak: TimeSpan.FromSeconds(10),
                onBreak: (exception, duration) =>
                {
                    _logger.LogWarning($"Circuito aberto devido a erro: {exception.Message}. Reiniciará em {duration.TotalSeconds} segundos.");
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuito fechado. Operações retomadas.");
                });

            var combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

            try
            {
                await combinedPolicy.ExecuteAsync(async () =>
                {
                    await InitializeRabbitMQAsync();
                    await StartConsumingInternalAsync();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro crítico ao executar consumidor: {ex.Message}");
            }
        }

        private async Task InitializeRabbitMQAsync()
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _hostname,
                    Port = _port,
                    UserName = _username,
                    Password = _password,
                    RequestedHeartbeat = TimeSpan.FromSeconds(30)
                };

                _logger.LogInformation("Tentando estabelecer conexão com RabbitMQ...");
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _logger.LogInformation($"Conectado ao RabbitMQ e fila {_queueName} declarada.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao conectar ao RabbitMQ: {ex.Message}");
                throw;
            }
        }

        private async Task StartConsumingInternalAsync()
        {
            _logger.LogInformation("Inicializando o consumidor...");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    _logger.LogInformation($"Mensagem recebida da fila {_queueName}: {message}");

                    var request = JsonSerializer.Deserialize<UpdateContactRequest>(message);
                    if (request != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var contactService = scope.ServiceProvider.GetRequiredService<IContactService>();

                        _logger.LogInformation($"Atualizando contato ID: {request.Id}");
                        var updatedContact = await contactService.UpdateContact(request);

                        // Armazena o último contato atualizado
                        _lastUpdatedContact = updatedContact;
                        _logger.LogInformation($"Último contato atualizado: {JsonSerializer.Serialize(_lastUpdatedContact)}");

                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    else
                    {
                        _logger.LogWarning("Mensagem inválida: dados ausentes.");
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Erro ao processar mensagem: {ex.Message}");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
            };

            await _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer);

            _logger.LogInformation($"Consumidor iniciado na fila {_queueName}");
        }

        /// <summary>
        /// Retorna o último contato atualizado processado pelo consumidor.
        /// </summary>
        public UpdateContactResponse? GetLastUpdatedContact()
        {
            return _lastUpdatedContact;
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogInformation("Finalizando consumidor e fechando conexões...");
            if (_channel is not null)
            {
                await _channel.CloseAsync();
            }
            if (_connection is not null)
            {
                await _connection.CloseAsync();
            }
        }
    }
}