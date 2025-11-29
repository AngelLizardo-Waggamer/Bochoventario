# Inventory Service API

## Descripción General

El Inventory Service es un microservicio RESTful desarrollado en ASP.NET Core 8.0 que gestiona el inventario de productos. Proporciona operaciones CRUD completas para artículos, con autenticación JWT y control de acceso basado en roles. Este servicio forma parte de una arquitectura de microservicios y utiliza MySQL como base de datos relacional.

## Tabla de Contenidos

- [Características Principales](#características-principales)
- [Arquitectura](#arquitectura)
- [Requisitos Previos](#requisitos-previos)
- [Instalación y Configuración](#instalación-y-configuración)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Modelo de Datos](#modelo-de-datos)
- [Endpoints de la API](#endpoints-de-la-api)
- [Autenticación y Autorización](#autenticación-y-autorización)
- [Despliegue](#despliegue)
- [Testing](#testing)
- [Variables de Entorno](#variables-de-entorno)

## Características Principales

- Gestión completa de inventario (CRUD de productos)
- Autenticación mediante JWT (JSON Web Tokens)
- Control de acceso basado en roles (Administrador, Gestor, Lector)
- Búsqueda y filtrado de productos
- Auditoría de cambios en el inventario
- Soporte para múltiples ubicaciones de almacenamiento
- Validación de negocio (SKU único, permisos, etc.)
- API RESTful con documentación Swagger/OpenAPI
- Containerización con Docker
- Base de datos MySQL con relaciones normalizadas

## Arquitectura

### Stack Tecnológico

- **Framework**: ASP.NET Core 8.0
- **Lenguaje**: C# 12
- **Base de Datos**: MySQL 8.4
- **ORM**: Entity Framework Core 9.0
- **Autenticación**: JWT Bearer Authentication
- **Documentación**: Swagger/OpenAPI
- **Containerización**: Docker
- **Testing**: xUnit, Moq, EF Core InMemory

### Patrón de Arquitectura

El servicio sigue una arquitectura en capas:

```
┌─────────────────────────────────┐
│   Controllers Layer             │  (API Endpoints)
├─────────────────────────────────┤
│   Business Logic Layer          │  (Validaciones, Permisos)
├─────────────────────────────────┤
│   Data Access Layer             │  (Entity Framework Core)
├─────────────────────────────────┤
│   Database Layer                │  (MySQL)
└─────────────────────────────────┘
```

## Requisitos Previos

### Para Desarrollo Local

- .NET SDK 8.0 o superior
- MySQL 8.0 o superior
- Visual Studio 2022 / VS Code / Rider (opcional)
- Git

### Para Despliegue con Docker

- Docker 20.10 o superior
- Docker Compose 2.0 o superior

## Instalación y Configuración

### Opción 1: Desarrollo Local

#### 1. Clonar el repositorio

```bash
git clone https://github.com/AngelLizardo-Waggamer/Bochoventario.git
cd Bochoventario/inventory_service
```

#### 2. Configurar la base de datos

Crear una base de datos MySQL y ejecutar el script de esquema:

```bash
mysql -u root -p < schema.sql
```

#### 3. Configurar variables de entorno o appsettings

Crear un archivo `appsettings.Development.json` o configurar las variables de entorno:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=inventory_db;User=root;Password=tu_password;"
  },
  "JwtSettings": {
    "SecretKey": "tu-clave-secreta-super-segura-cambia-en-produccion-minimo-32-caracteres",
    "Issuer": "auth-service",
    "Audience": "inventory-api"
  }
}
```

#### 4. Restaurar dependencias

```bash
dotnet restore
```

#### 5. Ejecutar la aplicación

```bash
dotnet run
```

La API estará disponible en `http://localhost:5213` y Swagger en `http://localhost:5213/swagger`

### Opción 2: Docker Compose (Recomendado)

#### 1. Configurar variables en docker-compose.yml

Editar las variables JWT en el archivo `docker-compose.yml` (sección `x-jwt-variables`).

#### 2. Levantar los servicios

```bash
docker-compose up -d
```

Esto iniciará:
- MySQL en el puerto `5003`
- API en el puerto `5001`

La API estará disponible en `http://localhost:5001` y Swagger en `http://localhost:5001/swagger`

#### 3. Verificar el estado

```bash
docker-compose ps
docker-compose logs -f inventory_api
```

#### 4. Detener los servicios

```bash
docker-compose down
```

## Estructura del Proyecto

```
inventory_service/
├── Controllers/
│   └── InventoryController.cs      # Controlador principal con endpoints REST
├── Data/
│   └── AppDbContext.cs              # Contexto de Entity Framework Core
├── Models/
│   ├── Articulo.cs                  # Modelo de producto/artículo
│   ├── Inventario.cs                # Modelo de registro de inventario
│   ├── Usuario.cs                   # Modelo de usuario
│   └── Rol.cs                       # Modelo de rol
├── Tests/
│   ├── CreateProductTests.cs        # Tests unitarios para creación
│   ├── DeleteProductTests.cs        # Tests unitarios para eliminación
│   ├── GetProductTests.cs           # Tests unitarios para consultas individuales
│   ├── GetProductsTests.cs          # Tests unitarios para listados
│   ├── UpdateProductTests.cs        # Tests unitarios para actualización
│   ├── HelperMethodsTests.cs        # Tests para métodos auxiliares
│   └── README.md                    # Documentación de tests
├── Properties/
│   └── launchSettings.json          # Configuración de perfiles de ejecución
├── Program.cs                       # Punto de entrada y configuración de servicios
├── Dockerfile                       # Definición de imagen Docker
├── docker-compose.yml               # Orquestación de contenedores
├── schema.sql                       # Script de inicialización de BD
├── inventory_service.csproj         # Archivo de proyecto .NET
├── appsettings.json                 # Configuración de producción
└── appsettings.Development.json     # Configuración de desarrollo
```

## Modelo de Datos

### Diagrama de Entidad-Relación

```
┌─────────────┐       ┌──────────────┐       ┌──────────────┐
│    Roles    │       │   Usuarios   │       │  Articulos   │
├─────────────┤       ├──────────────┤       ├──────────────┤
│ id_rol (PK) │◄──────┤ id_usuario   │       │ id_articulo  │
│ nombre_rol  │       │ id_rol (FK)  │       │ sku (UNIQUE) │
└─────────────┘       │ nombre_usr   │       │ nombre       │
                      │ password_hash│       │ descripcion  │
                      │ nombre_comp  │       │ precio_costo │
                      │ fecha_creac  │       └──────┬───────┘
                      └──────┬───────┘              │
                             │                      │
                             │     ┌────────────────┘
                             │     │
                             │     ▼
                             │  ┌──────────────────┐
                             │  │   Inventario     │
                             │  ├──────────────────┤
                             │  │ id_inventario    │
                             │  │ id_articulo (FK) │
                             └─►│ ultima_mod (FK)  │
                                │ cantidad         │
                                │ ubicacion        │
                                │ ultima_actual    │
                                └──────────────────┘
```

### Entidades

#### Articulos (Productos)

Representa los productos del inventario.

| Campo         | Tipo          | Descripción                          |
|---------------|---------------|--------------------------------------|
| id_articulo   | INT (PK)      | Identificador único del artículo     |
| sku           | VARCHAR(50)   | Código único del producto (UNIQUE)   |
| nombre        | VARCHAR(150)  | Nombre del producto                  |
| descripcion   | TEXT          | Descripción detallada (opcional)     |
| precio_costo  | DECIMAL(10,2) | Costo del producto                   |

#### Inventario

Registros de stock por ubicación.

| Campo                   | Tipo         | Descripción                              |
|-------------------------|--------------|------------------------------------------|
| id_inventario           | INT (PK)     | Identificador único del registro         |
| id_articulo             | INT (FK)     | Referencia al artículo                   |
| cantidad                | INT          | Cantidad en stock                        |
| ubicacion               | VARCHAR(50)  | Ubicación de almacenamiento (opcional)   |
| ultima_modificacion_por | INT (FK)     | Usuario que realizó la última modificación|
| ultima_actualizacion    | TIMESTAMP    | Fecha y hora de última actualización     |

**Constraint único**: `(id_articulo, ubicacion)` - Un artículo solo puede tener un registro por ubicación.

#### Usuarios

Usuarios del sistema con acceso a la API.

| Campo          | Tipo          | Descripción                        |
|----------------|---------------|------------------------------------|
| id_usuario     | INT (PK)      | Identificador único del usuario    |
| id_rol         | INT (FK)      | Referencia al rol del usuario      |
| nombre_usuario | VARCHAR(50)   | Nombre de usuario (UNIQUE)         |
| password_hash  | VARCHAR(255)  | Hash de la contraseña              |
| nombre_completo| VARCHAR(100)  | Nombre completo (opcional)         |
| fecha_creacion | TIMESTAMP     | Fecha de creación del usuario      |

#### Roles

Roles del sistema para control de acceso.

| Campo      | Tipo        | Descripción                    |
|------------|-------------|--------------------------------|
| id_rol     | INT (PK)    | Identificador único del rol    |
| nombre_rol | VARCHAR(50) | Nombre del rol (UNIQUE)        |

**Roles predefinidos**:
- **Administrador** (id_rol: 1): Acceso completo
- **Gestor** (id_rol: 2): Gestión de inventario
- **Lector** (id_rol: 3): Solo lectura

### Relaciones

- Un **Rol** puede tener múltiples **Usuarios** (1:N)
- Un **Artículo** puede tener múltiples registros de **Inventario** (1:N)
- Un **Usuario** puede modificar múltiples registros de **Inventario** (1:N)
- Eliminación en cascada: Al eliminar un **Artículo**, se eliminan sus registros de **Inventario**

## Endpoints de la API

Base URL: `http://localhost:5001/api/products` (Docker) o `http://localhost:5213/api/products` (local)

### 1. Listar Productos

```http
GET /api/products
```

Lista todos los productos con soporte para filtros opcionales.

**Parámetros de Query (opcionales)**:

| Parámetro | Tipo   | Descripción                                    |
|-----------|--------|------------------------------------------------|
| q         | string | Búsqueda general (nombre, SKU o descripción)   |
| category  | string | Filtro por categoría en la descripción         |

**Respuesta exitosa (200 OK)**:

```json
[
  {
    "idArticulo": 1,
    "sku": "SKU-001",
    "nombre": "Laptop Dell",
    "descripcion": "Laptop para oficina",
    "precioCosto": 15000.00
  },
  {
    "idArticulo": 2,
    "sku": "SKU-002",
    "nombre": "Mouse Logitech",
    "descripcion": "Mouse inalámbrico",
    "precioCosto": 350.00
  }
]
```

**Ejemplos de uso**:

```bash
# Listar todos los productos
curl -X GET "http://localhost:5001/api/products"

# Buscar por nombre o SKU
curl -X GET "http://localhost:5001/api/products?q=Laptop"

# Filtrar por categoría
curl -X GET "http://localhost:5001/api/products?category=electronica"

# Filtros combinados
curl -X GET "http://localhost:5001/api/products?q=Mouse&category=accesorios"
```

**Características**:
- No requiere autenticación
- Búsqueda case-insensitive
- Retorna array vacío si no hay resultados

### 2. Obtener Producto por ID

```http
GET /api/products/{id}
```

Obtiene los detalles de un producto específico, incluyendo sus registros de inventario.

**Parámetros de Ruta**:

| Parámetro | Tipo | Descripción                  |
|-----------|------|------------------------------|
| id        | int  | ID del artículo a consultar  |

**Respuesta exitosa (200 OK)**:

```json
{
  "idArticulo": 1,
  "sku": "SKU-001",
  "nombre": "Laptop Dell",
  "descripcion": "Laptop para oficina",
  "precioCosto": 15000.00,
  "inventarios": [
    {
      "idInventario": 1,
      "idArticulo": 1,
      "cantidad": 10,
      "ubicacion": "Almacén A",
      "ultimaModificacionPor": 1,
      "ultimaActualizacion": "2025-11-29T10:30:00"
    },
    {
      "idInventario": 2,
      "idArticulo": 1,
      "cantidad": 5,
      "ubicacion": "Almacén B",
      "ultimaModificacionPor": 2,
      "ultimaActualizacion": "2025-11-28T15:20:00"
    }
  ]
}
```

**Respuesta de error (404 Not Found)**:

```json
{
  "message": "Producto con ID 999 no encontrado"
}
```

**Ejemplo de uso**:

```bash
curl -X GET "http://localhost:5001/api/products/1"
```

**Características**:
- No requiere autenticación
- Incluye relación con inventarios
- Retorna 404 si el producto no existe

### 3. Crear Producto

```http
POST /api/products
```

Crea un nuevo producto en el sistema.

**Requiere autenticación**: Sí (Bearer Token JWT)

**Permisos requeridos**: Administrador o Gestor

**Body (JSON)**:

```json
{
  "articulo": {
    "sku": "SKU-003",
    "nombre": "Teclado Mecánico",
    "descripcion": "Teclado RGB para gaming",
    "precioCosto": 1200.00
  }
}
```

**Campos requeridos**:

| Campo       | Tipo   | Descripción                  | Validación           |
|-------------|--------|------------------------------|----------------------|
| sku         | string | Código único del producto    | Máx. 50 caracteres   |
| nombre      | string | Nombre del producto          | Máx. 150 caracteres  |
| descripcion | string | Descripción (opcional)       | -                    |
| precioCosto | decimal| Costo del producto           | Precisión (10,2)     |

**Respuesta exitosa (201 Created)**:

```json
{
  "idArticulo": 3,
  "sku": "SKU-003",
  "nombre": "Teclado Mecánico",
  "descripcion": "Teclado RGB para gaming",
  "precioCosto": 1200.00
}
```

**Headers de respuesta**:
```
Location: /api/products/3
```

**Respuestas de error**:

- **401 Unauthorized**: Token no válido, permisos insuficientes o claims requeridos faltantes
  ```json
  {
    "message": "No se pudo obtener el ID del usuario del token JWT"
  }
  ```
  o
  ```json
  {
    "message": "El usuario con rol 3 no tiene permisos suficientes"
  }
  ```

- **409 Conflict**: SKU duplicado
  ```json
  {
    "message": "Ya existe un producto con el SKU 'SKU-003'"
  }
  ```

**Ejemplo de uso**:

```bash
curl -X POST "http://localhost:5001/api/products" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "articulo": {
      "sku": "SKU-003",
      "nombre": "Teclado Mecánico",
      "descripcion": "Teclado RGB para gaming",
      "precioCosto": 1200.00
    }
  }'
```

**Validaciones de negocio**:
- El SKU debe ser único en el sistema
- El token JWT debe contener los claims requeridos: `id_usuario`, `nombre_usuario`, `id_rol`
- El `id_rol` debe ser 1 (Administrador) o 2 (Gestor) para crear productos
- La validación de permisos es completamente basada en el token (sin consultas a BD)

### 4. Actualizar Producto

```http
PUT /api/products/{id}
```

Actualiza un producto existente y sus registros de inventario asociados.

**Requiere autenticación**: Sí (Bearer Token JWT)

**Permisos requeridos**: Administrador o Gestor

**Parámetros de Ruta**:

| Parámetro | Tipo | Descripción                    |
|-----------|------|--------------------------------|
| id        | int  | ID del artículo a actualizar   |

**Body (JSON)**:

```json
{
  "articulo": {
    "idArticulo": 1,
    "sku": "SKU-001-UPDATED",
    "nombre": "Laptop Dell Inspiron",
    "descripcion": "Laptop actualizada para oficina",
    "precioCosto": 16000.00
  }
}
```

**Respuesta exitosa (204 No Content)**:

Sin contenido en el body.

**Respuestas de error**:

- **400 Bad Request**: ID en URL no coincide con ID en body
  ```json
  {
    "message": "El ID del producto no coincide"
  }
  ```

- **401 Unauthorized**: Token no válido, permisos insuficientes o claims requeridos faltantes
  ```json
  {
    "message": "El usuario 'lector' no tiene permisos suficientes"
  }
  ```
  o
  ```json
  {
    "message": "No se pudo obtener el nombre de usuario del token JWT"
  }
  ```

- **404 Not Found**: Producto no encontrado
  ```json
  {
    "message": "Producto con ID 999 no encontrado"
  }
  ```

- **409 Conflict**: SKU ya existe en otro producto
  ```json
  {
    "message": "El SKU 'SKU-002' ya está en uso por otro producto"
  }
  ```

**Ejemplo de uso**:

```bash
curl -X PUT "http://localhost:5001/api/products/1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "articulo": {
      "idArticulo": 1,
      "sku": "SKU-001-UPDATED",
      "nombre": "Laptop Dell Inspiron",
      "descripcion": "Laptop actualizada",
      "precioCosto": 16000.00
    }
  }'
```

**Comportamiento especial**:
- Actualiza automáticamente todos los registros de inventario asociados
- Establece `ultima_actualizacion` a la fecha/hora actual
- Establece `ultima_modificacion_por` al ID del usuario autenticado
- Valida que el SKU no esté en uso por otro producto

### 5. Eliminar Producto

```http
DELETE /api/products/{id}
```

Elimina un producto del sistema. Los registros de inventario asociados se eliminan en cascada.

**Requiere autenticación**: Sí (Bearer Token JWT)

**Permisos requeridos**: Administrador o Gestor

**Parámetros de Ruta**:

| Parámetro | Tipo | Descripción                  |
|-----------|------|------------------------------|
| id        | int  | ID del artículo a eliminar   |

**Respuesta exitosa (204 No Content)**:

Sin contenido en el body.

**Respuestas de error**:

- **401 Unauthorized**: Token no válido, permisos insuficientes o claims requeridos faltantes
  ```json
  {
    "message": "No se pudo obtener el ID del usuario del token JWT"
  }
  ```
  o
  ```json
  {
    "message": "El usuario con rol 3 no tiene permisos suficientes"
  }
  ```

- **404 Not Found**: Producto no encontrado
  ```json
  {
    "message": "Producto con ID 999 no encontrado"
  }
  ```

**Ejemplo de uso**:

```bash
curl -X DELETE "http://localhost:5001/api/products/1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Advertencias**:
- La eliminación es permanente y no se puede deshacer
- Los registros de inventario asociados se eliminan automáticamente (ON DELETE CASCADE)
- Solo usuarios con rol Administrador o Gestor pueden eliminar productos

## Autenticación y Autorización

### Autenticación JWT

El servicio utiliza JWT (JSON Web Tokens) para autenticar las peticiones a los endpoints protegidos.

#### Configuración del Token

El token JWT debe incluir los siguientes claims personalizados:

| Claim          | Tipo   | Descripción                           | Requerido |
|----------------|--------|---------------------------------------|-----------|
| id_usuario     | int    | ID del usuario autenticado            | Sí        |
| nombre_usuario | string | Nombre de usuario                     | Sí        |
| id_rol         | int    | ID del rol del usuario (1, 2 o 3)     | Sí        |
| iss            | string | Issuer del token                      | Sí        |
| aud            | string | Audience del token                    | Sí        |
| exp            | int    | Fecha de expiración (timestamp Unix)  | Sí        |

**Ejemplo de payload JWT**:
```json
{
  "id_usuario": 1,
  "nombre_usuario": "admin",
  "id_rol": 1,
  "iss": "auth-service",
  "aud": "inventory-api",
  "exp": 1735574400
}
```

**Nota**: El servicio extrae la información del usuario directamente de los claims del token JWT. No se realizan consultas adicionales a la base de datos para validar permisos.

#### Configuración de JWT

Las variables de configuración JWT se pueden establecer mediante:

1. **Variables de entorno** (recomendado para producción):
   ```bash
   JWT_SECRET_KEY=tu-clave-secreta-super-segura-minimo-32-caracteres
   JWT_ISSUER=auth-service
   JWT_AUDIENCE=inventory-api
   ```

2. **Archivo appsettings.json**:
   ```json
   {
     "JwtSettings": {
       "SecretKey": "tu-clave-secreta",
       "Issuer": "auth-service",
       "Audience": "inventory-api"
     }
   }
   ```

#### Uso del Token

Para realizar peticiones autenticadas, incluir el token en el header `Authorization`:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Ejemplo con curl**:

```bash
curl -X POST "http://localhost:5001/api/products" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"articulo": {...}}'
```

### Control de Acceso por Roles

El sistema implementa un control de acceso basado en roles (RBAC):

#### Roles del Sistema

| Rol            | ID | Permisos                                          |
|----------------|----|---------------------------------------------------|
| Administrador  | 1  | Acceso completo (crear, leer, actualizar, eliminar)|
| Gestor         | 2  | Gestión de inventario (crear, leer, actualizar, eliminar)|
| Lector         | 3  | Solo lectura (listar y consultar productos)       |

#### Matriz de Permisos

| Endpoint                        | Administrador | Gestor | Lector | Sin Auth |
|---------------------------------|---------------|--------|--------|----------|
| GET /api/products               | ✓             | ✓      | ✓      | ✓        |
| GET /api/products/{id}          | ✓             | ✓      | ✓      | ✓        |
| POST /api/products              | ✓             | ✓      | ✗      | ✗        |
| PUT /api/products/{id}          | ✓             | ✓      | ✗      | ✗        |
| DELETE /api/products/{id}       | ✓             | ✓      | ✗      | ✗        |

**Notas**:
- Los endpoints de lectura (GET) son públicos y no requieren autenticación
- Los endpoints de escritura (POST, PUT, DELETE) requieren rol de Administrador o Gestor
- Los usuarios con rol Lector recibirán un error 401 al intentar operaciones de escritura

### Validación de Permisos

El proceso de validación es completamente basado en claims del token JWT y sigue estos pasos:

1. **Extracción de claims del token JWT**
   - `id_usuario`: ID del usuario autenticado (claim requerido)
   - `nombre_usuario`: Nombre del usuario (claim requerido)
   - `id_rol`: ID del rol del usuario (claim requerido)
   - Si algún claim requerido falta, retorna 401 Unauthorized

2. **Verificación de permisos basada en rol**
   - Valida que el `id_rol` sea 1 (Administrador) o 2 (Gestor) para operaciones de escritura
   - Si es 3 (Lector), retorna 401 Unauthorized con mensaje descriptivo
   - No se consulta la base de datos - toda la validación se basa en los claims del token

3. **Auditoría automática**
   - En operaciones de actualización, registra el `id_usuario` extraído del token
   - Actualiza timestamp de última modificación en inventarios
   - Establece `ultima_modificacion_por` con el `id_usuario` del token

**Nota**: Este enfoque de autenticación sin estado (stateless) mejora el rendimiento al evitar consultas a la base de datos para cada petición. La responsabilidad de mantener la integridad de los datos del usuario recae en el servicio de autenticación que genera el token.

## Despliegue

### Despliegue con Docker Compose (Recomendado)

#### Requisitos

- Docker 20.10+
- Docker Compose 2.0+

#### Pasos

1. **Clonar el repositorio**:
   ```bash
   git clone https://github.com/AngelLizardo-Waggamer/Bochoventario.git
   cd Bochoventario/inventory_service
   ```

2. **Configurar variables de entorno**:
   Editar `docker-compose.yml` y actualizar la sección `x-jwt-variables`:
   ```yaml
   x-jwt-variables: &jwt-variables
     JWT_SECRET_KEY: tu-clave-segura-produccion
     JWT_ISSUER: auth-service
     JWT_AUDIENCE: inventory-api
   ```

3. **Construir y levantar servicios**:
   ```bash
   docker-compose up -d --build
   ```

4. **Verificar el estado**:
   ```bash
   docker-compose ps
   docker-compose logs -f inventory_api
   ```

5. **Acceder a la API**:
   - API: http://localhost:5001
   - Swagger: http://localhost:5001/swagger
   - MySQL: localhost:5003

#### Arquitectura de Contenedores

El `docker-compose.yml` define los siguientes servicios:

**mysql** (inventory_mysql):
- Imagen: mysql:8.4
- Puerto: 5003 → 3306
- Volumen: mysql_data (persistencia)
- Inicialización: schema.sql
- Healthcheck: mysqladmin ping

**inventory_api**:
- Build: Dockerfile multietapa
- Puerto: 5001 → 8080
- Usuario: appuser (no-root)
- Depends on: mysql (con healthcheck)
- Restart: unless-stopped

#### Características de Seguridad

- Contenedor de aplicación ejecuta como usuario no-root (appuser)
- Red aislada (inventory_network)
- Healthchecks configurados
- Variables sensibles mediante environment variables
- Volúmenes persistentes para datos

### Despliegue Manual con Docker

#### 1. Construir la imagen

```bash
docker build -t inventory-service:latest .
```

#### 2. Crear red

```bash
docker network create inventory_network
```

#### 3. Levantar MySQL

```bash
docker run -d \
  --name inventory_mysql \
  --network inventory_network \
  -e MYSQL_ROOT_PASSWORD=rootpassword \
  -e MYSQL_DATABASE=inventory_db \
  -e MYSQL_USER=inventory_user \
  -e MYSQL_PASSWORD=inventory_password \
  -p 5003:3306 \
  -v mysql_data:/var/lib/mysql \
  -v $(pwd)/schema.sql:/docker-entrypoint-initdb.d/schema.sql \
  mysql:8.4
```

#### 4. Levantar la aplicación

```bash
docker run -d \
  --name inventory_api \
  --network inventory_network \
  -p 5001:8080 \
  -e JWT_SECRET_KEY=tu-clave-secreta \
  -e JWT_ISSUER=auth-service \
  -e JWT_AUDIENCE=inventory-api \
  -e DB_CONNECTION_STRING="Server=inventory_mysql;Port=3306;Database=inventory_db;User=inventory_user;Password=inventory_password;" \
  inventory-service:latest
```

### Despliegue en Producción

#### Consideraciones

1. **Seguridad**:
   - Usar secretos seguros para JWT_SECRET_KEY (mínimo 32 caracteres)
   - Cambiar contraseñas de base de datos
   - Habilitar HTTPS/TLS
   - Configurar CORS apropiadamente
   - Implementar rate limiting

2. **Base de Datos**:
   - Usar MySQL gestionado (Azure Database, AWS RDS, etc.)
   - Configurar backups automáticos
   - Habilitar réplicas para alta disponibilidad
   - Monitorear performance

3. **Aplicación**:
   - Configurar logging centralizado
   - Implementar métricas y monitoreo (Prometheus, Application Insights)
   - Configurar health checks en el orquestador
   - Escalar horizontalmente según demanda

4. **Variables de Entorno**:
   ```bash
   ASPNETCORE_ENVIRONMENT=Production
   JWT_SECRET_KEY=<secret-from-vault>
   JWT_ISSUER=auth-service-prod
   JWT_AUDIENCE=inventory-api-prod
   DB_CONNECTION_STRING=<connection-from-vault>
   ```

## Testing

El proyecto incluye una suite completa de 43 tests unitarios que cubren todos los endpoints y casos de uso.

### Ejecutar Tests

```bash
# Ejecutar todos los tests
dotnet test

# Con verbosidad detallada
dotnet test --logger "console;verbosity=detailed"

# Ejecutar tests específicos
dotnet test --filter "FullyQualifiedName~CreateProductTests"
dotnet test --filter "FullyQualifiedName~UpdateProductTests"

# Generar reporte de cobertura
dotnet test --collect:"XPlat Code Coverage"
```

### Cobertura de Tests

| Clase de Test           | Tests | Descripción                              |
|-------------------------|-------|------------------------------------------|
| GetProductsTests        | 8     | Listado y filtrado de productos          |
| GetProductTests         | 3     | Consulta individual de productos         |
| CreateProductTests      | 7     | Creación de productos y validaciones     |
| UpdateProductTests      | 9     | Actualización de productos               |
| DeleteProductTests      | 7     | Eliminación de productos                 |
| HelperMethodsTests      | 9     | Métodos de autenticación y permisos      |

### Tipos de Tests Incluidos

- Tests de casos exitosos (happy path)
- Tests de validación de negocio
- Tests de autenticación y autorización
- Tests de roles y permisos
- Tests de manejo de errores
- Tests de validaciones de datos

Para más información sobre los tests, consultar [Tests/README.md](Tests/README.md).

## Variables de Entorno

### Variables Requeridas

| Variable                | Descripción                              | Ejemplo                                    | Requerido |
|-------------------------|------------------------------------------|--------------------------------------------|-----------|
| DB_CONNECTION_STRING    | Cadena de conexión a MySQL               | Server=mysql;Port=3306;Database=...       | Sí        |
| JWT_SECRET_KEY          | Clave secreta para firmar tokens JWT     | minimo-32-caracteres-seguros               | Sí        |
| JWT_ISSUER              | Emisor del token JWT                     | auth-service                               | Sí        |
| JWT_AUDIENCE            | Audiencia del token JWT                  | inventory-api                              | Sí        |

### Variables Opcionales

| Variable                | Descripción                              | Default                                    |
|-------------------------|------------------------------------------|--------------------------------------------|
| ASPNETCORE_ENVIRONMENT  | Entorno de ejecución                     | Production                                 |
| ASPNETCORE_URLS         | URLs en las que escucha la aplicación    | http://+:8080                              |

### Configuración por Entorno

#### Desarrollo Local

Usar `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=inventory_db;User=root;Password=password;"
  },
  "JwtSettings": {
    "SecretKey": "development-secret-key-32-chars",
    "Issuer": "auth-service-dev",
    "Audience": "inventory-api-dev"
  }
}
```

#### Docker Compose

Definir en `docker-compose.yml`:
```yaml
environment:
  JWT_SECRET_KEY: produccion-secret-key-segura
  JWT_ISSUER: auth-service
  JWT_AUDIENCE: inventory-api
  DB_CONNECTION_STRING: Server=mysql;Port=3306;...
```

#### Kubernetes

Usar Secrets y ConfigMaps:
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: inventory-secrets
type: Opaque
data:
  jwt-secret-key: <base64-encoded-secret>
  db-connection-string: <base64-encoded-connection>
```

### Prioridad de Configuración

La aplicación busca las configuraciones en el siguiente orden:

1. Variables de entorno
2. appsettings.{Environment}.json
3. appsettings.json
4. Valores por defecto (si están definidos)

## Troubleshooting

### Problemas Comunes

#### 1. Error de conexión a MySQL

**Síntoma**:
```
Unable to connect to any of the specified MySQL hosts
```

**Solución**:
- Verificar que MySQL esté ejecutándose: `docker-compose ps`
- Verificar logs de MySQL: `docker-compose logs mysql`
- Verificar string de conexión en variables de entorno
- Esperar a que el healthcheck de MySQL pase

#### 2. Error de autenticación JWT

**Síntoma**:
```
401 Unauthorized - No se pudo obtener el ID del usuario del token JWT
```

**Solución**:
- Verificar que el token JWT sea válido y no esté expirado
- Verificar que los claims incluyan: `id_usuario`, `nombre_usuario`, `id_rol`
- Verificar configuración de JWT (SecretKey, Issuer, Audience)
- Asegurarse que el token fue generado por el servicio de autenticación correcto

#### 3. Error de permisos

**Síntoma**:
```
401 Unauthorized - El usuario con rol 3 no tiene permisos suficientes
```

**Solución**:
- Verificar el valor del claim `id_rol` en el token JWT
- Solo roles 1 (Administrador) y 2 (Gestor) pueden realizar operaciones de escritura
- Los usuarios con rol 3 (Lector) solo tienen acceso de lectura
- Si necesita permisos de escritura, solicitar un token con rol 1 o 2

#### 4. SKU duplicado

**Síntoma**:
```
409 Conflict - Ya existe un producto con el SKU
```

**Solución**:
- Los SKUs deben ser únicos en el sistema
- Verificar que no exista otro producto con el mismo SKU
- Cambiar el SKU a uno diferente

### Logs y Debugging

#### Ver logs de contenedores

```bash
# Logs de la API
docker-compose logs -f inventory_api

# Logs de MySQL
docker-compose logs -f mysql

# Todos los logs
docker-compose logs -f
```

#### Habilitar logs detallados

Configurar en `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

#### Acceder al contenedor

```bash
# Shell en el contenedor de la API
docker exec -it inventory_api /bin/bash

# MySQL client
docker exec -it inventory_mysql mysql -u inventory_user -p
```

## Recursos Adicionales

### Documentación

- Swagger UI: `http://localhost:5001/swagger` (en ejecución)
- Tests: [Tests/README.md](Tests/README.md)
- Schema SQL: [schema.sql](schema.sql)

### Dependencias del Proyecto

- **ASP.NET Core**: 8.0
- **Entity Framework Core**: 9.0.11
- **Pomelo.EntityFrameworkCore.MySql**: 9.0.0
- **Microsoft.AspNetCore.Authentication.JwtBearer**: 8.0.22
- **System.IdentityModel.Tokens.Jwt**: 8.15.0
- **Swashbuckle.AspNetCore**: 10.0.1
- **xUnit**: 2.9.3 (testing)
- **Moq**: 4.20.72 (testing)
- **Microsoft.EntityFrameworkCore.InMemory**: 9.0.0 (testing)

### Repositorio

- GitHub: [Bochoventario](https://github.com/AngelLizardo-Waggamer/Bochoventario)
- Branch: inventoryImplementation

## Licencia

Este proyecto es parte de una actividad integradora de DevOps.

## Contacto

Para preguntas o issues, por favor contactar al equipo de desarrollo o crear un issue en el repositorio de GitHub.
