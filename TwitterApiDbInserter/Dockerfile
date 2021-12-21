FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TwitterTest/TwitterTest.csproj", "TwitterTest/"]
RUN dotnet restore "TwitterTest/TwitterTest.csproj"
COPY . .
WORKDIR "/src/TwitterTest"
RUN dotnet build "TwitterTest.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TwitterTest.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TwitterTest.dll"]
