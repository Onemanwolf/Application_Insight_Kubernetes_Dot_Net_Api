docker tag ebddc85e2b20 dockerdemoacr03.azurecr.io/applicationinsightskubernetes:v1

docker build . -t dockerdemoacr03.azurecr.io/applicationinsightskubernetes:v4

docker inspect ebddc85e2b20

docker run -it -rm

docker run -it --rm -p 5000:80 --name app-insights-demo app-insights:v1

docker push dockerdemoacr03.azurecr.io/applicationinsightskubernetes:v4


# Docker Sidecar for Tools

docker build --rm --pull -f "Dockerfile.tools" -t "dockerdemoacr03.azurecr.io/sidecar-monitor:latest" .
docker image push dockerdemoacr03.azurecr.io/sidecar-monitor:latest

k apply -f configure-pod-sidecar-monitor-dotnet-tools.yaml --namespace net-api

$POD=$(kubectl get pods -n net-api -o jsonpath="{.items[0].metadata.name}")
kubectl exec --stdin --tty $POD -n net-api  -- /bin/bash

dotnet-counters collect --process-id 13 --refresh-interval 10 --output /data/counters --format json
dotnet-trace collect -p 13 --format Chromium -o /data/trace.json
az acr login -n dockerdemoacr03.azurecr.io

az aks get-credentials -n AKSCluster-6738d774 -g monitoringaks


Set-Alias -Name k -Value Kubectl
k get nodes
k get po -n monitoring
k edit po app-insights-kubernetes -n monitoring
k config set-context --current --namespace monitoring
k config set-context --current --namespace net-api