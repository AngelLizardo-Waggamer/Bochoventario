# Microservicio de Inventario

Microservicio RESTful para la gestión de productos y control de inventario. Proporciona endpoints para administrar artículos y sus registros de stock en múltiples ubicaciones.

## Tecnologías

- ASP.NET Core 8.0
- Entity Framework Core
- MySQL 8.4
- JWT Authentication
- Docker

## Autenticación y Autorización

La mayoría de los endpoints requieren autenticación mediante JWT. Los roles disponibles son:

- **Administrador (ID: 1)**: Acceso completo a todos los endpoints
- **Gestor (ID: 2)**: Acceso completo a todos los endpoints
- **Lector (ID: 3)**: Solo lectura (GET)

Los endpoints de escritura (POST, PUT, PATCH, DELETE) requieren rol de Administrador o Gestor.

## Endpoints - Gestión de Productos

### 1. Listar Productos

**Método:** `GET`  
**Ruta:** `/api/inventory`  
**Autenticación:** No requerida

**Parámetros de consulta (Query):**
- `q` (string, opcional): Búsqueda por nombre, SKU o descripción del producto
- `category` (string, opcional): Filtro por categoría (busca en descripción)

**Descripción:** Obtiene una lista de todos los productos registrados. Permite filtrar por texto de búsqueda general y categoría. Si no se especifican filtros, retorna todos los productos.

**Respuesta exitosa (200):**
```json
[
  {
    "idArticulo": 1,
    "sku": "PROD-001",
    "nombre": "Producto Ejemplo",
    "descripcion": "Descripción del producto",
    "precioCosto": 150.00
  }
]
```

---

### 2. Obtener Producto por ID

**Método:** `GET`  
**Ruta:** `/api/inventory/{id}`  
**Autenticación:** No requerida

**Parámetros de ruta (Path):**
- `id` (int, requerido): ID del producto

**Descripción:** Obtiene el detalle completo de un producto específico incluyendo sus registros de inventario asociados. Retorna un error 404 si el producto no existe.

**Respuesta exitosa (200):**
```json
{
  "idArticulo": 1,
  "sku": "PROD-001",
  "nombre": "Producto Ejemplo",
  "descripcion": "Descripción del producto",
  "precioCosto": 150.00,
  "inventarios": [...]
}
```

---

### 3. Crear Producto

**Método:** `POST`  
**Ruta:** `/api/inventory`  
**Autenticación:** Requerida (Administrador o Gestor)

**Cuerpo de la petición (Body):**
```json
{
  "articulo": {
    "sku": "PROD-001",
    "nombre": "Producto Nuevo",
    "descripcion": "Descripción opcional",
    "precioCosto": 150.00
  }
}
```

**Descripción:** Crea un nuevo producto en el sistema. Valida que el SKU sea único. El usuario debe tener permisos de Administrador o Gestor. Retorna el producto creado con su ID generado.

**Respuestas:**
- 201 Created: Producto creado exitosamente
- 401 Unauthorized: Token JWT inválido o usuario sin permisos
- 409 Conflict: Ya existe un producto con ese SKU

---

### 4. Actualizar Producto

**Método:** `PUT`  
**Ruta:** `/api/inventory/{id}`  
**Autenticación:** Requerida (Administrador o Gestor)

**Parámetros de ruta (Path):**
- `id` (int, requerido): ID del producto a actualizar

**Cuerpo de la petición (Body):**
```json
{
  "articulo": {
    "idArticulo": 1,
    "sku": "PROD-001",
    "nombre": "Producto Actualizado",
    "descripcion": "Nueva descripción",
    "precioCosto": 200.00
  }
}
```

**Descripción:** Actualiza la información de un producto existente. Valida que el SKU no esté duplicado con otro producto. También actualiza la fecha y usuario de modificación en todos los registros de inventario asociados.

