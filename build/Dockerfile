# syntax=docker/dockerfile:1
# Multi-stage build for the In-Office Hours Assistant bot. Build context is the repo root:
#   docker build -f build/Dockerfile -t teamsbot .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Tfg.HrSystems.EmployeeData.TeamsBot.sln
RUN dotnet publish src/EmployeeData.TeamsBot.Presentation.Bot -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# curl for the healthcheck; a non-root user for the runtime.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && adduser --disabled-password --gecos "" --uid 10001 appuser
COPY --from=build /app .
USER appuser
ENV ASPNETCORE_URLS=http://+:5234
EXPOSE 5234
HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
    CMD curl -fsS http://localhost:5234/healthz || exit 1
ENTRYPOINT ["dotnet", "EmployeeData.TeamsBot.Presentation.Bot.dll"]
