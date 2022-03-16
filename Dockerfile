FROM debian:11.2-slim as base

# Install dependencies
RUN apt-get update && apt-get install -y \
    libgssapi-krb5-2

# Delete apt cache
RUN apt-get clean && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:5.0 as build

# Build mumblebot
WORKDIR /src
COPY . .

RUN dotnet restore && dotnet publish -r linux-x64 -c Release -p:PublishSingleFile=true -o /out

FROM base as run

WORKDIR /mumblebot
COPY --from=build /out/ .


ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

CMD ["/mumblebot/MumbleBot"]