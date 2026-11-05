# Starter: Tournament Services en Fedora + kind (Kubernetes)

Este paquete está adaptado a `NDMeillon/Tournaments-Cloud-Computing-2026-7C` rama `main`, revisada el 4 de noviembre de 2026. **No** despliega el proyecto del profesor ni su `db_script.sql`: el alumno usa EF Core relacional y la API del profesor usa UUID + JSONB. Este paquete es para una demo local, no para producción. No se ha ejecutado contra tu Fedora real.

## 1. Preparar clones (en Fedora, Bash)

```bash
mkdir -p ~/proyectos && cd ~/proyectos
git clone https://github.com/NDMeillon/Tournaments-Cloud-Computing-2026-7C.git
git clone --branch convert-to-csharp --single-branch https://github.com/ttimaizumi/tournaments.git tournaments-profesor
cd Tournaments-Cloud-Computing-2026-7C
```

Copiar `Containerfile`, `.containerignore` y la carpeta `kubernetes/` de este paquete **a la raíz de tu proyecto** (al lado de `docker-compose.yml`). El repositorio del profesor se mantiene separado como referencia.

## 2. Verificar clúster ya creado

```bash
kind get clusters
kubectl config current-context
kubectl get nodes
kubectl get storageclass
```

Si el nombre del clúster es `kind`, el contexto será normalmente `kind-kind`. No borres un clúster que ya tiene datos. No necesitas aplicar el `kind.yml` del profesor a un clúster existente: para la API usaremos `kubectl port-forward`.

## 3. Desplegar PostgreSQL 16 (tu modelo)

**No levantes a la vez la DB de Docker Compose en el puerto 5432** si quieres usar la DB de kind mediante redirección. Este StatefulSet mantiene datos en un PVC y EF Core crea las tablas al arrancar la API.

```bash
kubectl create namespace tournaments --dry-run=client -o yaml | kubectl apply -f -
kubectl apply -f kubernetes/postgres.yml
kubectl -n tournaments rollout status statefulset/tournament-db --timeout=180s
kubectl -n tournaments get pods,pvc,svc
```

## 4. Construir la imagen C# y cargarla a kind

En Fedora con **Podman**:

```bash
podman build -f Containerfile -t localhost/tournaments-api:dev .
podman save --format docker-archive -o /tmp/tournaments-api-dev.tar localhost/tournaments-api:dev
kind load image-archive /tmp/tournaments-api-dev.tar --name kind
rm /tmp/tournaments-api-dev.tar
```

Si kind se creó con `KIND_EXPERIMENTAL_PROVIDER=podman`, usa también `KIND_EXPERIMENTAL_PROVIDER=podman` para el comando `kind load`. Si se creó con otro nombre, reemplaza `kind` por el nombre real. Alternativa **Docker**:

```bash
docker build -f Containerfile -t localhost/tournaments-api:dev .
kind load docker-image localhost/tournaments-api:dev --name kind
```

La imagen compilada contiene el SDK sólo en la fase de construcción. La API sigue necesitando el runtime .NET 9 por ahora. Más adelante hay que migrar a .NET 10.

## 5. Desplegar la API

```bash
kubectl apply -f kubernetes/app-config.yml
kubectl apply -f kubernetes/api.yml
kubectl -n tournaments rollout status deployment/tournaments-api --timeout=180s
kubectl -n tournaments get pods,services
kubectl -n tournaments logs deployment/tournaments-api --tail=100
```

**Si no ves el rollout exitoso**, inspecciona los logs y el Pod antes de seguir.

## 6. Probar la API

En otra terminal:

```bash
kubectl -n tournaments port-forward service/tournaments-api 8080:8080
```

En otra terminal:

```bash
curl -i http://localhost:8080/health
curl -i http://localhost:8080/teams
```

Debes obtener `Services running` y luego `[]` si aún no hay equipos. En el navegador abre `http://localhost:8080/` (demo) y `http://localhost:8080/swagger`.

`/health` prueba que el proceso responde; `/teams` permite comprobar también la consulta a PostgreSQL.

## 7. Verificar datos en PostgreSQL

```bash
kubectl -n tournaments exec tournament-db-0 -- psql -U tournament_svc -d tournament_db -c '\dt'
```

La migración de EF debería generar `teams`, `tournaments`, `groups`, `matches` y `group_teams`, además de la tabla de historial de migraciones.

## 8. Escalado HPA, después de validar la API

Solo en un clúster Kubernetes 1.31+:

```bash
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/download/v0.8.0/components.yaml
kubectl -n kube-system patch deployment metrics-server --type=json -p='[{"op":"add","path":"/spec/template/spec/containers/0/args/-","value":"--kubelet-insecure-tls"}]'
kubectl -n kube-system rollout status deployment/metrics-server --timeout=180s
kubectl wait --for=condition=Available apiservice/v1beta1.metrics.k8s.io --timeout=180s
kubectl top pods -n tournaments
kubectl apply -f kubernetes/api-hpa.yml
kubectl get hpa -n tournaments -w
```

El flag `--kubelet-insecure-tls` es sólo para un kind de laboratorio. No confirma escalado a 3 réplicas: únicamente configura el control de autoescalado. Para demostrarlo hay que generar carga y observar CPU y número de Pods.

## 9. Después de editar código

```bash
podman build -f Containerfile -t localhost/tournaments-api:dev .
podman save --format docker-archive -o /tmp/tournaments-api-dev.tar localhost/tournaments-api:dev
kind load image-archive /tmp/tournaments-api-dev.tar --name kind
rm /tmp/tournaments-api-dev.tar
kubectl -n tournaments rollout restart deployment/tournaments-api
kubectl -n tournaments rollout status deployment/tournaments-api --timeout=180s
```

## 10. Guardar cambios sólo en tu repositorio

```bash
git status
git add Containerfile .containerignore kubernetes
git commit -m 'Add local kind Kubernetes deployment for Tournament Services'
git push origin main
```

Cuidado: el pre-commit del proyecto puede ejecutar tests y requerir .NET 10 instalado. En ningún caso hagas `git push` al repositorio del profesor.

## Problemas comunes

- `ErrImageNeverPull`: la imagen no se importó al clúster correcto, o el tag difiere.
- `CrashLoopBackOff`: inspecciona `kubectl -n tournaments logs deployment/tournaments-api --previous` y la cadena de conexión.
- PostgreSQL `Pending`: revisa StorageClass y PVC (`kubectl -n tournaments describe pvc data-tournament-db-0`).
- `Connection refused` al `curl`: mantén port-forward en otra terminal y confirma Pod y Service.
- API responde a `/health`, pero `/teams` da error: revisar PostgreSQL y migraciones; health no comprueba la base.
- Si tienes aplicada la DB del profesor **en el mismo namespace**, no la reemplaces ni intentes ejecutar tus migraciones encima: usan esquemas incompatibles. Usa un namespace separado o consulta cómo migrar.

## Pendientes para la versión final

- Pasar API, Domain, Infrastructure y tests a .NET 10 con paquetes compatibles.
- Reubicar migraciones EF Core en un Job/init explícito antes de escalar la API.
- Configurar secrets de forma segura en lugar de la contraseña didáctica.
- Si lo exige el profesor: integrar ActiveMQ con eventos reales + Consumer, y adaptar Locust a los endpoints de tu API.
- Añadir Prometheus/OpenTelemetry, dashboards Grafana y un ensayo de HPA con evidencias.
- Integrar pipeline Jenkins/GitHub Actions para contenedor y despliegue, si figura en rúbrica.
