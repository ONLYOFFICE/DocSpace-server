FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["common/services/ASC.ClearEvents/ASC.ClearEvents.csproj", "common/services/ASC.ClearEvents/"]
COPY ["common/ASC.Api.Core/ASC.Api.Core.csproj", "common/ASC.Api.Core/"]
COPY ["web/ASC.Web.Core/ASC.Web.Core.csproj", "web/ASC.Web.Core/"]
COPY ["common/ASC.Core.Common/ASC.Core.Common.csproj", "common/ASC.Core.Common/"]
COPY ["common/ASC.Common/ASC.Common.csproj", "common/ASC.Common/"]
COPY ["common/ASC.EventBus/ASC.EventBus.csproj", "common/ASC.EventBus/"]
COPY ["common/ASC.Data.Encryption/ASC.Data.Encryption.csproj", "common/ASC.Data.Encryption/"]
COPY ["common/ASC.Data.Storage/ASC.Data.Storage.csproj", "common/ASC.Data.Storage/"]
COPY ["common/ASC.FederatedLogin/ASC.FederatedLogin.csproj", "common/ASC.FederatedLogin/"]
COPY ["common/ASC.IPSecurity/ASC.IPSecurity.csproj", "common/ASC.IPSecurity/"]
COPY ["common/ASC.MessagingSystem/ASC.MessagingSystem.csproj", "common/ASC.MessagingSystem/"]
COPY ["common/services/ASC.AuditTrail/ASC.AuditTrail.csproj", "common/services/ASC.AuditTrail/"]
COPY ["common/services/ASC.ElasticSearch/ASC.ElasticSearch.csproj", "common/services/ASC.ElasticSearch/"]
COPY ["common/ASC.EventBus.ActiveMQ/ASC.EventBus.ActiveMQ.csproj", "common/ASC.EventBus.ActiveMQ/"]
COPY ["common/ASC.EventBus.Extensions.Logger/ASC.EventBus.Extensions.Logger.csproj", "common/ASC.EventBus.Extensions.Logger/"]
COPY ["common/ASC.EventBus.RabbitMQ/ASC.EventBus.RabbitMQ.csproj", "common/ASC.EventBus.RabbitMQ/"]
COPY ["common/ASC.Webhooks.Core/ASC.Webhooks.Core.csproj", "common/ASC.Webhooks.Core/"]
RUN dotnet restore "common/services/ASC.ClearEvents/ASC.ClearEvents.csproj"
COPY . .
WORKDIR "/src/common/services/ASC.ClearEvents"
RUN dotnet build "ASC.ClearEvents.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ASC.ClearEvents.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ASC.ClearEvents.dll"]
