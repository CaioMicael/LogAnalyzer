FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["LogAnalyzer.sln", "."]
COPY ["LogAnalyzer.Domain/LogAnalyzer.Domain.csproj", "LogAnalyzer.Domain/"]
COPY ["LogAnalyzer.Infrastructure/LogAnalyzer.Infrastructure.csproj", "LogAnalyzer.Infrastructure/"]
COPY ["LogAnalyzer.Application/LogAnalyzer.Application.csproj", "LogAnalyzer.Application/"]
COPY ["LogAnalyzer.Worker/LogAnalyzer.Worker.csproj", "LogAnalyzer.Worker/"]
COPY ["LogAnalyzer.API/LogAnalyzer.API.csproj", "LogAnalyzer.API/"]
RUN dotnet restore "LogAnalyzer.API/LogAnalyzer.API.csproj"

COPY . .
RUN dotnet publish "LogAnalyzer.API/LogAnalyzer.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "LogAnalyzer.API.dll"]
