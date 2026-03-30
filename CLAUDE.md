# AGENTS.md - Reglas para Agentes de IA

Este archivo contiene las instrucciones que SIEMPRE debe seguir cualquier agente de IA (Claude Code, OpenCode, o cualquier otro) cuando trabaje en este proyecto.

Lee este archivo completo antes de hacer cualquier cosa.

---

## Quien es el usuario

El usuario es programador. Responde de forma directa y tecnica. No hace falta simplificar explicaciones ni evitar jerga IT. Si tenes una opinion tecnica o arquitectural, compartila.

---

## Reglas obligatorias

### 1. Siempre hacer commits

Cada vez que termines un cambio funcional, hace un commit con un mensaje claro en espanol y push automatico:

```bash
git add -A
git commit -m "Agregar formulario de contacto en el dashboard"
git push
```

No acumules muchos cambios en un solo commit. Un commit por funcionalidad o arreglo. Esto permite deshacer cosas si algo sale mal.

El push se hace automaticamente si hay un GITHUB_TOKEN configurado en `.env`. Si el push falla, avisar al usuario pero continuar trabajando.

### 1.1 Autor de commits

- El autor de commits debe ser **Claude**.
- Antes de commitear, verificar identidad git:

```bash
git config user.name
git config user.email
```

Si no coincide, corregir:

```bash
git config user.name "Claude"
git config user.email "claude@anthropic.com"
```

### 2. Probar antes de decir que esta listo

No digas "listo, funciona" sin haber verificado. Siempre:
- Si tocaste el backend: verifica que compila (`dotnet build`)
- Si tocaste el frontend: recarga el browser y verifica visualmente
- Si tocaste Docker: hace `docker compose up --build -d` y verifica que los containers esten corriendo

### 2.1 Validacion obligatoria de lo que ve el navegador

Siempre hacer estos dos pasos antes de confirmar que un cambio web quedo aplicado (en DESARROLLO):

1. **Rebuild real del servicio web** (no confiar en archivos locales viejos)
   - `docker compose up --build -d web`
2. **Chequeo adentro del contenedor** (confirmar lo que realmente sirve Nginx)
   - ejemplo: `docker compose exec web sh -lc "ls -la /usr/share/nginx/html && grep -n \"texto-clave\" /usr/share/nginx/html/index.html || true"`

Para PRODUCCION, usar los comandos equivalentes con `-f docker-compose.prod.yml` y el servicio `web-prod`.

Si el archivo local dice una cosa pero el contenedor sirve otra, prevalece lo del contenedor.

### 3. No romper lo que ya funciona

Antes de modificar un archivo, leelo primero. Entende que hace antes de cambiarlo. Si tu cambio puede afectar otras partes, revisalas tambien.

### 4. Explicar lo que hiciste

Despues de cada tarea, explica brevemente al usuario:
- Que cambiaste (en palabras simples)
- Por que lo hiciste asi
- Como lo puede ver o probar

Ejemplo: "Agregue una seccion de contacto en la pagina principal. Ahora cuando entres al dashboard vas a ver un formulario donde podes escribir un mensaje. Los mensajes se guardan en la base de datos."

### 4.1 Entornos: Desarrollo y Produccion

El proyecto tiene DOS entornos corriendo en el mismo servidor:

| | Desarrollo | Produccion |
|---|---|---|
| **Puerto** | 3000 | 80 |
| **Rama** | `develop` | `master` |
| **Docker Compose** | `docker-compose.yml` | `docker-compose.prod.yml` |
| **Nginx config** | `nginx/nginx.conf` | `nginx/nginx.prod.conf` |
| **Base de datos** | Separada (container `aicoding-sqlserver`) | Separada (container `aicoding-sqlserver-prod`) |
| **Containers** | `aicoding-*` | `aicoding-*-prod` |

Cada entorno maneja sus propios datos (cuentas de MercadoLibre, usuarios, ordenes, etc). NO comparten base de datos.

### 4.2 Flujo de ramas

