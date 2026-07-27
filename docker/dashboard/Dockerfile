# Dockerfile para OCAP Dashboard (Blazor WebAssembly servido por Nginx)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Dashboard/OCAP.Dashboard/OCAP.Dashboard.csproj", "src/Dashboard/OCAP.Dashboard/"]
RUN dotnet restore "src/Dashboard/OCAP.Dashboard/OCAP.Dashboard.csproj"

COPY . .
WORKDIR "/src/src/Dashboard/OCAP.Dashboard"
RUN dotnet publish "OCAP.Dashboard.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=build /app/publish/wwwroot .
COPY docker/dashboard/nginx.dashboard.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