**Respuestas:**
- 204 No Content: Actualización exitosa
- 400 Bad Request: El ID del producto no coincide
- 401 Unauthorized: Usuario sin permisos
- 404 Not Found: Producto no encontrado
- 409 Conflict: SKU duplicado

---

### 5. Eliminar Producto

**Método:** `DELETE`  
**Ruta:** `/api/inventory/{id}`  
**Autenticación:** Requerida (Administrador o Gestor)

**Parámetros de ruta (Path):**
- `id` (int, requerido): ID del producto a eliminar

**Descripción:** Elimina un producto del sistema. Debido a las restricciones de integridad referencial en cascada, también eliminará todos los registros de inventario asociados al producto.

**Respuestas:**
- 204 No Content: Eliminación exitosa
- 401 Unauthorized: Usuario sin permisos
- 404 Not Found: Producto no encontrado

---

## Endpoints - Gestión de Inventario (Stock)

### 6. Obtener Inventario por Producto

**Método:** `GET`  
**Ruta:** `/api/inventory/stock/{idArticulo}`  
**Autenticación:** No requerida

**Parámetros de ruta (Path):**
- `idArticulo` (int, requerido): ID del artículo

**Descripción:** Obtiene todos los registros de inventario asociados a un producto específico. Incluye información del artículo y del usuario que realizó la última modificación. Útil para ver el stock del producto en diferentes ubicaciones.

**Respuesta exitosa (200):**
```json
[
  {
    "idInventario": 1,
    "idArticulo": 1,
    "cantidad": 100,
    "ubicacion": "Almacen A",
    "ultimaActualizacion": "2025-11-29T10:30:00",
    "ultimaModificacionPor": 1,
    "articulo": {...},
    "usuarioModificador": {...}
  }
]
```

---

### 7. Obtener Inventario por Ubicación

**Método:** `GET`  
**Ruta:** `/api/inventory/stock/location/{ubicacion}`  
**Autenticación:** No requerida

**Parámetros de ruta (Path):**
- `ubicacion` (string, requerido): Nombre de la ubicación

**Descripción:** Obtiene todos los registros de inventario en una ubicación específica. Incluye información completa del artículo y usuario modificador. Útil para auditorías de almacén o reportes por ubicación.

**Respuesta exitosa (200):**
```json
[
  {
    "idInventario": 1,
    "idArticulo": 1,
    "cantidad": 100,
    "ubicacion": "Almacen A",
    "ultimaActualizacion": "2025-11-29T10:30:00",
    "ultimaModificacionPor": 1,
    "articulo": {...},
    "usuarioModificador": {...}
  }
]
```

---

### 8. Listar Todo el Inventario

**Método:** `GET`  
**Ruta:** `/api/inventory/stock`  
**Autenticación:** No requerida

**Descripción:** Obtiene todos los registros de inventario del sistema. Incluye información completa de artículos y usuarios modificadores. Útil para reportes generales y auditorías completas del inventario.

**Respuesta exitosa (200):**
```json
[
  {
    "idInventario": 1,
    "idArticulo": 1,
    "cantidad": 100,
    "ubicacion": "Almacen A",
    "ultimaActualizacion": "2025-11-29T10:30:00",
    "ultimaModificacionPor": 1,
    "articulo": {...},
    "usuarioModificador": {...}
  }
]
```

---

### 9. Crear Registro de Inventario

**Método:** `POST`  
**Ruta:** `/api/inventory/stock`  
**Autenticación:** Requerida (Administrador o Gestor)

**Cuerpo de la petición (Body):**
```json
{
  "idArticulo": 1,
  "cantidad": 100,
  "ubicacion": "Almacen A"
}
```

**Descripción:** Crea un nuevo registro de inventario para un producto en una ubicación específica. Valida que el artículo exista y que no haya un registro duplicado (mismo artículo y ubicación). Registra automáticamente el usuario y fecha de creación.

