# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia arquivos de projeto e restaura dependências
COPY ["CreateContactService/CreateContactService.csproj", "CreateContactService/"]
COPY ["TechChallenge.Application/TechChallenge.Application.csproj", "TechChallenge.Application/"]
COPY ["TechChallenge.Domain/TechChallenge.Domain.csproj", "TechChallenge.Domain/"]
COPY ["TechChallenge.Infrastructure/TechChallenge.Infrastructure.csproj", "TechChallenge.Infrastructure/"]

RUN dotnet restore "CreateContactService/CreateContactService.csproj"

# Copia o código-fonte restante e compila a aplicação
COPY . .
WORKDIR "/src/CreateContactService"
RUN dotnet publish -c Release -o /app/out

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copia os arquivos compilados
COPY --from=build /app/out .

# Definir ambiente de produção
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

# Expor a porta do microsserviço
EXPOSE 5000

# Inicia o microsserviço
ENTRYPOINT ["dotnet", "CreateContactService.dll"]
