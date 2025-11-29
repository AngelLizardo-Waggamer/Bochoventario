// jwtUtils.js: Funciones de utilidad para JWT con depuración mejorada.

import jwt from 'jsonwebtoken';

// TIEMPO DE EXPIRACIÓN
export const ACCESS_TOKEN_EXPIRY = '1h'; 

/**
 * Helper para obtener el secreto de forma segura.
 * Evita problemas de carga asíncrona de dotenv.
 */
function getSecret() {
    const secret = process.env.JWT_SECRET;
    if (!secret) {
        console.error("❌ ERROR CRÍTICO: JWT_SECRET no está definido en las variables de entorno.");
        throw new Error('JWT_SECRET not configured.');
    }
    return secret;
}

/**
 * Genera un nuevo Access Token.
 */
export function generateAccessToken(Usuarios) {
    const payload = {
        id_usuario: Usuarios.id_usuario,
        nombre_usuario: Usuarios.nombre_usuario,
        id_rol: Usuarios.id_rol,
    };

    // Usamos getSecret() aquí para asegurar que leemos el valor actual
    return jwt.sign(payload, getSecret(), { expiresIn: ACCESS_TOKEN_EXPIRY });
}

/**
 * Verifica si un token es válido.
 */
export function verifyToken(token) {
    try {
        // Limpiamos el token de posibles espacios en blanco al copiar/pegar
        const cleanToken = token.trim();
        
        return jwt.verify(cleanToken, getSecret());
    } catch (error) {
        // ESTO ES CLAVE: Imprimimos el error real en la consola del servidor
        console.error("⚠️ Error al verificar token:", error.message);
        
        // Lanzamos el error genérico para el cliente (seguridad)
        throw new Error('Token inválido o expirado');
    }
}

/**
 * Decodifica sin verificar (solo debug).
 */
export function decodeToken(token) {
    try {
        return jwt.decode(token);
    } catch (error) {
        return null;
    }
}