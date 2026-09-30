# Stage 1: build and publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CoffeeNChill.Functions.csproj .
RUN dotnet restore "CoffeeNChill.Functions.csproj"

COPY . .
RUN dotnet publish "CoffeeNChill.Functions.csproj" -c Release -o /home/site/wwwroot

# Stage 2: runtime
FROM --platform=linux/amd64 mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
WORKDIR /home/site/wwwroot

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=build /home/site/wwwroot /home/site/wwwroot

EXPOSE 80

