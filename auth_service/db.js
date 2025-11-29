// db.js: Configuración del Pool de Conexiones a la base de datos MySQL.
// Este módulo solo se encarga de establecer la conexión, sin inicializar tablas.

import mysql from 'mysql2/promise';

// Configuración obtenida de las variables de entorno inyectadas por Docker Compose
// Los valores predeterminados (ej. 'localhost') solo se usan si las variables de entorno no están definidas.
const config = {
    host: process.env.DB_HOST || 'localhost',
    user: process.env.DB_USER || 'user',
    password: process.env.DB_PASSWORD || 'bochovpword_?',
    database: process.env.DB_NAME || 'authdb',
    // Opciones del Pool:
    waitForConnections: true,
    connectionLimit: 10, // Número máximo de conexiones simultáneas
    queueLimit: 0        // Cola infinita para peticiones
};

// Crear y exportar el Pool de Conexiones para usarlo en 'userModel.js'
const pool = mysql.createPool(config);

// Exportar el pool de conexiones para usarlo en el resto de la aplicación
export default pool;