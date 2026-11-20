# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copio primero los .csproj: si no cambian, Docker reutiliza la capa de restore.
COPY ["src/Turnos.API/Turnos.API.csproj", "src/Turnos.API/"]
COPY ["src/Turnos.Application/Turnos.Application.csproj", "src/Turnos.Application/"]
COPY ["src/Turnos.Domain/Turnos.Domain.csproj", "src/Turnos.Domain/"]
COPY ["src/Turnos.Infrastructure/Turnos.Infrastructure.csproj", "src/Turnos.Infrastructure/"]
RUN dotnet restore "src/Turnos.API/Turnos.API.csproj"

COPY . .
RUN dotnet publish "src/Turnos.API/Turnos.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# La imagen aspnet trae un usuario no root llamado "app".
USER app

# Render fija el puerto con la variable PORT (default 10000) y lo usa para el health check.
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "export ASPNETCORE_URLS=http://+:${PORT:-10000}; exec dotnet Turnos.API.dll"]
