FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GOJOR.AdminAPI/GOJOR.AdminAPI.csproj", "GOJOR.AdminAPI/"]
COPY ["GOJOR.Application/GOJOR.Application.csproj", "GOJOR.Application/"]
COPY ["GOJOR.Domain/GOJOR.Domain.csproj", "GOJOR.Domain/"]
COPY ["GOJOR.Persistence/GOJOR.Persistence.csproj", "GOJOR.Persistence/"]
COPY ["GOJOR.Shared/GOJOR.Shared.csproj", "GOJOR.Shared/"]
COPY ["GOJOR.SMS/GOJOR.SMS.csproj", "GOJOR.SMS/"]
RUN dotnet restore "GOJOR.AdminAPI/GOJOR.AdminAPI.csproj"
COPY . .
WORKDIR "/src/GOJOR.AdminAPI"
RUN dotnet build "GOJOR.AdminAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GOJOR.AdminAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GOJOR.AdminAPI.dll"]
