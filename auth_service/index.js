// index.js: Punto de entrada OPTIMIZADO del Microservicio de Autenticación
import 'dotenv/config'; 
import express from 'express';
import pool from './db.js'; // CORREGIDO: Usa ./db.js porque están en la misma carpeta

import * as userModel from './models/userModel.js';
import * as passwordUtils from './utils/passwordUtils.js';
import * as jwtUtils from './utils/jwtUtils.js';

const app = express();
const PORT = process.env.PORT || 3000; 

app.use(express.json());

// Ruta de Salud
app.get('/', (req, res) => {
    res.status(200).json({ message: 'JWT Auth Microservice running successfully. 🚀', status: 'OK' });
});

/**
 * RUTA: POST /auth/register
 */
app.post('/auth/register', async (req, res) => {
    try {
        const { nombre_usuario, password, id_rol } = req.body;
        
        if (!nombre_usuario || !password || !id_rol) {
            return res.status(400).json({ 
                error: 'Faltan datos. Se requiere: nombre_usuario, password e id_rol.' 
            });
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
        if (error.message.includes('id_rol especificado no existe')) {
            return res.status(400).json({ error: 'El ID de rol proporcionado no es válido.' });
        }
        
        console.error('Registration Error:', error.message);
        res.status(500).json({ error: 'Error interno del servidor al registrar.' });
    }
});

/**
 * RUTA: POST /auth/login
 */
app.post('/auth/login', async (req, res) => {
    try {
        const { nombre_usuario, password } = req.body;

        if (!nombre_usuario || !password) {
            return res.status(400).json({ error: 'nombre_usuario y password son requeridos.' });
        }
        
        const user = await userModel.findBynombre_usuario(nombre_usuario);
        
        if (!user) {
            return res.status(401).json({ error: 'Credenciales inválidas.' });
        }
        
        const passwordMatch = await passwordUtils.comparePassword(password, user.password_hash);

        if (passwordMatch) {
             // Generamos el token usando la función correcta de jwtUtils
             const token = jwtUtils.generateAccessToken(user);
             
             return res.status(200).json({ token });
         } else {
             return res.status(401).json({ error: 'Credenciales inválidas.' });
         }

    } catch (error) {
        console.error('Login Error:', error.message);
        res.status(500).json({ 
            error: 'Error interno del servidor.',
            debug_info: error.message 
        });
    }
});

app.listen(PORT, () => {
    console.log(`\n======================================================`);
    console.log(` 🚀 Auth Service escuchando en puerto ${PORT}`);
    console.log(`======================================================\n`);
});