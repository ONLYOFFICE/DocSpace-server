FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5027

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Packages.props", "."]
COPY ["common/services/ASC.Monolith/ASC.Monolith.csproj", "common/services/ASC.Monolith/"]
# Core
COPY ["common/ASC.Api.Core/ASC.Api.Core.csproj", "common/ASC.Api.Core/"]
COPY ["common/ASC.Common/ASC.Common.csproj", "common/ASC.Common/"]
COPY ["common/ASC.Core.Common/ASC.Core.Common.csproj", "common/ASC.Core.Common/"]
COPY ["common/ASC.Data.Backup.Core/ASC.Data.Backup.Core.csproj", "common/ASC.Data.Backup.Core/"]
COPY ["common/ASC.Data.Storage/ASC.Data.Storage.csproj", "common/ASC.Data.Storage/"]
COPY ["common/ASC.Data.Encryption/ASC.Data.Encryption.csproj", "common/ASC.Data.Encryption/"]
COPY ["common/ASC.Data.Reassigns/ASC.Data.Reassigns.csproj", "common/ASC.Data.Reassigns/"]
COPY ["common/ASC.EventBus/ASC.EventBus.csproj", "common/ASC.EventBus/"]
COPY ["common/ASC.EventBus.ActiveMQ/ASC.EventBus.ActiveMQ.csproj", "common/ASC.EventBus.ActiveMQ/"]
COPY ["common/ASC.EventBus.Extensions.Logger/ASC.EventBus.Extensions.Logger.csproj", "common/ASC.EventBus.Extensions.Logger/"]
COPY ["common/ASC.EventBus.InMemory/ASC.EventBus.InMemory.csproj", "common/ASC.EventBus.InMemory/"]
COPY ["common/ASC.EventBus.RabbitMQ/ASC.EventBus.RabbitMQ.csproj", "common/ASC.EventBus.RabbitMQ/"]
COPY ["common/ASC.EventBus.RedisMQ/ASC.EventBus.RedisMQ.csproj", "common/ASC.EventBus.RedisMQ/"]
COPY ["common/ASC.FederatedLogin/ASC.FederatedLogin.csproj", "common/ASC.FederatedLogin/"]
COPY ["common/ASC.IPSecurity/ASC.IPSecurity.csproj", "common/ASC.IPSecurity/"]
COPY ["common/ASC.MessagingSystem/ASC.MessagingSystem.csproj", "common/ASC.MessagingSystem/"]
COPY ["common/ASC.Migration/ASC.Migration.Core.csproj", "common/ASC.Migration/"]
COPY ["common/ASC.ActiveDirectory/ASC.ActiveDirectory.csproj", "common/ASC.ActiveDirectory/"]
COPY ["common/ASC.Webhooks.Core/ASC.Webhooks.Core.csproj", "common/ASC.Webhooks.Core/"]
# Services
COPY ["common/services/ASC.AuditTrail/ASC.AuditTrail.csproj", "common/services/ASC.AuditTrail/"]
COPY ["common/services/ASC.ElasticSearch/ASC.ElasticSearch.csproj", "common/services/ASC.ElasticSearch/"]
COPY ["common/services/ASC.Data.Backup/ASC.Data.Backup.csproj", "common/services/ASC.Data.Backup/"]
COPY ["common/services/ASC.Data.Backup.Worker/ASC.Data.Backup.Worker.csproj", "common/services/ASC.Data.Backup.Worker/"]
COPY ["common/services/ASC.Notify/ASC.Notify.csproj", "common/services/ASC.Notify/"]
COPY ["common/services/ASC.Studio.Notify/ASC.Studio.Notify.csproj", "common/services/ASC.Studio.Notify/"]
COPY ["common/services/ASC.ClearEvents/ASC.ClearEvents.csproj", "common/services/ASC.ClearEvents/"]
COPY ["common/services/ASC.TelegramService/ASC.TelegramService.csproj", "common/services/ASC.TelegramService/"]
COPY ["common/services/ASC.ApiSystem/ASC.ApiSystem.csproj", "common/services/ASC.ApiSystem/"]
# Web
COPY ["web/ASC.Web.Core/ASC.Web.Core.csproj", "web/ASC.Web.Core/"]
COPY ["web/ASC.Web.Api/ASC.Web.Api.csproj", "web/ASC.Web.Api/"]
COPY ["web/ASC.Web.Studio/ASC.Web.Studio.csproj", "web/ASC.Web.Studio/"]
# Products
COPY ["products/ASC.Files/Core/ASC.Files.Core.csproj", "products/ASC.Files/Core/"]
COPY ["products/ASC.Files/Server/ASC.Files.csproj", "products/ASC.Files/Server/"]
COPY ["products/ASC.Files/Worker/ASC.Files.Worker.csproj", "products/ASC.Files/Worker/"]
COPY ["products/ASC.People/Server/ASC.People.csproj", "products/ASC.People/Server/"]
COPY ["products/ASC.AI/Core/ASC.AI.Core.csproj", "products/ASC.AI/Core/"]
COPY ["products/ASC.AI/Server/ASC.AI.csproj", "products/ASC.AI/Server/"]
COPY ["products/ASC.AI/Worker/ASC.AI.Worker.csproj", "products/ASC.AI/Worker/"]
# Thirdparty
COPY ["thirdparty/", "thirdparty/"]
RUN dotnet restore "common/services/ASC.Monolith/ASC.Monolith.csproj"
COPY . .
WORKDIR "/src/common/services/ASC.Monolith"
RUN dotnet build "ASC.Monolith.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ASC.Monolith.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ASC.Monolith.dll"]
