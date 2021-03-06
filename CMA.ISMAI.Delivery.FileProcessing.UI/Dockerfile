#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["CMA.ISMAI.Delivery.FileProcessing.UI/CMA.ISMAI.Delivery.FileProcessing.UI.csproj", "CMA.ISMAI.Delivery.FileProcessing.UI/"]
COPY ["CMA.ISMAI.Delivery.FileProcessing.CrossCutting.Bus/CMA.ISMAI.Delivery.FileProcessing.CrossCutting.Bus.csproj", "CMA.ISMAI.Delivery.FileProcessing.CrossCutting.Bus/"]
COPY ["CMA.ISMAI.Delivery.FileProcessing.CrossCutting.FileMover/CMA.ISMAI.Delivery.FileProcessing.CrossCutting.FileMover.csproj", "CMA.ISMAI.Delivery.FileProcessing.CrossCutting.FileMover/"]
COPY ["CMA.ISMAI.Delivery.FileProcessing.Domain/CMA.ISMAI.Delivery.FileProcessing.Domain.csproj", "CMA.ISMAI.Delivery.FileProcessing.Domain/"]
COPY ["CMA.ISMAI.Delivery.Core/CMA.ISMAI.Core.csproj", "CMA.ISMAI.Delivery.Core/"]
COPY ["CMA.ISMAI.Delivery.EventStore/CMA.ISMAI.Delivery.EventStore.csproj", "CMA.ISMAI.Delivery.EventStore/"]
COPY ["CMA.ISMAI.Delivery.Logging.Log/CMA.ISMAI.Delivery.Logging.csproj", "CMA.ISMAI.Delivery.Logging.Log/"]
COPY ["CMA.ISMAI.Delivery.FileProcessing.CrossCutting.FileReader/CMA.ISMAI.Delivery.FileProcessing.CrossCutting.FileReader.csproj", "CMA.ISMAI.Delivery.FileProcessing.CrossCutting.FileReader/"]
COPY ["CMA.ISMAI.Delivery.FileProcessing.CrossCutting.FileProcessing/CMA.ISMAI.Delivery.FileProcessing.CrossCutting.FileProcessing.csproj", "CMA.ISMAI.Delivery.FileProcessing.CrossCutting.FileProcessing/"]
COPY ["CMA.ISMAI.FileProcessing.CrossCutting.Camunda/CMA.ISMAI.Delivery.FileProcessing.CrossCutting.Camunda.csproj", "CMA.ISMAI.FileProcessing.CrossCutting.Camunda/"]
RUN dotnet restore "CMA.ISMAI.Delivery.FileProcessing.UI/CMA.ISMAI.Delivery.FileProcessing.UI.csproj"
COPY . .
WORKDIR "/src/CMA.ISMAI.Delivery.FileProcessing.UI"
RUN dotnet build "CMA.ISMAI.Delivery.FileProcessing.UI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CMA.ISMAI.Delivery.FileProcessing.UI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CMA.ISMAI.Delivery.FileProcessing.UI.dll"]