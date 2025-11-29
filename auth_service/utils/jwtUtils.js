// jwtUtils.js: Funciones de utilidad para la generación y firma de JSON Web Tokens (JWT).

import jwt from 'jsonwebtoken';

// Obtiene la clave secreta del entorno inyectada por Docker Compose.
export const JWT_SECRET = process.env.JWT_SECRET; 

// Tiempo de expiración del token de acceso.
export const ACCESS_TOKEN_EXPIRY = '1h'; // 1 hora de validez para el token de acceso


/**
 * Genera un nuevo Access Token (JWT) para un usuario autenticado.
 * @param {object} user - Objeto de usuario obtenido de la base de datos (debe tener id, username, y role).
 * @returns {string} El JWT firmado y codificado.
 */
export function generateAccessToken(user) {
    if (!JWT_SECRET) {
        // Fallo crítico: la clave no está configurada.
        throw new Error('JWT_SECRET not configured. Token generation failed.');
    }
    
    // El payload contiene las 'claims' del token.
    const payload = {
        id: user.id,         // ID único del usuario (esencial)
        username: user.username, // Nombre de usuario
        role: user.role,     // Rol/Permisos del usuario
    };

    // jwt.sign() toma el payload, lo firma con la clave secreta y le añade el tiempo de expiración.
    const token = jwt.sign(
        payload, 
        JWT_SECRET, 
        { expiresIn: ACCESS_TOKEN_EXPIRY }
    );

    return token;
}

// -----------------------------------------------------------
// NOTA: La función verifyToken() se usaría en un Middleware en los microservicios
// de consumo (ej. inventory_service) para validar el token que envíe el cliente.
// -----------------------------------------------------------S