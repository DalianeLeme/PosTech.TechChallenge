# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os arquivos da solu��o e projetos
COPY *.sln ./
COPY DataPersistenceService/DataPersistenceService.csproj DataPersistenceService/
COPY CreateContactService/CreateContactService.csproj CreateContactService/
COPY DeleteContactService/DeleteContactService.csproj DeleteContactService/
COPY GetContactService/GetContactService.csproj GetContactService/
COPY UpdateContactService/UpdateContactService.csproj UpdateContactService/
COPY TechChallenge.Application/TechChallenge.Application.csproj TechChallenge.Application/
COPY TechChallenge.Domain/TechChallenge.Domain.csproj TechChallenge.Domain/
COPY TechChallenge.Infrastructure/TechChallenge.Infrastructure.csproj TechChallenge.Infrastructure/

# Restaura os pacotes
RUN dotnet restore DataPersistenceService/DataPersistenceService.csproj

# Copia o restante dos arquivos
COPY . .

# Remove arquivos duplicados antes de compilar
RUN find . -name "appsettings.Development.json" -delete

# Define o diret�rio de build e compila o microsservi�o
WORKDIR /src/DataPersistenceService
RUN dotnet publish -c Release -o /app/out

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Exp�e as portas 80 e 8080 para comunica��o
# Copia o build gerado para o cont�iner final
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:80

# Define o ponto de entrada
ENTRYPOINT ["dotnet", "DataPersistenceService.dll"]
