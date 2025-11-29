// userModel.js: Funciones para interactuar con la tabla 'users' en MySQL.

import pool from '../db.js';

/**
 * Inserta un nuevo usuario en la base de datos con una contraseña hasheada.
 * Se usa en el endpoint de registro (/auth/register).
 * @param {string} nombre_usuario - Nombre de usuario (debe ser único).
 * @param {string} password_hash - Contraseña ya hasheada (desde passwordUtils).
 * @param {number} id_rol - El rol asignado al usuario.
 * @returns {Promise<number>} El ID del usuario recién creado.
 */
export async function createUser(nombre_usuario, password_hash, id_rol) {
    const query = `
        INSERT INTO Usuarios (nombre_usuario, password_hash, id_rol)
        VALUES (?, ?, ?);
    `;
    
    try {
        // Ejecutamos la consulta usando un array para evitar inyección SQL.
        const [result] = await pool.execute(query, [nombre_usuario, password_hash, id_rol]);
        return result.insertId; 
    } catch (error) {
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
    const query = `
        SELECT id_usuario, nombre_usuario, password_hash, id_rol
        FROM Usuarios
        WHERE nombre_usuario = ?;
    `;
    
    try {
        const [rows] = await pool.execute(query, [nombre_usuario]);
        if (rows.length === 0) return null;
        return rows[0]; 
    } catch (error) {
        console.error('Error al buscar usuario en DB:', error);
        throw new Error('Database error during user retrieval.');
    }
}

/**
 * Obtiene la lista completa de usuarios (sin las contraseñas).
 * @returns {Promise<Array>} Lista de usuarios.
 */
export async function getAllUsers() {
    // Excluimos password_hash por seguridad
    const query = `
        SELECT id_usuario, nombre_usuario, id_rol, fecha_creacion 
        FROM Usuarios;
    `;
    
    try {
        const [rows] = await pool.execute(query);
        return rows;
    } catch (error) {
        console.error('Error al obtener usuarios:', error);
        throw new Error('Database error during fetching users.');
    }
}

/**
 * Actualiza el rol de un usuario específico.
 * @param {number} id_usuario - ID del usuario a modificar.
 * @param {number} new_id_rol - Nuevo ID de rol.
 * @returns {Promise<boolean>} True si se actualizó, False si el usuario no existe.
 */
export async function updateUserRole(id_usuario, new_id_rol) {
    const query = `
        UPDATE Usuarios 
        SET id_rol = ? 
        WHERE id_usuario = ?;
    `;
    
    try {
        const [result] = await pool.execute(query, [new_id_rol, id_usuario]);
        // affectedRows > 0 indica que encontró el usuario y realizó la operación
        return result.affectedRows > 0;
    } catch (error) {
        console.error('Error al actualizar rol:', error);
        throw new Error('Database error during role update.');
    }
}