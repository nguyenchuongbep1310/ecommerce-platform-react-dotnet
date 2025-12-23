# Stage 1: Build the application (using SDK image)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all projects files to restore dependencies
COPY ["src/ProductCatalogService/ProductCatalogService.API/ProductCatalogService.csproj", "src/ProductCatalogService/ProductCatalogService.API/"]
COPY ["src/ProductCatalogService/ProductCatalogService.Application/ProductCatalogService.Application.csproj", "src/ProductCatalogService/ProductCatalogService.Application/"]
COPY ["src/ProductCatalogService/ProductCatalogService.Domain/ProductCatalogService.Domain.csproj", "src/ProductCatalogService/ProductCatalogService.Domain/"]
COPY ["src/ProductCatalogService/ProductCatalogService.Infrastructure/ProductCatalogService.Infrastructure.csproj", "src/ProductCatalogService/ProductCatalogService.Infrastructure/"]
COPY ["src/Shared.Messages/Shared.Messages.csproj", "src/Shared.Messages/"]

RUN dotnet restore "src/ProductCatalogService/ProductCatalogService.API/ProductCatalogService.csproj"

# Copy the rest of the code and publish
COPY . .
WORKDIR /src/src/ProductCatalogService/ProductCatalogService.API
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Create the final runtime image (using ASP.NET runtime image)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080 

# Define the entry point
ENTRYPOINT ["dotnet", "ProductCatalogService.dll"]