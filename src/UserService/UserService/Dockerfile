# Stage 1: Build the application (using SDK image)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all projects files to restore dependencies across the solution structure
# This is a key step when building multiple services from the root context
COPY ["src/UserService/UserService.csproj", "src/UserService/"]
RUN dotnet restore "src/UserService/UserService.csproj"

# Copy the rest of the code and publish
COPY . .
WORKDIR /src/src/UserService
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Create the final runtime image (using ASP.NET runtime image)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Expose the default ASP.NET Core port (8080 is often used in containerized environments)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080 

# Define the entry point
ENTRYPOINT ["dotnet", "UserService.dll"]