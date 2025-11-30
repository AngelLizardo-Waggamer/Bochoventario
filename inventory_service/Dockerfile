# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["inventory_service.csproj", "."]
RUN dotnet restore "inventory_service.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "inventory_service.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "inventory_service.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app
COPY --from=publish /app/publish .

# Change ownership of app files
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "inventory_service.dll"]