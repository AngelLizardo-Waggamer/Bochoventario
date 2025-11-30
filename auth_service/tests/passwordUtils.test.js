import { hashPassword, comparePassword } from '../utils/passwordUtils.js';

describe('Pruebas de passwordUtils', () => {
  
  test('Debe hashear una contraseña correctamente', async () => {
    const password = 'miPasswordSeguro';
    const hash = await hashPassword(password);
    expect(hash).toBeDefined();
    expect(hash).not.toBe(password);
  });

  test('Debe verificar una contraseña válida', async () => {
    const password = 'bochoventario';
    const hash = await hashPassword(password);
    const esValida = await comparePassword(password, hash);
    expect(esValida).toBe(true);
  });
});