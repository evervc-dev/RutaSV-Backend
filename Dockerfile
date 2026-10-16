# Etapa 1: Compilación y Publicación (SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia archivos de configuración global y solución
COPY ["RutaSV.slnx", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY [".editorconfig", "./"]

# Copia los archivos .csproj de cada proyecto y restaura las dependencias
COPY ["src/RutaSV.Domain/RutaSV.Domain.csproj", "src/RutaSV.Domain/"]
COPY ["src/RutaSV.Application/RutaSV.Application.csproj", "src/RutaSV.Application/"]
COPY ["src/RutaSV.Infrastructure/RutaSV.Infrastructure.csproj", "src/RutaSV.Infrastructure/"]
COPY ["src/RutaSV.Api/RutaSV.Api.csproj", "src/RutaSV.Api/"]
COPY ["tests/RutaSV.UnitTests/RutaSV.UnitTests.csproj", "tests/RutaSV.UnitTests/"]
COPY ["tests/RutaSV.IntegrationTests/RutaSV.IntegrationTests.csproj", "tests/RutaSV.IntegrationTests/"]

# Descarga e instala todos los paquetes NuGet
RUN dotnet restore "RutaSV.slnx"

# Copia el resto del código fuente y publica la Web API en modo Release
COPY . .
WORKDIR "/src/src/RutaSV.Api"
RUN dotnet publish "RutaSV.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Imagen Final de Ejecución (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

# Copia únicamente los archivos compilados desde la etapa anterior
COPY --from=build /app/publish .

# Ejecuta la aplicación con un usuario sin privilegios de root por seguridad
USER $APP_UID
ENTRYPOINT ["dotnet", "RutaSV.Api.dll"]