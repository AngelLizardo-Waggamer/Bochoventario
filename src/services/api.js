import axios from 'axios';

const api = axios.create({
    baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5001',
});

api.interceptors.request.use((config => {
    const token = localStorage.getItem('token');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
}, error => {
    return Promise.reject(error);
}));

export const inventoryService = {
  // GET: Obtener lista
  getAll: async () => {
    const response = await api.get('/api/inventory');
    return response.data.map(item => ({
      id: item.IdArticulo,
      sku: item.Sku,
      name: item.Nombre,
      description: item.Descripcion,
      price: item.PrecioCosto,
      quantity: 0 // Provisional
    }));
  },

  // POST: Crear producto
  create: async (productData) => {
    // Estructura requerida: { "Articulo": { ... } }
    const payload = {
      Articulo: {
        Sku: productData.sku,
        Nombre: productData.name,
        Descripcion: productData.description,
        PrecioCosto: parseFloat(productData.price)
      }
    };
    return await api.post('/api/inventory', payload);
  },

  // DELETE: Borrar producto
  delete: async (id) => {
    return await api.delete(`/api/inventory/${id}`);
  }
};

export default api;