#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

RUN echo "deb https://deb.debian.org/debian/ unstable main contrib non-free" >> /etc/apt/sources.list
RUN echo "deb-src https://deb.debian.org/debian/ unstable main contrib non-free" >> /etc/apt/sources.list
RUN apt-get update
RUN apt-get install -y libleptonica-dev libtesseract-dev tesseract-ocr

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY ["EDSystemProgress/EDSystemProgress.csproj", "EDSystemProgress/"]
RUN dotnet restore "EDSystemProgress/EDSystemProgress.csproj"
COPY . .
WORKDIR "/src/EDSystemProgress"
RUN dotnet build "EDSystemProgress.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EDSystemProgress.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

RUN echo "deb https://deb.debian.org/debian/ unstable main contrib non-free" >> /etc/apt/sources.list
RUN echo "deb-src https://deb.debian.org/debian/ unstable main contrib non-free" >> /etc/apt/sources.list
RUN apt-get update
RUN apt-get install -y libleptonica-dev libtesseract-dev tesseract-ocr

###
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EDSystemProgress.dll"]