**Respuestas:**
- 201 Created: Registro creado exitosamente
- 401 Unauthorized: Usuario sin permisos
- 404 Not Found: Artículo no encontrado
- 409 Conflict: Ya existe inventario para ese artículo en esa ubicación

---

### 10. Actualizar Cantidad de Inventario

**Método:** `PUT`  
**Ruta:** `/api/inventory/stock/{id}`  
**Autenticación:** Requerida (Administrador o Gestor)

**Parámetros de ruta (Path):**
- `id` (int, requerido): ID del registro de inventario

**Cuerpo de la petición (Body):**
```json
{
  "cantidad": 150
}
```

**Descripción:** Actualiza directamente la cantidad de un registro de inventario estableciendo un valor absoluto. Útil para correcciones de inventario o ajustes por conteo físico. Actualiza automáticamente la fecha y usuario de modificación.

**Respuestas:**
- 204 No Content: Actualización exitosa
- 401 Unauthorized: Usuario sin permisos
- 404 Not Found: Registro de inventario no encontrado

---

### 11. Ajustar Inventario (Entrada/Salida)

**Método:** `PATCH`  
**Ruta:** `/api/inventory/stock/{id}/adjust`  
**Autenticación:** Requerida (Administrador o Gestor)

**Parámetros de ruta (Path):**
- `id` (int, requerido): ID del registro de inventario

**Cuerpo de la petición (Body):**
```json
{
  "ajuste": 25
}
```

**Descripción:** Ajusta la cantidad de inventario de forma relativa. Acepta valores positivos para entradas de stock y negativos para salidas. Valida que el ajuste no resulte en cantidad negativa. Útil para operaciones de entrada de mercancía, ventas o transferencias.

**Respuestas:**
- 200 OK: Ajuste exitoso (retorna nueva cantidad)
- 400 Bad Request: El ajuste resultaría en stock negativo
- 401 Unauthorized: Usuario sin permisos
- 404 Not Found: Registro de inventario no encontrado

**Respuesta exitosa (200):**
```json
{
  "message": "Inventario ajustado. Nueva cantidad: 125",
  "cantidad": 125
}
```

---

### 12. Eliminar Registro de Inventario

**Método:** `DELETE`  
**Ruta:** `/api/inventory/stock/{id}`  
**Autenticación:** Requerida (Administrador o Gestor)

**Parámetros de ruta (Path):**
- `id` (int, requerido): ID del registro de inventario

**Descripción:** Elimina un registro de inventario específico. No elimina el artículo asociado, solo el registro de stock en esa ubicación. Útil para limpiar ubicaciones que ya no se utilizan o corregir registros erróneos.

**Respuestas:**
- 204 No Content: Eliminación exitosa
- 401 Unauthorized: Usuario sin permisos
- 404 Not Found: Registro de inventario no encontrado

---

## Códigos de Estado HTTP

- **200 OK**: Operación de consulta exitosa
- **201 Created**: Recurso creado exitosamente
- **204 No Content**: Operación de modificación/eliminación exitosa
- **400 Bad Request**: Datos de entrada inválidos
- **401 Unauthorized**: Autenticación requerida o permisos insuficientes
- **404 Not Found**: Recurso no encontrado
- **409 Conflict**: Conflicto con el estado actual (duplicados)

## Ejecución

### Desarrollo Local

```bash
dotnet run
```

### Docker

```bash
docker-compose up -d
```

### Pruebas

```bash
# Ejecutar todas las pruebas
dotnet test

# Ejecutar con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

## Base de Datos

El microservicio utiliza MySQL 8.4. El schema se encuentra en `schema.sql` e incluye las siguientes tablas principales:

- **Articulos**: Definición de productos
- **Inventario**: Registros de stock por ubicación
- **Usuarios**: Usuarios del sistema
- **Roles**: Roles de autorización

Las relaciones incluyen claves foráneas con restricciones en cascada para mantener la integridad referencial.