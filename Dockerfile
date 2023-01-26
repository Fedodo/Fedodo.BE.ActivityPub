FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Fedodo.Server/Fedodo.Server.csproj", "Fedodo.Server/"]
RUN dotnet restore "Fedodo.Server/Fedodo.Server.csproj"
COPY . .
WORKDIR "/src/Fedodo.Server"
RUN dotnet build "Fedodo.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fedodo.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fedodo.Server.dll"]
