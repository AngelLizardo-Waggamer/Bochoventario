// index.js: Punto de entrada del Microservicio de Autenticación
// Configura el servidor Express, carga variables de entorno y define las rutas.

// 1. IMPORTACIONES
import 'dotenv/config'; // Carga las variables de entorno al inicio
import express from 'express';
import dbPool from './db.js'; // Conexión a la DB

// Importaciones de módulos internos
import * as userModel from './models/userModel.js';
import * as passwordUtils from './utils/passwordUtils.js';
import * as jwtUtils from './utils/jwtUtils.js';

// 2. CONFIGURACIÓN INICIAL
const app = express();
// Puerto de la aplicación (usará el 3000 definido en docker-compose)
const PORT = process.env.PORT || 3000; 

// Middlewares: Permite a Express leer JSON en el cuerpo de las peticiones
app.use(express.json());


// 3. RUTAS DE AUTENTICACIÓN (LOGIN y REGISTER)

// Ruta de Salud/Prueba
app.get('/', (req, res) => {
    res.status(200).json({ 
        message: 'JWT Auth Microservice running successfully. 🚀',
        environment: process.env.NODE_ENV || 'development'
    });
});

// Ruta POST para el REGISTRO de un nuevo usuario
app.post('/auth/register', async (req, res) => {
    try {
        const { username, password } = req.body;
        
        // --- LOGICA PENDIENTE ---
        // 1. Validar datos (username y password no vacíos)
        if (!username || !password) {
            return res.status(400).json({ error: 'Username and password are required.' });
        }
        
        // 2. Hashear la contraseña (usando passwordUtils.hashPassword)
        const hashedPassword = await passwordUtils.hashPassword(password);
        
        // 3. Guardar el usuario en la DB (usando userModel.createUser)
        await userModel.createUser(username, hashedPassword); 
        
        // 4. Responder
        res.status(201).json({ 
            message: 'User registered successfully (DUMMY RESPONSE).', 
            // TODO: Eliminar después de implementar la lógica real
            received: { username, password } 
        });

    } catch (error) {
        // Manejo de errores de duplicidad de usuario o DB
        console.error('Registration Error:', error.message);
        res.status(500).json({ error: 'Internal Server Error during registration.' });
    }
});


// Ruta POST para el INICIO DE SESIÓN (LOGIN)
app.post('/auth/login', async (req, res) => {
    try {
        const { username, password } = req.body;

        if (!username || !password) {
            return res.status(400).json({ error: 'Username and password are required.' });
        }
        
        // --- LOGICA PENDIENTE ---
        // 1. Buscar usuario en la DB (userModel.findByUsername)
        const user = await userModel.findByUsername(username);
        
        // 2. Verificar la contraseña (passwordUtils.comparePassword)
        const passwordMatch = await passwordUtils.comparePassword(password, user.password_hash);

        // 3. Si es válida, generar JWT
        if (passwordMatch) {
             const token = jwtUtils.generateToken(user);
             return res.status(200).json({ token });
         } else {
             return res.status(401).json({ error: 'Invalid username or password.' });
         }

    } catch (error) {
        console.error('Login Error:', error.message);
        // En un login, es mejor no dar detalles del error por seguridad.
        res.status(401).json({ error: 'Invalid credentials or Internal Server Error.' });
    }
});


// 4. INICIO DEL SERVIDOR

app.listen(PORT, () => {
    console.log(`\n======================================================`);
    console.log(` Microservicio JWT Auth escuchando en puerto ${PORT} `);
    console.log(` Acceso: http://localhost:${PORT}`);
    console.log(`======================================================\n`);
    
    // El pool de DB ya se inicializó en db.js
    // Se puede agregar una verificación final de la conexión si es necesario.
});