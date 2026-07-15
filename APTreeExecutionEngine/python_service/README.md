# PDDL Planning Service

A REST API service that invokes PDDL planners (ENHSP, FF, LAMA-FIRST, TFD) and returns plan results.

## Requirements

| Dependency | Version | Purpose |
|---|---|---|
| Python | 3.10+ | Runtime |
| Java JRE | 17+ | ENHSP planner execution |
| Docker | 24.0+ | (Optional) planutils-based planners (FF, LAMA-FIRST) |

## Setup

### 1. Create and activate a virtual environment

```bash
cd APTreeExecutionEngine/python_service

python3 -m venv pddl_env
source pddl_env/bin/activate          # Linux/macOS
# pddl_env\Scripts\activate           # Windows
```

### 2. Install dependencies

```bash
pip install -r requirements.txt
```

### 3. Provide the ENHSP JAR

Place the ENHSP JAR at the path configured in `pddl_planning_service.py`:

```python
DEFAULT_ENHSP_PATH = "/home/ubuntu/ENHSP-Public/enhsp.jar"
```

Or pass the path via the API request body (`plannerPath` field).

For Docker deployment, place `enhsp.jar` in this directory and it will be copied into the container automatically.

## Running the Service

```bash
python pddl_planning_service.py
```

The service listens on port **5000**.

## Health Check

```bash
curl http://localhost:5000/health
```

## Supported Planners

| Planner | Type | Backend |
|---|---|---|
| ENHSP | Numeric / temporal | Local JAR (Java) |
| FF | Classical | planutils Docker container |
| LAMA-FIRST | Satisficing | planutils Docker container |
| TFD | Temporal | planutils Docker container |

## Example Request

```bash
curl -X POST http://localhost:5000/plan \
  -H "Content-Type: application/json" \
  -d '{
    "planningType": "PDDL",
    "plannerName": "ENHSP",
    "domainFile": "Plannerinputs/static/domain.pddl",
    "problemFile": "Plannerinputs/static/problemC1.pddl",
    "timeoutSeconds": 120
  }'
```

## Docker-based Planners (FF, LAMA-FIRST)

These require the `planutils` container:

```bash
# Install Docker (if needed)
sudo apt install -y docker.io
sudo systemctl enable --now docker

# Start the planutils container
docker run -d --name planutils --privileged aiplanning/planutils:latest tail -f /dev/null
docker start planutils
```

## 9) Copy domain/problem files into the Planutils container

On the VM (not inside the container):

```bash
container=planutils

sudo docker exec -i "$container" bash -c "cat > /root/domain.pddl" \
  < Plannerinputs/static/domain.pddl

sudo docker exec -i "$container" bash -c "cat > /root/problemC1.pddl" \
  < Plannerinputs/static/problemC1.pddl
```

Test ENHSP (inside the container, optional):

```bash
planutils activate
apt update
apt install -y openjdk-17-jre
java -jar /root/enhsp.jar -o /root/domain.pddl -f /root/problemC1.pddl -planner pt-blind
```

Note: Some planners (like FF and LAMA-FIRST) do not support `:functions` or numeric preconditions (e.g., `>=`). If your PDDL uses those, ENHSP is the recommended planner.



terminal 1:
ssh -L 5000:localhost:5000 -i C:\Users\sherk\.ssh\id_ed25519 ubuntu@193.196.52.17
docker start planutils

Terminal 2: 
ssh -i C:\Users\sherk\.ssh\id_ed25519 ubuntu@193.196.52.17
cd APTree-automated-planning-tree/APTreeExecutionEngine/python_service
source pddl_env/bin/activate
python pddl_planning_service.py




 Enter the container
sudo docker exec -it planutils bash

# Inside the container:
planutils activate