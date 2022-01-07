FROM mcr.microsoft.com/dotnet/aspnet:5.0-focal AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

ENV ASPNETCORE_URLS=http://+:80
ENV InstrumentationKey=3345206c-f51e-43c6-8f0c-55867e5d3684

FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /src
COPY Application_Insight_Kubernetes_Dot_Net_Api/AppInsightsKubernetes.csproj ./Application_Insight_Kubernetes_Dot_Net_Api/
RUN dotnet restore Application_Insight_Kubernetes_Dot_Net_Api/AppInsightsKubernetes.csproj
COPY . .
WORKDIR /src/Application_Insight_Kubernetes_Dot_Net_Api
RUN dotnet build AppInsightsKubernetes.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish AppInsightsKubernetes.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AppInsightsKubernetes.dll"]
