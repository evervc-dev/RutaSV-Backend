# Guía de Inicio Rápido y Desarrollo Local: RutaSV Backend

¡Bienvenido al equipo de desarrollo de **RutaSV**! Esta guía está pensada para que cualquier miembro del equipo, sin importar su nivel de experiencia previa con Docker o contenedores, pueda clonar el proyecto, configurarlo en pocos minutos y empezar a programar con recarga en vivo de cambios.

---

## 1. Conceptos Clave (Explicación para Principiantes)

Antes de teclear comandos, entendamos las herramientas que usaremos a diario para que no parezcan una "caja negra":

* **¿Qué es Docker?**  
  Imagina que en lugar de instalar PostgreSQL, herramientas de correo y servidores de archivos directamente en tu sistema operativo (lo que suele causar el clásico problema de *"en mi máquina sí funciona"*), Docker empaqueta cada herramienta en una "cajita de arena" aislada llamada **Contenedor**.
* **¿Qué es Docker Compose?**  
  Es un director de orquesta. Con un solo comando (`docker compose up`), levanta y conecta entre sí todos los contenedores que la aplicación necesita: la base de datos (**PostgreSQL 18**), el gestor de imágenes (**MinIO**), el simulador de correos (**Mailpit**) y nuestra **API de .NET 10**.
* **¿Qué es Hot Reload (`dotnet watch`)?**  
  Gracias a la configuración de volúmenes, la carpeta con tu código en tu computadora se refleja dentro del contenedor. Cuando edites un archivo `.cs` y guardes los cambios (`Ctrl + S`), el contenedor detectará el cambio y recompilará la API en caliente sin que tengas que reiniciar nada manualmente.
* **¿Qué es el archivo `.env`?**  
  Es un archivo de texto simple donde se guardan variables de configuración (puertos, nombres de usuario, contraseñas). Sirve para que nadie suba contraseñas reales al repositorio de Git por accidente.

---

## 2. Requisitos Previos en tu Computadora

Antes de empezar, asegúrate de tener instalado en tu máquina:

