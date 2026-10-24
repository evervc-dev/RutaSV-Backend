# Guía de Inicio Rápido y Desarrollo Local: RutaSV Backend

¡Bienvenido al equipo de desarrollo de **RutaSV**! Esta guía está pensada para que cualquier miembro del equipo, sin importar su nivel de experiencia previa con Docker o contenedores, pueda clonar el proyecto, configurarlo en pocos minutos y empezar a programar con recarga en vivo de cambios y dependencias completamente resueltas.

---

## 1. Conceptos Clave (Explicación para Principiantes)

Antes de teclear comandos, entendamos las herramientas que usaremos a diario para que no parezcan una "caja negra":

* **¿Qué es Docker?**  
  Imagina que en lugar de instalar PostgreSQL, herramientas de correo y servidores de archivos directamente en tu sistema operativo (lo que suele causar el clásico problema de *"en mi máquina sí funciona"*), Docker empaqueta cada herramienta en una "cajita de arena" aislada llamada **Contenedor**.
* **¿Qué es Docker Compose?**  
  Es un director de orquesta. Con un solo comando (`docker compose up`), levanta y conecta entre sí todos los contenedores que la aplicación necesita: la base de datos (**PostgreSQL 18**), el gestor de imágenes (**MinIO**), el simulador de correos (**Mailpit**) y nuestra **API de .NET 10**.
* **¿Qué es Dev Containers (VS Code)?**  
  Es una tecnología que permite que Visual Studio Code se ejecute directamente "dentro" del contenedor de la API (`rutasv-api-dev`). Al hacerlo, el editor no utiliza el SDK ni las librerías de tu máquina host, sino las que ya están instaladas y configuradas dentro del contenedor Linux. Esto elimina por completo los errores de dependencias no encontradas, los subrayados rojos de imports (`using`) y los conflictos de permisos entre tu usuario y Docker.
* **¿Qué es Hot Reload (`dotnet watch`)?**  
  Gracias a la configuración de volúmenes, la carpeta con tu código en tu computadora se refleja dentro del contenedor. Cuando edites un archivo `.cs` y guardes los cambios (`Ctrl + S`), el contenedor detectará el cambio y recompilará la API en caliente sin que tengas que reiniciar nada manualmente.
* **¿Qué es el archivo `.env`?**  
  Es un archivo de texto simple donde se guardan variables de configuración (puertos, nombres de usuario, contraseñas). Sirve para que nadie suba contraseñas reales al repositorio de Git por accidente.

---

## 2. Requisitos Previos en tu Computadora

Antes de empezar, asegúrate de tener instalado en tu máquina:

