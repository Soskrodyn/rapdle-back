# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY ["Rapdle.Api.csproj", ""]
RUN dotnet restore "Rapdle.Api.csproj"
COPY . .
RUN dotnet publish "Rapdle.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5095
ENV ASPNETCORE_URLS=http://+:5095
ENTRYPOINT ["dotnet", "Rapdle.Api.dll"]
