# ---
# First stage (build)
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# Copy solution as distinct layer
COPY DiaryCollector.sln .
COPY DiaryCollector/DiaryCollector.csproj ./DiaryCollector/
COPY DiaryCollector/DiaryCollector.csproj ./DiaryCollector/
RUN dotnet restore

# Copy everything else and build
COPY DiaryCollector/. ./DiaryCollector/
WORKDIR /app/DiaryCollector
RUN dotnet publish -c Release -o out

# ---
# Second stage (execution)
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime

WORKDIR /app
COPY --from=build /app/DiaryCollector/out ./

# Run on localhost:8779
ENV ASPNETCORE_URLS http://+:8779
EXPOSE 8779

# Drop privileges
USER 1000

ENTRYPOINT ["dotnet", "DiaryCollector.dll"]