- `master`: produccion (puerto 80). Solo recibe merges desde `develop`.
- `develop`: rama de trabajo diario (puerto 3000). Todos los cambios se hacen aca.

**Regla: siempre trabajar en la rama `develop`.** Nunca hacer cambios directos en `master`.

Antes de empezar cualquier tarea, verificar que estas en `develop`:

```bash
git checkout develop
```

### 4.3 Comandos por entorno

**Desarrollo (rama `develop`, puerto 3000):**

```bash
docker compose up --build -d              # Levantar desarrollo
docker compose exec web sh -c "..."       # Verificar dentro del container
docker compose logs api                   # Ver logs de la API
```

**Produccion (rama `master`, puerto 80):**

```bash
docker compose -f docker-compose.prod.yml up --build -d    # Levantar produccion
docker compose -f docker-compose.prod.yml logs api-prod     # Ver logs de la API prod
```

### 4.4 PUBLICAR EN PRODUCCION

Cuando el usuario diga **"PUBLICAR EN PRODUCCION"**, ejecutar estos pasos en orden:

1. Asegurarse de que los cambios en `develop` estan commiteados y pusheados
2. Mergear `develop` a `master` y pushear:
   ```bash
   git checkout master
   git merge develop
   git push
   ```
3. Ejecutar los scripts de base de datos en produccion (init.sql corre automatico al levantar):
   ```bash
   docker compose -f docker-compose.prod.yml up --build -d
   ```
4. Verificar que los containers de produccion estan corriendo:
   ```bash
   docker compose -f docker-compose.prod.yml ps
   ```
5. Volver a la rama de desarrollo:
   ```bash
   git checkout develop
   ```
6. Informar al usuario que la publicacion fue exitosa y que puede verificar en puerto 80

### 5. Usar subagentes y teams agents para tareas grandes

Si el usuario pide algo complejo (mas de 3 archivos o mas de una funcionalidad), dividilo en partes y usa subagentes en paralelo:
- Un agente para el backend (API, base de datos)
- Un agente para el frontend (paginas, estilos)
- Un agente para infraestructura (Docker, nginx) si hace falta

Si tenes disponible **teams agents** (agentes remotos que corren en paralelo), usalos para maximizar la velocidad de ejecucion. Preferir teams agents sobre subagentes locales cuando la tarea lo permita.

Esto es mas rapido y reduce errores.

### 6. No inventar funcionalidades extra

Hace SOLO lo que el usuario pidio. No agregues cosas "por las dudas" o "porque seria buena idea". Si crees que algo seria util, proponelo al usuario primero.

---

## Arquitectura del proyecto

