-- Creación de la base de datos (si no existe)
CREATE DATABASE IF NOT EXISTS `InventarioNormalizadoDB`;

-- Seleccionar la base de datos para usar
USE `InventarioNormalizadoDB`;

-- ---
-- 1. Tabla de Roles (Estandarización)
-- Solo contendrá los tres roles solicitados.
-- ---

CREATE TABLE `Roles` (
    `id_rol` INT NOT NULL AUTO_INCREMENT,
    `nombre_rol` VARCHAR(50) NOT NULL UNIQUE,
    PRIMARY KEY (`id_rol`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Inserción de los registros estandarizados (Administrador, Gestor, Lector)
INSERT INTO `Roles` (`nombre_rol`) VALUES
('Administrador'),
('Gestor'),
('Lector');

-- ---
-- 2. Tabla de Usuarios (Users)
-- Ahora usa una clave foránea (id_rol) que apunta a la tabla Roles.
-- ---

CREATE TABLE `Usuarios` (
    `id_usuario` INT NOT NULL AUTO_INCREMENT,
    `id_rol` INT NOT NULL, -- Clave foránea al rol
    `nombre_usuario` VARCHAR(50) NOT NULL UNIQUE,
    `password_hash` VARCHAR(255) NOT NULL,
    `nombre_completo` VARCHAR(100),
    `fecha_creacion` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id_usuario`),
    
    -- Definir clave foránea a la tabla Roles
    FOREIGN KEY (`id_rol`) REFERENCES `Roles`(`id_rol`)
        ON DELETE RESTRICT -- Evita borrar un rol si hay usuarios asociados
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ---
-- 3. Tabla de Artículos (InventoryItem)
-- Almacena la definición de cada producto.
-- ---

CREATE TABLE `Articulos` (
    `id_articulo` INT NOT NULL AUTO_INCREMENT,
    `sku` VARCHAR(50) NOT NULL UNIQUE,
    `nombre` VARCHAR(150) NOT NULL,
    `descripcion` TEXT,
    `precio_costo` DECIMAL(10, 2) DEFAULT 0.00,
    PRIMARY KEY (`id_articulo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ---
-- 4. Tabla de Inventario (Inventory)
-- Almacena el stock actual y su ubicación.
-- ---

CREATE TABLE `Inventario` (
    `id_inventario` INT NOT NULL AUTO_INCREMENT,
    `id_articulo` INT NOT NULL,
    `cantidad` INT NOT NULL DEFAULT 0,
    `ubicacion` VARCHAR(50),
    `ultima_modificacion_por` INT,
    `ultima_actualizacion` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id_inventario`),
    UNIQUE KEY `uk_articulo_ubicacion` (`id_articulo`, `ubicacion`),
    
    -- Relación con Articulos
    FOREIGN KEY (`id_articulo`) REFERENCES `Articulos`(`id_articulo`)
        ON DELETE CASCADE 
        ON UPDATE CASCADE,
        
    -- Relación con Usuarios para auditoría
    FOREIGN KEY (`ultima_modificacion_por`) REFERENCES `Usuarios`(`id_usuario`)
        ON DELETE SET NULL 
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;