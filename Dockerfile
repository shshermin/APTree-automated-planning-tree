FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY . .
WORKDIR /src/APTreeExecutionEngine
RUN dotnet restore BehaviorTreeMainProject.csproj
RUN dotnet publish BehaviorTreeMainProject.csproj -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:8.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 python3-pip python3-flask python3-requests openjdk-17-jre bash curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish /app

# MontiCore tool + DSL (required for /api/aptree/validate)
COPY APTreeDSL /app/APTreeDSL

# Python planner service + inputs
COPY APTreeExecutionEngine/python_service /opt/python_service

# Place ENHSP JAR at the path expected by the service
RUN mkdir -p /home/ubuntu/ENHSP-Public \
    && if [ -f /opt/python_service/enhsp.jar ]; then cp /opt/python_service/enhsp.jar /home/ubuntu/ENHSP-Public/enhsp.jar; fi

COPY docker/start.sh /start.sh
RUN chmod +x /start.sh

EXPOSE 5000 5254

ENTRYPOINT ["/start.sh"]
