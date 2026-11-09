# Builds one .NET project (Api, Worker or DbMigrator) into a small runtime image.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /src
COPY FarmerSample.slnx Directory.Build.props* ./
COPY src/ src/
RUN dotnet publish src/${PROJECT}/${PROJECT}.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG PROJECT
ENV APP_DLL=${PROJECT}.dll
WORKDIR /app
COPY --from=build /out .
USER $APP_UID
ENTRYPOINT ["sh", "-c", "exec dotnet $APP_DLL"]