1. **Git:** Para clonar y manejar el control de versiones ([Descargar Git](https://git-scm.com/)).
2. **Docker Desktop** (en Windows/Mac) o **Docker Engine + Docker Compose Plugin** (en Linux). Asegúrate de que el servicio de Docker esté corriendo.
3. **Editor de código recomendado:** Visual Studio Code ([Descargar VS Code](https://code.visualstudio.com/)) con las siguientes extensiones oficiales:
   * **Dev Containers** (`ms-vscode-remote.remote-containers`).
   * *(Opcional en tu host si no usas Dev Containers)*: C# Dev Kit (`ms-dotnettools.csdevkit`).
4. **Opcional (Solo si vas a depurar en local fuera de Docker):** SDK de .NET 10 y JetBrains Rider o Visual Studio 2022/2026.

---

## 3. Paso a Paso: Desde Cero hasta el Primer "Hola Mundo"

Sigue estos pasos en tu terminal (PowerShell, Bash o WSL2):

### Paso 1: Clonar el repositorio
Abre tu terminal y clona el proyecto en la carpeta de tu preferencia:

```bash
git clone https://github.com/evervc-dev/RutaSV-Backend.git
cd RutaSV-Backend
```

---

### Paso 2 (PRIMORDIAL): Cambiar de rama a tu espacio de trabajo
> ⚠️ **Regla de equipo:** Nunca programes directamente sobre la rama `main` ni `develop`. Cada tarea, historia de usuario o bugfix debe trabajarse en su propia rama descriptiva.

Antes de configurar o abrir el entorno, actualiza las referencias remotas y cámbiate a la rama asignada (o crea tu nueva rama a partir de `develop`):

* **Si tu rama ya fue creada en GitHub:**
  ```bash
  git fetch origin
  git checkout nombre-de-tu-rama
  git pull origin nombre-de-tu-rama
  ```

* **Si vas a iniciar una nueva tarea (ejemplo HU-02):**
  ```bash
  git checkout develop
  git pull origin develop
  git checkout -b feature/HU-02-descripcion-corta
  ```

---

### Paso 3: Crear tu archivo de variables locales (`.env`)
El repositorio incluye una plantilla llamada `.env.example`. Debes hacer una copia de este archivo y nombrarla `.env`:

* **En Linux / macOS / Git Bash:**
  ```bash
  cp .env.example .env
  ```
* **En Windows (PowerShell):**
  ```powershell
  Copy-Item .env.example .env
  ```

> 💡 **Nota:** Para empezar a trabajar **solo necesitas asignar los campos que no tienen valor** dentro del `.env`. Ya hay valores predeterminados para algunos servicios, pero hay ciertos campos vacíos (como credenciales de Google OAuth `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET` y otros), cuando asignes esos valores, el entorno ya estará listo para funcionar en local. Este archivo está en el `.gitignore`, por lo que tus configuraciones personales nunca se subirán a GitHub.

---

### Paso 4: Abrir el proyecto con Dev Containers (Forma Oficial del Equipo)

Esta es la forma estándar para evitar cualquier problema de librerías, dependencias o permisos de compilación:

1. Abre la carpeta del proyecto en **Visual Studio Code**:
   ```bash
   code .
   ```
2. Al abrir la carpeta, VS Code detectará el archivo `.devcontainer/devcontainer.json` y mostrará una notificación en la esquina inferior derecha:
   > *"Folder contains a Dev Container configuration file. Reopen to develop in a container."*
3. Haz clic en **Reopen in Container** (o pulsa `Ctrl + Shift + P` / `F1` y selecciona `Dev Containers: Reopen in Container`).
4. **¿Qué sucederá automáticamente tras bambalinas?**
   * Docker Compose levantará la base de datos **PostgreSQL 18**, **MinIO**, **Mailpit** y el contenedor de desarrollo de la **API**.
   * VS Code instalará dentro del contenedor las extensiones necesarias (`C# Dev Kit`, `Database Client`, `REST Client`).
   * Se restaurará la herramienta de migraciones `dotnet-ef` de forma transparente.
   * El código cargará sin advertencias rojas ni errores de imports.

---

### Alternativa: Encender el entorno por consola manual (Terminal externa)

Si por alguna razón prefieres levantar los servicios manualmente sin Dev Containers:

```bash
docker compose -f docker-compose.dev.yml up
```

#### ¿Qué va a pasar la primera vez?
1. Docker descargará las imágenes oficiales de PostgreSQL 18, MinIO, Mailpit y el SDK de .NET 10 (esto puede tardar 2 o 3 minutos según tu conexión a internet).
2. PostgreSQL 18 iniciará y validará que esté listo para recibir conexiones.
3. La API compilará y ejecutará las **migraciones automáticas**, creando la base de datos y sus tablas por ti.
4. Verás en la consola mensajes en tiempo real de la API esperando peticiones en `http://localhost:5000`.

---

## 4. Servicios Disponibles en tu Navegador

Una vez que los contenedores estén corriendo, abre tu navegador web y accede a los siguientes enlaces:

| Servicio | URL Local | Descripción | Credenciales por defecto |
| :--- | :--- | :--- | :--- |
| **Scalar (Documentación de la API)** | [http://localhost:5000/scalar/v1](http://localhost:5000/scalar/v1) | Interfaz visual interactiva para probar todos los endpoints HTTP sin necesidad de Postman. | *No requiere* |
| **Mailpit (Bandeja de Correos)** | [http://localhost:8025](http://localhost:8025) | Bandeja de entrada simulada. Aquí verás los correos de confirmación de reserva (HU-05) y verificación sin enviar nada a internet. | *No requiere* |
| **MinIO Console (Archivos S3)** | [http://localhost:9001](http://localhost:9001) | Panel de control para ver las imágenes y fotos de experiencias subidas al catálogo. | **Usuario:** `rutasv_admin`<br>**Clave:** `rutasv_secret_2026` |
| **PostgreSQL 18** | `localhost:5432` | Servidor de base de datos relacional. | **BD:** `rutasv_db`<br>**User:** `rutasv_user`<br>**Pass:** `RutaSV_Pass_2026!` |

---

## 5. Modalidades de Trabajo (Elige la que prefieras)

En el equipo admitimos tres modalidades de desarrollo según cómo te sientas más cómodo:

### Modalidad A: Con Dev Containers en VS Code (Recomendada)
* Abres el proyecto con `Reopen in Container`.
* Editas cualquier archivo `.cs` y guardas (`Ctrl + S`).
* `dotnet watch` recarga la API automáticamente en caliente y la terminal integrada de VS Code ya está situada dentro del entorno Linux de la aplicación (`/app`).

### Modalidad B: Todo en Docker desde consola externa
* Ejecutas `docker compose -f docker-compose.dev.yml up` en una terminal.
* Abres el código en tu editor, haces cambios en los archivos `.cs` y guardas (`Ctrl + S`).
* Verás en la terminal cómo `dotnet watch` recarga la API automáticamente.

### Modalidad C: Base de datos en Docker + Depuración (Debug) en tu IDE
Si quieres poner puntos de interrupción (*breakpoints*) y depurar línea por línea en Visual Studio o Rider:
1. Enciende únicamente la base de datos, el correo y MinIO en segundo plano:
   ```bash
   docker compose -f docker-compose.dev.yml up postgres minio mailpit -d
   ```
2. Abre la solución `RutaSV.slnx` en tu IDE favorito.
3. Dale al botón **Play / Run / Debug** apuntando al proyecto `RutaSV.Api`.
4. La API se ejecutará directamente en tu máquina pero se conectará a la base de datos de Docker sin conflictos.

---

## 6. Gestión de Base de Datos y Migraciones (Entity Framework Core)

Cuando crees nuevas entidades en `RutaSV.Domain` o agregues configuraciones de mapeo Fluent API en `RutaSV.Infrastructure/Persistence/Configurations/`, debes generar una migración para que PostgreSQL actualice su esquema relacional.

> 💡 **Nota:** La herramienta `dotnet ef` ya está registrada en el repositorio mediante `.config/dotnet-tools.json` y se restaura sola dentro del Dev Container. No necesitas instalar nada a nivel global.

Ejecuta los siguientes comandos en la **terminal integrada** (dentro del Dev Container o en la raíz de tu solución):

### 1. Crear una nueva migración
Usa un nombre en formato PascalCase que describa con claridad el cambio (ej. `AddCompanyAndOperatorRequest`):

```bash
dotnet ef migrations add NombreDeTuMigracion \
  --project src/RutaSV.Infrastructure \
  --startup-project src/RutaSV.Api \
  --output-dir Persistence/Migrations
```

### 2. Aplicar la migración a la base de datos local
Para aplicar los cambios a PostgreSQL inmediatamente sin tener que reiniciar los contenedores:

```bash
dotnet ef database update \
  --project src/RutaSV.Infrastructure \
  --startup-project src/RutaSV.Api
```

*(Recuerda que al reiniciar el contenedor de la API, el archivo `Program.cs` también ejecuta `MigrateAsync()` automáticamente en modo Development).*

### 3. Comandos útiles y resolución de problemas con migraciones

* **Ver el listado de migraciones y saber cuáles ya se aplicaron:**
  ```bash
  dotnet ef migrations list \
    --project src/RutaSV.Infrastructure \
    --startup-project src/RutaSV.Api
  ```

* **Eliminar la última migración creada (solo si NO la has subido a Git):**
  Si cometiste un error en la migración recién generada:
  * Si **aún no** corriste `database update`:
    ```bash
    dotnet ef migrations remove \
      --project src/RutaSV.Infrastructure \
      --startup-project src/RutaSV.Api
    ```
  * Si **ya** habías corrido `database update`, primero regresa la base de datos a la migración anterior y luego remuévela:
    ```bash
    # 1. Regresar la BD al estado de la migración previa
    dotnet ef database update NombreDeLaMigracionAnterior \
      --project src/RutaSV.Infrastructure \
      --startup-project src/RutaSV.Api

    # 2. Borrar los archivos de la migración errónea
    dotnet ef migrations remove \
      --project src/RutaSV.Infrastructure \
      --startup-project src/RutaSV.Api
    ```

* **Generar el script SQL puro (útil para auditoría o revisión):**
  ```bash
  dotnet ef migrations script \
    --project src/RutaSV.Infrastructure \
    --startup-project src/RutaSV.Api
  ```

---

## 7. Comandos del Día a Día para Desarrolladores

Guarda estos comandos útiles para tu trabajo diario:

* **Apagar el entorno conservando los datos:**
  Si vas a dejar de trabajar por el día:
  ```bash
  docker compose -f docker-compose.dev.yml down
  ```
  *(Tus datos de prueba, usuarios y reservas seguirán guardados para la próxima sesión).*

* **Reiniciar la base de datos desde cero:**
  Si hiciste pruebas raras o quieres una base de datos 100% limpia:
  ```bash
  docker compose -f docker-compose.dev.yml down -v
  ```
  *(El flag `-v` elimina los volúmenes de datos antiguos y las migraciones se volverán a correr en limpio).*

* **Ver los registros (logs) si algo falla:**
  ```bash
  docker compose -f docker-compose.dev.yml logs -f rutasv-api
  ```

---

## 8. Regla de Oro antes de hacer `git push` (Evita que el CI falle)

El repositorio cuenta con un pipeline estricto en GitHub Actions que revisa la calidad del código, el formato y las directivas `using` innecesarias. 

Antes de hacer commit y push hacia `develop` o crear un Pull Request, ejecuta siempre estos tres comandos en la raíz del proyecto (o en la terminal del Dev Container):

```bash
# 1. Limpia y formatea automáticamente tu código (organiza imports y saltos de línea)
dotnet format RutaSV.slnx

# 2. Compila verificando que no existan advertencias ni errores
dotnet build RutaSV.slnx -warnaserror

# 3. Ejecuta las pruebas unitarias
dotnet test RutaSV.slnx
```

Si estos tres comandos terminan sin errores en tu computadora, tienes la certeza absoluta de que el pipeline de GitHub Actions se completará con éxito en verde.