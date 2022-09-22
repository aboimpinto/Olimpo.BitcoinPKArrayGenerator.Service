FROM mcr.microsoft.com/dotnet/sdk:6.0 as build
WORKDIR /source
ADD Olimpo.BitcoinPKArrayGenerator.Service/ /source

RUN dotnet restore && \
    dotnet publish --configuration Release --output /app --no-restore


FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Olimpo.BitcoinPKArrayGenerator.Service"]