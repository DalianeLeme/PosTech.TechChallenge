using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using TechChallenge.Infrastructure.Context;

namespace TechChallenge.Infrastructure
{
    public class ContactDbContextFactory : IDesignTimeDbContextFactory<ContactDbContext>
    {
        public ContactDbContext CreateDbContext(string[] args)
        {
            // Caminho do microserviço de DataPersistenceService
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "DataPersistenceService");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)  // Define o caminho para encontrar o appsettings.json
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ContactDbContext>();
            var connectionString = configuration.GetConnectionString("ConexaoPadrao");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("A string de conexão não foi encontrada no appsettings.json do DataPersistenceService.");
            }

            optionsBuilder.UseSqlServer(connectionString);

            return new ContactDbContext(optionsBuilder.Options);
        }
    }
}
