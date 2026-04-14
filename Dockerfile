# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["DkpSystem/DkpSystem.csproj", "DkpSystem/"]
RUN dotnet restore "DkpSystem/DkpSystem.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/DkpSystem"
RUN dotnet build "DkpSystem.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DkpSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published app
COPY --from=publish /app/publish .

# Copy migrations folder explicitly
COPY --from=build /src/DkpSystem/Migrations ./Migrations

# Create directory for Data Protection keys (will be mounted as volume in production)
RUN mkdir -p /app/DataProtection-Keys && chmod 755 /app/DataProtection-Keys

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "DkpSystem.dll"]
