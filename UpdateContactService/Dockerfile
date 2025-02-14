# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os arquivos de projeto individualmente para otimizar o cache do Docker
COPY ["UpdateContactService/UpdateContactService.csproj", "UpdateContactService/"]
COPY ["TechChallenge.Application/TechChallenge.Application.csproj", "TechChallenge.Application/"]
COPY ["TechChallenge.Domain/TechChallenge.Domain.csproj", "TechChallenge.Domain/"]
COPY ["TechChallenge.Infrastructure/TechChallenge.Infrastructure.csproj", "TechChallenge.Infrastructure/"]

# Adiciona fontes de pacotes NuGet se necessário

# Restaura as dependências de todos os projetos
RUN dotnet restore "UpdateContactService/UpdateContactService.csproj"

# Copia todo o código-fonte para o contêiner
COPY . .

# Define o diretório de trabalho para a compilação
WORKDIR "/src/UpdateContactService"

# Executa novamente a restauração para garantir que todos os pacotes sejam encontrados corretamente
RUN dotnet restore 

# Compila a aplicação para ambiente de produção
RUN dotnet publish -c Release -o /app/out --no-restore

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copia o build gerado para o contêiner final
COPY --from=build /app/out .

# Define a variável de ambiente opcional para configurações
ENV ASPNETCORE_ENVIRONMENT=Production

# Exposição da porta do microsserviço
EXPOSE 5000

# Executa o microsserviço
ENTRYPOINT ["dotnet", "UpdateContactService.dll"]
