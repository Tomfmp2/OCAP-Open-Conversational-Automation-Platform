# Dockerfile para OCAP API (Backend ASP.NET Core .NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Api/OCAP.Api/OCAP.Api.csproj", "src/Api/OCAP.Api/"]
COPY ["src/Application/OCAP.Application/OCAP.Application.csproj", "src/Application/OCAP.Application/"]
COPY ["src/Core/OCAP.Core/OCAP.Core.csproj", "src/Core/OCAP.Core/"]
COPY ["src/Infrastructure/OCAP.Infrastructure/OCAP.Infrastructure.csproj", "src/Infrastructure/OCAP.Infrastructure/"]
COPY ["src/Modules/OCAP.Agents.Application/OCAP.Agents.Application.csproj", "src/Modules/OCAP.Agents.Application/"]
COPY ["src/Modules/OCAP.Agents.Domain/OCAP.Agents.Domain.csproj", "src/Modules/OCAP.Agents.Domain/"]
COPY ["src/Modules/OCAP.Agents.Abstractions/OCAP.Agents.Abstractions.csproj", "src/Modules/OCAP.Agents.Abstractions/"]
COPY ["src/Tools/OCAP.Tools.Abstractions/OCAP.Tools.Abstractions.csproj", "src/Tools/OCAP.Tools.Abstractions/"]
COPY ["src/Security/OCAP.Security.Abstractions/OCAP.Security.Abstractions.csproj", "src/Security/OCAP.Security.Abstractions/"]

RUN dotnet restore "src/Api/OCAP.Api/OCAP.Api.csproj"
COPY . .
WORKDIR "/src/src/Api/OCAP.Api"
RUN dotnet build "OCAP.Api.csproj" -c Release -o /app/build
RUN dotnet publish "OCAP.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
HEALTHCHECK --interval=15s --timeout=5s --start-period=40s --retries=5 \
  CMD curl -fsS http://localhost:5000/health/ready || exit 1
ENTRYPOINT ["dotnet", "OCAP.Api.dll"]
