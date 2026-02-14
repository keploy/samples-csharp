# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies first (layer caching)
COPY simpleAPI_v1.csproj ./
RUN dotnet restore simpleAPI_v1.csproj

# Copy the rest of the source code and build
COPY . ./
RUN dotnet publish simpleAPI_v1.csproj -c Release -o /app/publish

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# .NET 8 defaults to port 8080
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "simpleAPI_v1.dll"]