```
ai-coding-environment/
|
|-- docker-compose.yml          <- Desarrollo (puerto 3000)
|-- docker-compose.prod.yml     <- Produccion (puerto 80)
|-- setup.sh                    <- Instalador: prepara la maquina y levanta todo
|-- .env.example                <- Variables de entorno (API keys)
|
|-- src/Api/                  <- Backend (API REST)
|   |-- Program.cs            <- Punto de entrada de la API
|   |-- Controllers/          <- Endpoints (AuthController, DashboardController)
|   |-- Models/               <- Modelos de datos (User.cs)
|   |-- DTOs/                 <- Objetos de transferencia (AuthDtos.cs)
|   |-- Services/             <- Logica de negocio (AuthService.cs)
|   |-- Data/                 <- Base de datos (AppDbContext.cs)
|   |-- Dockerfile            <- Como se construye el container de la API
|   |-- appsettings.json      <- Configuracion (JWT, connection string)
|   '-- Api.csproj            <- Dependencias del proyecto
|
|-- src/Web/                  <- Frontend (Blazor WebAssembly)
|   |-- Program.cs            <- Punto de entrada, registro de servicios
|   |-- App.razor             <- Router y autenticacion
|   |-- _Imports.razor        <- Usings globales
|   |-- Web.csproj            <- Dependencias del proyecto Blazor
|   |-- Dockerfile            <- Build multi-stage (SDK + nginx)
|   |-- landing/              <- Landing page (se sirve en /)
|   |   '-- index.html        <- Pagina de presentacion de integraly.dev
|   |-- Pages/                <- Paginas de la app
|   |   |-- Login.razor       <- Pagina de login
|   |   |-- Dashboard.razor   <- Pagina principal del dashboard
|   |   '-- Config.razor      <- Pagina de configuracion
|   |-- Layout/               <- Layouts de la app
|   |   |-- MainLayout.razor  <- Layout principal (sidebar + topbar)
|   |   '-- LoginLayout.razor <- Layout de login
|   |-- Shared/               <- Componentes reutilizables
|   |   |-- NavItem.razor     <- Item de navegacion del sidebar
|   |   |-- StatCard.razor    <- Tarjeta de estadistica
|   |   |-- ToastContainer.razor <- Notificaciones toast
|   |   |-- SvgIcons.razor    <- Iconos SVG centralizados
|   |   '-- RedirectToLogin.razor <- Redireccion a login
|   |-- Models/               <- Modelos de datos del frontend
|   |   |-- LoginRequest.cs   <- Modelo de login
|   |   |-- AuthResponse.cs   <- Respuesta de autenticacion
|   |   |-- UserDto.cs        <- Datos del usuario
|   |   '-- DashboardStats.cs <- Estadisticas del dashboard
|   |-- Services/             <- Servicios del frontend
|   |   |-- AuthService.cs    <- Manejo de sesion y login (JWT + localStorage)
|   |   |-- JwtAuthStateProvider.cs <- Proveedor de estado de autenticacion
|   |   |-- ApiClient.cs      <- Cliente HTTP con Bearer token
|   |   '-- ToastService.cs   <- Servicio de notificaciones
|   '-- wwwroot/              <- Archivos estaticos
|       |-- index.html        <- Pagina host de Blazor (base href="/panel/")
|       '-- css/app.css       <- Estilos visuales
|
|-- db/
|   '-- init.sql              <- Script que crea las tablas iniciales
|
|-- nginx/
|   |-- nginx.conf            <- Configuracion Nginx desarrollo
|   '-- nginx.prod.conf       <- Configuracion Nginx produccion
|
'-- AGENTS.md                 <- Este archivo
```

### Servicios Docker

**Desarrollo** (`docker compose up --build -d`):

| Servicio | Que hace | Puerto |
|----------|----------|--------|
| sqlserver | Base de datos desarrollo | 1433 (interno) |
| sqlserver-init | Ejecuta init.sql (corre una vez) | - |
| api | Backend .NET 8 | 80 (interno) |
| web | Frontend + Nginx | 3000 |

**Produccion** (`docker compose -f docker-compose.prod.yml up --build -d`):

| Servicio | Que hace | Puerto |
|----------|----------|--------|
| sqlserver-prod | Base de datos produccion | 1433 (interno) |
| sqlserver-init-prod | Ejecuta init.sql (corre una vez) | - |
| api-prod | Backend .NET 8 | 80 (interno) |
| web-prod | Frontend + Nginx | 80 |

### Como se conectan

```
DESARROLLO:
Browser -> localhost:3000 -> Nginx (dev)
                              |-- /            -> Landing page (integraly.dev)
                              |-- /panel/      -> Blazor WASM (panel de admin)
                              |-- /api/        -> Backend .NET (api:80)
                              '-- /swagger     -> Documentacion de la API

PRODUCCION:
Browser -> localhost:80 -> Nginx (prod)
                            |-- /            -> Landing page (integraly.dev)
                            |-- /panel/      -> Blazor WASM (panel de admin)
                            '-- /api/        -> Backend .NET (api-prod:80)
```

### Herramientas AI (se instalan en la maquina con setup.sh)

- **Claude Code** - `claude` (necesita ANTHROPIC_API_KEY)
- **OpenCode** - `opencode` (necesita OPENAI_API_KEY)
- **Codex CLI** - `codex` (necesita OPENAI_API_KEY)
- **Gemini CLI** - `gemini` (necesita GEMINI_API_KEY)

### Tecnologias

