// passwordUtils.js: Funciones de utilidad para el manejo seguro de contraseñas.
// Usamos bcrypt para hashear las contraseñas, lo cual es esencial para no
// guardarlas nunca en texto plano en la base de datos.

import bcrypt from 'bcrypt';

// Número de "salt rounds" (cuánto más alto, más seguro, pero más lento).
// 10 es un valor seguro y estándar para la mayoría de las aplicaciones web.
const saltRounds = 10;

/**
 * Genera un hash seguro para la contraseña proporcionada.
 * Esta función se usa al registrar un nuevo usuario.
 * @param {string} password - Contraseña en texto plano del usuario.
 * @returns {Promise<string>} El hash de la contraseña.
 */
export async function hashPassword(password) {
    try {
        // Genera el hash de forma asíncrona.
        const hash = await bcrypt.hash(password, saltRounds);
        return hash;
    } catch (error) {
        console.error("Error al hashear la contraseña:", error);
        // Lanza el error para que sea capturado por el endpoint de registro.
        throw new Error('Error processing password hash.');
    }
}

/**
 * Compara una contraseña en texto plano con un hash almacenado.
 * Esta función se usa durante el inicio de sesión.
 * @param {string} password - Contraseña ingresada por el usuario.
 * @param {string} hash - Hash de la contraseña almacenado en la base de datos.
 * @returns {Promise<boolean>} True si coinciden, False en caso contrario.
 */
export async function comparePassword(password, hash) {
    try {
        // Compara el texto plano con el hash de forma asíncrona.
        const match = await bcrypt.compare(password, hash);
        return match;
    } catch (error) {
        console.error("Error al comparar la contraseña:", error);
        // En caso de error (ej. hash inválido), asumimos que no hay coincidencia.
        return false; 
    }
}