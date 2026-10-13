# Docker (Full Image)

This image runs:
- Python planner service on `:5000`
- ASP.NET backend on `:5254`

## Build

```bash
cd /home/ubuntu/APTree-automated-planning-tree
docker build -t aptree:latest .
```

The MontiCore tool jar for `/api/aptree/validate` is built inside the image; no local Gradle step is needed.
```

## Run

```bash
docker run -p 127.0.0.1:5000:5000 -p 5254:5254 aptree:latest
```

The planner service on `:5000` has no authentication and runs planner
processes, so publish it on the host's loopback only (as above and in
`docker-compose.yml`). Set `PLANNER_DEBUG=1` only for local debugging: it
enables the Werkzeug debugger, which allows remote code execution.

## Test

```bash
curl http://localhost:5000/health
curl http://localhost:5254/health
```

## Compose (optional)

```bash
docker compose up --build
```

## ENHSP JAR

Ensure the JAR exists at:

```
APTreeExecutionEngine/python_service/enhsp.jar
```

It is copied into the image and placed at:

```
/home/ubuntu/ENHSP-Public/enhsp.jar
```