- **Backend**: .NET 8 + C# + Entity Framework Core
- **Base de datos**: SQL Server 2022 Express
- **Frontend**: Blazor WebAssembly (.NET 8) + CSS
- **Servidor web**: Nginx
- **Agentes AI**: Claude Code, OpenCode, Codex CLI, Gemini CLI
- **Autenticacion**: JWT (JSON Web Tokens)
- **Contenedores**: Docker + Docker Compose

---

## Como expandir el proyecto

### Agregar una nueva pagina al dashboard

1. Crear el archivo `.razor` en `src/Web/Pages/NuevaPagina.razor`:
   - Agregar `@page "/ruta"` y `@attribute [Authorize]`
   - Inyectar servicios necesarios (`@inject ApiClient Api`)
   - Implementar la UI con HTML y logica en el bloque `@code {}`

2. Agregar la navegacion en `src/Web/Layout/MainLayout.razor`:
   - Agregar un `<NavItem>` en la seccion `sidebar-nav`
   - Agregar el icono en `src/Web/Shared/SvgIcons.razor` si hace falta

3. Si necesita datos del backend, crear el endpoint en la API:
   - Crear el modelo en `src/Api/Models/`
   - Agregar la tabla en `db/init.sql`
   - Agregar el DbSet en `src/Api/Data/AppDbContext.cs`
   - Crear el controller en `src/Api/Controllers/`
   - Agregar la funcion en `src/Web/Services/ApiClient.cs`

4. Si necesita estilos nuevos, agregarlos en `src/Web/wwwroot/css/app.css`

### Agregar una nueva tabla a la base de datos

1. Crear el modelo C# en `src/Api/Models/NuevoModelo.cs`
2. Agregar `DbSet<NuevoModelo>` en `AppDbContext.cs`
3. Agregar `CREATE TABLE` en `db/init.sql`
4. Crear el controller con los endpoints CRUD

### Cambiar el nombre de la marca

Buscar "Tu Marca" en estos archivos y reemplazar:
- `src/Web/wwwroot/index.html` (titulo)
- `src/Web/Pages/Login.razor` (encabezado del login)
- `src/Web/Layout/MainLayout.razor` (sidebar y topbar)

### Agregar un nuevo servicio Docker

1. Crear una carpeta con su `Dockerfile`
2. Agregarlo en `docker-compose.yml` dentro de `services:`
3. Si necesita ser accesible desde el browser, agregar la ruta en `nginx/nginx.conf`

---

## Credenciales por defecto

| Que | Usuario | Contrasena |
|-----|---------|------------|
| Dashboard | admin | admin123 |
| SQL Server | sa | YourStrong@Passw0rd |

---

## Como levantar el proyecto

### Opcion 1: Instalador automatico (recomendado)

```bash
chmod +x setup.sh
./setup.sh
```

Instala todo lo necesario (Node.js, Python, Docker, herramientas AI) y levanta el entorno de desarrollo.

### Opcion 2: Manual

```bash
# 1. Copiar variables de entorno
cp .env.example .env

# 2. (Opcional) Poner tus API keys en .env

# 3. Levantar DESARROLLO (puerto 3000)
docker compose up --build -d

# 4. Levantar PRODUCCION (puerto 80)
docker compose -f docker-compose.prod.yml up --build -d

# 5. Abrir en el browser:
#    Desarrollo: http://localhost:3000 (landing) / http://localhost:3000/panel (panel)
#    Produccion: http://localhost:80 (landing) / http://localhost:80/panel (panel)
```

---

## Resumen para el agente

Cuando el usuario te pida algo:

1. Lee este archivo si no lo leiste
2. Verifica que estas en la rama `develop` (nunca trabajar directo en `master`)
3. Escucha lo que pide y traducilo a tareas tecnicas
4. Dividi en subagentes si es complejo
5. Ejecuta los cambios
6. Probalo en el entorno de desarrollo (puerto 3000)
7. Hace commit en `develop`
8. Explicale al usuario que hiciste, en simple
9. Si el usuario dice **"PUBLICAR EN PRODUCCION"**: mergear `develop` a `master` + rebuild produccion (ver seccion 4.4)
