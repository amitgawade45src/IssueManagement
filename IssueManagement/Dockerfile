# -------- Build Stage --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy entire solution
COPY . .

# Restore ONLY the Web project (not solution)
RUN dotnet restore IssueManagement/IssueManagement.csproj

# Publish ONLY the Web project
RUN dotnet publish IssueManagement/IssueManagement.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# -------- Runtime Stage --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "IssueManagement.dll"]