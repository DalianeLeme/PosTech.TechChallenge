# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os arquivos de projeto individualmente para otimizar o cache do Docker
COPY ["UpdateContactService/UpdateContactService.csproj", "UpdateContactService/"]
COPY ["TechChallenge.Application/TechChallenge.Application.csproj", "TechChallenge.Application/"]
COPY ["TechChallenge.Domain/TechChallenge.Domain.csproj", "TechChallenge.Domain/"]
COPY ["TechChallenge.Infrastructure/TechChallenge.Infrastructure.csproj", "TechChallenge.Infrastructure/"]

# Adiciona fontes de pacotes NuGet se necess�rio

# Restaura as depend�ncias de todos os projetos
RUN dotnet restore "UpdateContactService/UpdateContactService.csproj"

# Copia todo o c�digo-fonte para o cont�iner
COPY . .

# Define o diret�rio de trabalho para a compila��o
WORKDIR "/src/UpdateContactService"

# Executa novamente a restaura��o para garantir que todos os pacotes sejam encontrados corretamente
RUN dotnet restore 

# Compila a aplica��o para ambiente de produ��o
RUN dotnet publish -c Release -o /app/out --no-restore

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copia o build gerado para o cont�iner final
COPY --from=build /app/out .

# Define a vari�vel de ambiente opcional para configura��es
ENV ASPNETCORE_ENVIRONMENT=Production

# Exposi��o da porta do microsservi�o
ENV ASPNETCORE_URLS=http://+:80

# Executa o microsservi�o
ENTRYPOINT ["dotnet", "UpdateContactService.dll"]
