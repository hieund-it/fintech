FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files for layer caching
COPY VnStock.sln ./
COPY src/VnStock.Domain/VnStock.Domain.csproj src/VnStock.Domain/
COPY src/VnStock.Application/VnStock.Application.csproj src/VnStock.Application/
COPY src/VnStock.Infrastructure/VnStock.Infrastructure.csproj src/VnStock.Infrastructure/
COPY src/VnStock.API/VnStock.API.csproj src/VnStock.API/
RUN dotnet restore

COPY src/ src/
RUN dotnet publish src/VnStock.API/VnStock.API.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "VnStock.API.dll"]
