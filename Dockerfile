FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

#FROM mcr.microsoft.com/dotnet/sdk:6.0 AS test
#WORKDIR /src
#COPY ["Fedido.Server.Test/Fedido.Server.Test.csproj", "Fedido.Server.Test/"]
#RUN dotnet restore "Fedido.Server.Test/Fedido.Server.Test.csproj"
#COPY . .
#WORKDIR "/src/Fedido.Server.Test"
#RUN dotnet test "Fedido.Server.Test.csproj" -c Release -o /app/test

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Fedido.Server/Fedido.Server.csproj", "Fedido.Server/"]
RUN dotnet restore "Fedido.Server/Fedido.Server.csproj"
COPY . .
WORKDIR "/src/Fedido.Server"
RUN dotnet build "Fedido.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fedido.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fedido.Server.dll"]
