FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["v3.csproj", "."]
RUN dotnet restore "./v3.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "v3.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "v3.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "v3.dll"]
