@echo off
set NameSpace=student-r-lehner
set AppPrefix=climbconnectapi

kubectl delete -n %NameSpace% deployment %AppPrefix%
kubectl delete -n %NameSpace% service %AppPrefix%-svc
kubectl delete -n %NameSpace% ingress %AppPrefix%-ingress
kubectl delete -n %NameSpace% pod -l app=%AppPrefix%
kubectl create -f ./backend-leocloud-%NameSpace%-%AppPrefix%-volumeClaim.yaml
kubectl create -f ./backend-leocloud-%NameSpace%-%AppPrefix%.yaml

pause

:: Daten aus Volume kopieren:
:: kubectl cp %NameSpace%/<pod-name>:/app/Data .
