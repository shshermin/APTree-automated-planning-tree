# Python Planning Service (BW Cloud VM Guide)

First, you need an SSH key to access the VM.

## 1) Connect to the VM

```bash
ssh -i ~/.ssh/id_ed25519 ubuntu@193.196.52.17
```
ssh -i C:\Users\sherk\.ssh\id_ed25519 ubuntu@193.196.52.17

## 1b) SSH Tunnel (for C# execution engine)

The C# execution engine connects to `http://localhost:5000`. To forward that
port from your local machine to the VM, open a separate terminal and run:

```bash

```
ssh -L 5000:localhost:5000 -i C:\Users\sherk\.ssh\id_ed25519 ubuntu@193.196.52.17
Keep this terminal open while running the execution engine.

## 2) Project directory on the VM

```bash
cd APTree-automated-planning-tree/APTreeExecutionEngine/python_service
```

## 3) Python environment and service start

Install packages once:

```bash
sudo apt update
sudo apt install -y python3-venv python3-pip openjdk-17-jre
```

Create the virtual environment once:

```bash
python3 -m venv pddl_env
```

Activate and install dependencies:

```bash
source pddl_env/bin/activate
python -m pip install flask requests
```

Start the service:

```bash
python pddl_planning_service.py
```

## 4) Health check

```bash
curl http://localhost:5000/health
```

## 5) Provide the ENHSP JAR on the VM

The JAR must be present. We use:

```
/home/ubuntu/ENHSP-Public/enhsp.jar
```

If you have it locally, copy it to the VM:

```bash
scp -i ~/.ssh/id_ed25519 /path/to/enhsp.jar ubuntu@193.196.52.17:/home/ubuntu/ENHSP-Public/enhsp.jar
```

## 6) Default ENHSP path in the service

In `pddl_planning_service.py`:

```python
DEFAULT_ENHSP_PATH = "/home/ubuntu/ENHSP-Public/enhsp.jar"
```

## 7) Test planning (ENHSP)

```bash
curl -X POST http://localhost:5000/plan \
  -H "Content-Type: application/json" \
  -d '{
    "planningType":"PDDL",
    "plannerName":"ENHSP",
    "domainFile":"Plannerinputs/static/domain.pddl",
    "problemFile":"Plannerinputs/static/problemC1.pddl",
    "timeoutSeconds":120
  }'
```

## 8) Optional: Planutils (Docker)

Install Docker:

```bash
sudo apt update
sudo apt install -y docker.io
sudo systemctl enable --now docker
```

Start the Planutils container (optional):

```bash
sudo docker run -it --name planutils --privileged aiplanning/planutils:latest bash
```

Inside the container:

```bash
planutils activate
planutils list
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
