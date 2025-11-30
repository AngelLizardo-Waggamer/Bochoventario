import mysql from 'mysql2/promise';
import dotenv from 'dotenv';

// Cargar las variables del archivo .env
dotenv.config();

// Crear la pool de conexiones
const pool = mysql.createPool({
    host: process.env.DB_HOST, 
    user: process.env.DB_USER, 
    password: process.env.DB_PASSWORD,
    database: process.env.DB_NAME,
    // Configuraciones recomendada para la Pool
    waitForConnections: true, // Si todas las conexiones están usadas, espera a que se libere una
    connectionLimit: 10,      // Número máximo de conexiones simultáneas
    queueLimit: 0,            // 0 significa que no hay límite de peticiones en cola
    enableKeepAlive: true,    // Mantiene las conexiones abiertas para evitar el overhead de reconexión
    keepAliveInitialDelay: 0,
});

// (Opcional) Verificar la conexión inicial para debug
pool.getConnection()
    .then(connection => {
        pool.releaseConnection(connection);
        console.log('✅ Base de datos conectada exitosamente a:', process.env.DB_NAME);
    })
    .catch(err => {
        console.error('❌ Error al conectar a la base de datos:', err.message);
    });

export default pool;