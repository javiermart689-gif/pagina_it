# Dockerfile de despliegue (Railway y otros hosts basados en contenedor).
# La aplicación es .NET 8 (ver OptivosaITManager/OptivosaITManager.csproj y global.json).
# Esta imagen no cambia la lógica de la aplicación ni sus cadenas de conexión: solo
# compila y publica el proyecto tal como está, y expone el puerto que indique la
# variable de entorno PORT en tiempo de ejecución.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaurar solo con el .csproj primero para aprovechar la cache de capas de Docker.
COPY OptivosaITManager/OptivosaITManager.csproj OptivosaITManager/
RUN dotnet restore OptivosaITManager/OptivosaITManager.csproj

# Copiar el resto del código y publicar en Release.
COPY OptivosaITManager/ OptivosaITManager/
WORKDIR /src/OptivosaITManager
RUN dotnet publish OptivosaITManager.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
# Puerto por defecto para pruebas locales (docker run sin -e PORT=...).
# Railway inyecta su propio PORT en tiempo de ejecución y ese valor tiene prioridad.
ENV PORT=8080
EXPOSE 8080

# Forma shell (no exec) para que $PORT se expanda en tiempo de ejecución con el valor
# real que indique la plataforma; --urls es un argumento de línea de comandos estándar
# de Kestrel, no requiere tocar Program.cs.
ENTRYPOINT dotnet OptivosaITManager.dll --urls http://+:$PORT
