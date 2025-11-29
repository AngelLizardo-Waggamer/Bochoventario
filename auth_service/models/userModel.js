// userModel.js: Funciones para interactuar con la tabla 'users' en MySQL.

import pool from '../db.js';

/**
 * Inserta un nuevo usuario en la base de datos con una contraseña hasheada.
 * Se usa en el endpoint de registro (/auth/register).
 * @param {string} nombre_usuario - Nombre de usuario (debe ser único).
 * @param {string} password_hash - Contraseña ya hasheada (desde passwordUtils).
 * @returns {Promise<number>} El ID del usuario recién creado.
 */
 // FIX: Definimos un rol por defecto (Ej: 2 = Usuario).
const ID_ROL_DEFAULT = 2;
export async function createUser(nombre_usuario, password_hash, id_rol) {
    const query = `
        INSERT INTO Usuarios (nombre_usuario, password_hash, id_rol)
        VALUES (?, ?, ?);
    `;
    
    try {
        // Ejecutamos la consulta usando un array [nombre_usuario, password_hash] para evitar inyección SQL.
        const [result] = await pool.execute(query, [nombre_usuario, password_hash,id_rol]);
        
        // 'insertId' es la propiedad que devuelve mysql2/promise con el ID de la nueva fila.
        return result.insertId; 
    } catch (error) {
        // Manejo específico del error de duplicidad (código 1062 en MySQL)
        if (error.code === 'ER_DUP_ENTRY') {
            throw new Error('nombre_usuario already exists.');
        }
        console.error('Error al crear usuario en DB:', error);
        throw new Error('Database error during user creation.');
    }
}

/**
 * Busca un usuario por su nombre de usuario.
 * Se usa en el endpoint de inicio de sesión (/auth/login).
 * @param {string} nombre_usuario - Nombre de usuario a buscar.
 * @returns {Promise<object | null>} Objeto de usuario si se encuentra, o null.
 */
export async function findBynombre_usuario(nombre_usuario) {
    // Solo seleccionamos los campos necesarios (el hash es crucial para el login)
    const query = `
        SELECT id_usuario, nombre_usuario, password_hash, id_rol
        FROM Usuarios
        WHERE nombre_usuario = ?;
    `;
    
    try {
        const [rows] = await pool.execute(query, [nombre_usuario]);
        
        // Si no hay filas, el usuario no existe.
        if (rows.length === 0) {
            return null;
        }

        // Devolvemos el primer resultado encontrado.
        return rows[0]; 
    } catch (error) {
        console.error('Error al buscar usuario en DB:', error);
        throw new Error('Database error during user retrieval.');
    }
}