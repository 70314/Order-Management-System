FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy Directory.Packages.props if it exists
COPY ["Directory.Packages.props", "."]

# Copy project files
COPY ["src/OMS.API/OMS.API.csproj", "src/OMS.API/"]
COPY ["src/OMS.Application/OMS.Application.csproj", "src/OMS.Application/"]
COPY ["src/OMS.Infrastructure/OMS.Infrastructure.csproj", "src/OMS.Infrastructure/"]
COPY ["src/OMS.Domain/OMS.Domain.csproj", "src/OMS.Domain/"]

# Restore dependencies
RUN dotnet restore "src/OMS.API/OMS.API.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/src/OMS.API"

# Build and publish
RUN dotnet publish "OMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5001

ENV ASPNETCORE_URLS=http://0.0.0.0:5001
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "OMS.API.dll"]