FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY LocalPress.sln ./
COPY src/LocalPress.Core/LocalPress.Core.csproj src/LocalPress.Core/
COPY src/LocalPress.Infrastructure/LocalPress.Infrastructure.csproj src/LocalPress.Infrastructure/
COPY src/LocalPress.Api/LocalPress.Api.csproj src/LocalPress.Api/
RUN dotnet restore src/LocalPress.Api/LocalPress.Api.csproj
COPY src/ src/
RUN dotnet publish src/LocalPress.Api/LocalPress.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "LocalPress.Api.dll"]
