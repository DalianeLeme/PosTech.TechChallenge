# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os arquivos de projeto e restaura depend�ncias
COPY ["GetContactService/GetContactService.csproj", "GetContactService/"]
COPY ["TechChallenge.Application/TechChallenge.Application.csproj", "TechChallenge.Application/"]
COPY ["TechChallenge.Domain/TechChallenge.Domain.csproj", "TechChallenge.Domain/"]
COPY ["TechChallenge.Infrastructure/TechChallenge.Infrastructure.csproj", "TechChallenge.Infrastructure/"]
RUN dotnet restore "GetContactService/GetContactService.csproj"

# Copia todo o c�digo-fonte para o cont�iner
COPY . .

# Define o diret�rio de trabalho para a compila��o
WORKDIR "/src/GetContactService"

# Compila a aplica��o
RUN dotnet publish -c Release -o /app/out

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
ENTRYPOINT ["dotnet", "GetContactService.dll"]
