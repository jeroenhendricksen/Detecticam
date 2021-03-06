FROM raimondb/opencv-dotnet-runtime-deps AS base
COPY --from=raimondb/yolov3-data ["yolov3.weights", "yolov3.cfg", "coco.names", "/yolo-data/"] 

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-bionic AS build
WORKDIR /src
COPY ["DetectiCam/DetectiCam.csproj", "DetectiCam/"]
COPY ["DetectiCam.Core/DetectiCam.Core.csproj", "DetectiCam.Core/"]
RUN dotnet restore "DetectiCam/DetectiCam.csproj" --runtime ubuntu.18.04-x64
COPY . .
WORKDIR "/src/DetectiCam"
RUN dotnet build "DetectiCam.csproj" -c Release --runtime ubuntu.18.04-x64 -o /app/build

FROM build AS publish
RUN dotnet publish "DetectiCam.csproj" -c Release --runtime ubuntu.18.04-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=true -o /app/publish

FROM base AS final
VOLUME /captures
VOLUME /config
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./DetectiCam", "--configdir=/config"]
