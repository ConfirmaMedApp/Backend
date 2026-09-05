# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copiar archivos del proyecto
COPY *.csproj ./
RUN dotnet restore

# Copiar todo y compilar
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copiar archivos publicados
COPY --from=build /app/publish .

# Variables de entorno para ASP.NET Core
ENV ASPNETCORE_ENVIRONMENT=Production

# El puerto se configura dinámicamente
EXPOSE 8080

# Comando de inicio
ENTRYPOINT ["dotnet", "Backend.dll"]