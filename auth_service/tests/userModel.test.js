import { jest } from '@jest/globals';

// 1. Primero hacemos el mock de la conexión a la DB
// Esto intercepta cualquier llamada a '../db.js'
jest.unstable_mockModule('../db.js', () => ({
    default: {
        execute: jest.fn(), // Simulamos la función execute
    },
}));

// 2. Importamos dinámicamente el módulo que vamos a probar
// (Es necesario usar import() dinámico cuando usamos unstable_mockModule en ESM)
const { createUser, findBynombre_usuario } = await import('../models/userModel.js');
const pool = (await import('../db.js')).default;

describe('Pruebas de userModel (Mock DB)', () => {

    afterEach(() => {
        jest.clearAllMocks(); // Limpiar contadores después de cada test
    });

    test('createUser debe retornar el ID del nuevo usuario', async () => {
        // Simulamos que la DB responde: "OK, inserté el ID 5"
        // execute retorna un array: [rows, fields]. Aquí simulamos rows.
        pool.execute.mockResolvedValue([{ insertId: 5 }]);

        const userId = await createUser('nuevoUser', 'hash123', 2);

        expect(pool.execute).toHaveBeenCalledTimes(1);
        expect(userId).toBe(5);
    });

    test('findBynombre_usuario debe retornar el usuario si existe', async () => {
        const mockRow = { id_usuario: 1, nombre_usuario: 'juan', password_hash: 'abc', id_rol: 1 };
        
        // Simulamos que la DB encuentra 1 registro
        pool.execute.mockResolvedValue([[mockRow]]); 

        const user = await findBynombre_usuario('juan');

        expect(user).toEqual(mockRow);
        expect(user.nombre_usuario).toBe('juan');
    });

    test('findBynombre_usuario debe retornar null si no existe', async () => {
        // Simulamos que la DB devuelve un array vacío (no encontró nada)
        pool.execute.mockResolvedValue([[]]); 

        const user = await findBynombre_usuario('fantasma');

        expect(user).toBeNull();
    });
});