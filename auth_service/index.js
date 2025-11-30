// index.js: Punto de entrada con depuración mejorada
import 'dotenv/config'; 
import express from 'express';
import pool from './db.js'; 

import * as userModel from './models/userModel.js';
import * as passwordUtils from './utils/passwordUtils.js';
import * as jwtUtils from './utils/jwtUtils.js';

const app = express();
const PORT = process.env.PORT || 5002; 

app.use(express.json());

// --- MIDDLEWARE DE AUTORIZACIÓN (ADMIN) ---
const verifyAdmin = (req, res, next) => {
    const authHeader = req.headers['authorization'];
    
    // Mejoramos la extracción del token para evitar errores de formato
    // "Bearer <token>" -> split(' ')[1]
    const token = authHeader && authHeader.split(' ')[1]; 

    if (!token) {
        return res.status(401).json({ error: 'Acceso denegado. Token no proporcionado.' });
    }

    try {
        // El verifyToken actualizado se encargará de limpiar espacios
        const decoded = jwtUtils.verifyToken(token);
        
        if (decoded.id_rol !== 1) {
            return res.status(403).json({ error: 'Acceso prohibido. Se requieren permisos de administrador.' });
        }

        req.user = decoded;
        next(); 
    } catch (error) {
        // Aquí no devolvemos el error.message original por seguridad, pero ya lo vimos en consola gracias a jwtUtils
        return res.status(403).json({ error: 'Token inválido o expirado.' });
    }
};

// ... (El resto del código de index.js sigue igual, las rutas login/register etc) ...

// Ruta de Salud
app.get('/', (req, res) => {
    res.status(200).json({ message: 'JWT Auth Microservice running successfully. 🚀', status: 'OK' });
});

app.post('/auth/register', async (req, res) => {
    try {
        const { nombre_usuario, password, id_rol } = req.body;
        
        if (!nombre_usuario || !password || !id_rol) {
            return res.status(400).json({ error: 'Faltan datos.' });
        }
        
        const password_hash = await passwordUtils.hashPassword(password);
        const userId = await userModel.createUser(nombre_usuario, password_hash, id_rol); 
        
        res.status(201).json({ 
            message: 'Usuario registrado exitosamente.', 
            userId: userId,
            rolAsignado: id_rol
        });
    } catch (error) {
        if (error.message.includes('already exists')) {
            return res.status(409).json({ error: 'El nombre de usuario ya está en uso.' });
        }
        console.error('Registration Error:', error.message);
        res.status(500).json({ error: 'Error interno.' });
    }
});

app.post('/auth/login', async (req, res) => {
    try {
        const { nombre_usuario, password } = req.body;
        if (!nombre_usuario || !password) return res.status(400).json({ error: 'Faltan credenciales.' });
        
        const user = await userModel.findBynombre_usuario(nombre_usuario);
        if (!user) return res.status(401).json({ error: 'Credenciales inválidas.' });
        
        const passwordMatch = await passwordUtils.comparePassword(password, user.password_hash);

        if (passwordMatch) {
             const token = jwtUtils.generateAccessToken(user);
             return res.status(200).json({ token });
         } else {
             return res.status(401).json({ error: 'Credenciales inválidas.' });
         }
    } catch (error) {
        console.error('Login Error:', error.message);
        res.status(500).json({ error: 'Error interno.' });
    }
});

app.get('/auth/users', verifyAdmin, async (req, res) => {
    try {
        const users = await userModel.getAllUsers();
        res.status(200).json(users);
    } catch (error) {
        res.status(500).json({ error: 'Error al obtener usuarios.' });
    }
});

app.put('/auth/update-role', verifyAdmin, async (req, res) => {
    const { id_usuario, new_id_rol } = req.body;
    if (!id_usuario || !new_id_rol) return res.status(400).json({ error: 'Faltan datos.' });

    try {
        const updated = await userModel.updateUserRole(id_usuario, new_id_rol);
        if (updated) res.status(200).json({ message: 'Rol actualizado.' });
        else res.status(404).json({ error: 'Usuario no encontrado.' });
    } catch (error) {
        res.status(500).json({ error: 'Error al actualizar rol.' });
    }
});

app.listen(PORT, () => {
    console.log(`\n======================================================`);
    console.log(` 🚀 Auth Service escuchando en puerto ${PORT}`);
    console.log(`======================================================\n`);
});