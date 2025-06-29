FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ./src/v3/v3.csproj ./v3/
COPY ./src/common/common.csproj ./common/
COPY ./src/v3 ./v3
COPY ./src/common ./common
WORKDIR "/src/."
RUN dotnet build "v3/v3.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "v3/v3.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "v3.dll"]
