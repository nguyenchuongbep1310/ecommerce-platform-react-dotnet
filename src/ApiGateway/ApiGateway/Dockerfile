# Stage 1: Build the application (using SDK image)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all projects files to restore dependencies
COPY ["src/ApiGateway/ApiGateway/ApiGateway.csproj", "src/ApiGateway/ApiGateway/"]
RUN dotnet restore "src/ApiGateway/ApiGateway/ApiGateway.csproj"

# Copy the rest of the code and publish
COPY . .
WORKDIR /src/src/ApiGateway/ApiGateway
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Create the final runtime image (using ASP.NET runtime image)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080 

# Define the entry point
ENTRYPOINT ["dotnet", "ApiGateway.dll"]