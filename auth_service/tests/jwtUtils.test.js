import { jest } from '@jest/globals'; // Importamos jest explícitamente para ESM
import { generateAccessToken, verifyToken } from '../utils/jwtUtils.js';

// Configuramos una variable de entorno falsa para la prueba
process.env.JWT_SECRET = 'test_secret_key_123';

describe('Pruebas de jwtUtils', () => {
    
    // Datos de prueba simulando un usuario de la DB
    const mockUser = {
        id_usuario: 1,
        nombre_usuario: 'testuser',
        id_rol: 2
    };

    test('Debe generar un token válido', () => {
        const token = generateAccessToken(mockUser);
        
        expect(typeof token).toBe('string');
        // Verificamos que tenga 3 partes (header.payload.signature)
        expect(token.split('.').length).toBe(3);
    });

    test('Debe verificar y decodificar un token correcto', () => {
        const token = generateAccessToken(mockUser);
        const decoded = verifyToken(token);

        expect(decoded).toHaveProperty('id_usuario', 1);
        expect(decoded).toHaveProperty('nombre_usuario', 'testuser');
        expect(decoded).toHaveProperty('id_rol', 2);
    });

    test('Debe lanzar error con un token alterado o falso', () => {
        const tokenFalso = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.falso.falso';
        
        // 1. "Espiamos" console.error y lo silenciamos temporalmente
        const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

        // 2. Ejecutamos la prueba que sabemos que fallará
        expect(() => {
            verifyToken(tokenFalso);
        }).toThrow('Token inválido o expirado');

        // 3. (Opcional) Verificamos que el código intentó imprimir el error
        expect(consoleSpy).toHaveBeenCalled();

        // 4. Restauramos la consola para no afectar otros tests
        consoleSpy.mockRestore();
    });
});