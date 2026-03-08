# Docker (Full Image)

This image runs:
- Python planner service on `:5000`
- ASP.NET backend on `:5254`

## Build

```bash
cd /home/ubuntu/APTree-automated-planning-tree
cd APTreeDSL && gradle shadowJar && cd ..
docker build -t aptree:latest .
```

## Run

```bash
docker run -p 5000:5000 -p 5254:5254 aptree:latest
```

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
