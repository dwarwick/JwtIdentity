# Build environment for JwtIdentity using .NET 9 SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /workspace
COPY . .

# Restore project dependencies as part of the image build
RUN dotnet restore

