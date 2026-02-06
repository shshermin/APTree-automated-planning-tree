# Navigate to your project folder
cd /mnt/c/Users/sherk/Documents/BehaviorTreeMainProject/BehaviorTreeMainProject/python_service


# Start backend (ASP.NET Core)

## Standard (uses the port from Properties/launchSettings.json, currently 5254)
cd BehaviorTreeMainProject
dotnet run --project BehaviorTreeMainProject.csproj

## Set port explicitly (important: Vite proxy expects http://localhost:5254)
dotnet run --project BehaviorTreeMainProject.csproj --urls http://localhost:5254

## URLs
- Health:  http://localhost:5254/health
- Swagger: http://localhost:5254/swagger
- Catalogs:
    - http://localhost:5254/api/catalog/decorators
    - http://localhost:5254/api/catalog/services
    - http://localhost:5254/api/catalog/flows


# MontiCore APTree tool (for .bt import/validation)

The backend endpoint `/api/aptree/validate` executes the MontiCore tool jar.

Build/update the jar:
cd MontiCoreTool
gradle shadowJar


    source pddl_env/bin/activate

       python pddl_planning_service.py



# start the planutils docker image


#fixing docker deamon
sudo systemctl status docker
sudo systemctl start docker
sudo dockerd &

# start the docker
docker start  stupefied_hellman

planutils activate


To set up the python planning service, the following steps are required:
1.  Activate the virtual environment: Open your WSL terminal and navigate to the python_service directory, then activate the virtual environment:

###commands:
cd /mnt/c/Users/sherk/Documents/BehaviorTreeMainProject/BehaviorTreeMainProject/python_service
source pddl_env/bin/activate

2. Start PDDL Planning Service:

###Commands:
python pddl_planning_service.py


Make sure to have dependencies installed: 
###Commands:
pip install flask requests