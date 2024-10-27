# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory in the container
WORKDIR /app

# Copy the project file and restore any dependencies
COPY biddingServer.csproj ./
RUN dotnet restore biddingServer.csproj

# Copy the rest of the application code
COPY . ./

# Build the application
RUN dotnet publish biddingServer.csproj -c Release -o out

# Use a lightweight runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Set the working directory in the runtime container
WORKDIR /app

# Copy the built files from the build container
COPY --from=build /app/out .

# Expose the port your application runs on
EXPOSE 5260

# Define the entry point for the container
ENTRYPOINT ["dotnet", "biddingServer.dll"]