1. **Git:** Para clonar y manejar el control de versiones ([Descargar Git](https://git-scm.com/)).
2. **Docker Desktop** (en Windows/Mac) o **Docker Engine + Docker Compose Plugin** (en Linux). Asegúrate de que el servicio de Docker esté corriendo.
3. **.NET 10 SDK:** Recomendado si deseas que tu editor de código (VS Code, Rider o Visual Studio) reconozca la sintaxis, autocompletado y ejecute pruebas locales ([Descargar .NET](https://dotnet.microsoft.com/download)).
4. **Editor de código recomendado:** Visual Studio Code (con la extensión *C# Dev Kit*), JetBrains Rider o Visual Studio 2022/2026.

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

### Paso 2: Crear tu archivo de variables locales (`.env`)
El repositorio incluye una plantilla llamada `.env.example`. Debes hacer una copia de este archivo y nombrarla `.env`:

* **En Linux / macOS / Git Bash:**
  ```bash
  cp .env.example .env
  ```
* **En Windows (PowerShell):**
  ```powershell
  Copy-Item .env.example .env
  ```

> 💡 **Nota:** Para empezar a trabajar **solo necesitas asignar los campos que no tinenen valor** dentro del `.env`. Ya hay valores predeterminados, pero hay ciertos campos vacíos (usuarios y contraseñás), cuando asignes esos valores, el entorno ya estará listo para funcionar en local. Este archivo está en el `.gitignore`, por lo que tus configuraciones personales nunca se subirán a GitHub.

---

### Paso 3: Encender el entorno completo con Docker Compose

Ejecuta el siguiente comando en la raíz del proyecto:

```bash
docker compose -f docker-compose.dev.yml up
```

#### ¿Qué va a pasar la primera vez?
1. Docker descargará las imágenes oficiales de PostgreSQL 18, MinIO, Mailpit y el SDK de .NET 10 (esto puede tardar 2 o 3 minutos según tu conexión a internet).
2. PostgreSQL 18 iniciará y validará que esté listo para recibir conexiones.
3. La API compilará y ejecutará las **migraciones automáticas**, creando la base de datos y sus tablas por ti.
4. Verás en la consola mensajes en tiempo real de la API esperando peticiones.

---

## 4. Servicios Disponibles en tu Navegador

Una vez que los contenedores estén corriendo, abre tu navegador web y accede a los siguientes enlaces:

| Servicio | URL Local | Descripción | Credenciales por defecto |
| :--- | :--- | :--- | :--- |
| **Scalar (Documentación de la API)** | [http://localhost:5000/scalar/v1](http://localhost:5000/scalar/v1) | Interfaz visual para probar todos los endpoints HTTP sin necesidad de Postman. | *No requiere* |
| **Mailpit (Bandeja de Correos)** | [http://localhost:8025](http://localhost:8025) | Bandeja de entrada simulada. Aquí verás los correos de confirmación de reserva (HU-05) sin enviar nada a internet. | *No requiere* |
| **MinIO Console (Archivos S3)** | [http://localhost:9001](http://localhost:9001) | Panel de control para ver las imágenes y fotos de experiencias subidas al catálogo. | **Usuario:** `rutasv_admin`<br>**Clave:** `rutasv_secret_2026` |
| **PostgreSQL 18** | `localhost:5432` | Servidor de base de datos relacional. | **BD:** `rutasv_db`<br>**User:** `rutasv_user`<br>**Pass:** `RutaSV_Pass_2026!` |

---

## 5. Dos Formas de Trabajar (Elige la que prefieras)

En el equipo admitimos dos modalidades de desarrollo según cómo te sientas más cómodo:

### Modalidad A: Todo en Docker (Recomendada si no quieres configurar nada local)
* Ejecutas `docker compose -f docker-compose.dev.yml up`.
* Abres el código en tu editor, haces cambios en los archivos `.cs` y guardas (`Ctrl + S`).
* Verás en la terminal cómo `dotnet watch` recarga la API automáticamente.

### Modalidad B: Base de datos en Docker + Depuración (Debug) en tu IDE
Si quieres poner puntos de interrupción (*breakpoints*) y depurar línea por línea en Visual Studio o Rider:
1. Enciende únicamente la base de datos, el correo y MinIO en segundo plano:
   ```bash
   docker compose -f docker-compose.dev.yml up postgres minio mailpit -d
   ```
2. Abre la solución `RutaSV.slnx` en tu IDE favorito.
3. Dale al botón **Play / Run / Debug** apuntando al proyecto `RutaSV.Api`.
4. La API se ejecutará directamente en tu máquina pero se conectará a la base de datos de Docker sin conflictos.

---

## 6. Comandos del Día a Día para Desarrolladores

Guarda estos comandos útiles para tu trabajo diario:

* **Apagar el entorno conservando los datos:**
  Si vas a dejar de trabajar por el día:
  ```bash
  docker compose -f docker-compose.dev.yml down
  ```
  *(Tus datos de prueba y reservas seguirán guardados para la próxima sesión).*

* **Reiniciar la base de datos desde cero:**
  Si hiciste pruebas raras o quieres una base de datos 100% limpia:
  ```bash
  docker compose -f docker-compose.dev.yml down -v
  ```
  *(El flag `-v` elimina los volúmenes de datos antiguos).*

* **Ver los registros (logs) si algo falla:**
  ```bash
  docker compose -f docker-compose.dev.yml logs -f rutasv-api
  ```

---

## 7. Regla de Oro antes de hacer `git push` (Evita que el CI falle)

El repositorio cuenta con un pipeline estricto en GitHub Actions que revisa la calidad del código, el formato y las directivas `using` innecesarias. 

Antes de hacer commit y push hacia `develop` o crear un Pull Request, ejecuta siempre estos dos comandos en la raíz del proyecto:

```bash
# 1. Limpia y formatea automáticamente tu código (organiza imports y saltos de línea)
dotnet format RutaSV.slnx

# 2. Compila verificando que no existan advertencias ni errores
dotnet build RutaSV.slnx -warnaserror

# 3. Ejecuta las pruebas unitarias
dotnet test RutaSV.slnx
```

Si estos tres comandos terminan sin errores en tu computadora, tienes la certeza absoluta de que el pipeline de GitHub Actions se completará con éxito en verde.