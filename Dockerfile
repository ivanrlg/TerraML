# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY FuzzySat.slnx .
COPY src/FuzzySat.Core/FuzzySat.Core.csproj src/FuzzySat.Core/
COPY src/FuzzySat.CLI/FuzzySat.CLI.csproj src/FuzzySat.CLI/
COPY src/FuzzySat.Web/FuzzySat.Web.csproj src/FuzzySat.Web/
RUN dotnet restore src/FuzzySat.Web/FuzzySat.Web.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/FuzzySat.Web/FuzzySat.Web.csproj \
    -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FuzzySat.Web.dll"]
