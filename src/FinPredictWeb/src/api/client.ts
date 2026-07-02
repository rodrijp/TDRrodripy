import createClient from 'openapi-fetch';
import type { paths } from './schema';

// Configurar la URL base de la API
const BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:7269';

// Crear el cliente con type-safety
export const apiClient = createClient<paths>({
  baseUrl: BASE_URL,
  // Configuración adicional si es necesaria
  credentials: 'include', // Para incluir cookies si las usas
});

// Exportar métodos específicos para comodidad
export const api = {
  // Obtener todos los datos
  async getData() {
    const { data, error } = await apiClient.GET('/api/data');
    if (error) throw new Error('Error fetching data');
    return data;
  },

  // Obtener datos históricos por ID
  async getHistoricalData(dataId: number) {
    const { data, error } = await apiClient.GET('/api/HistoricalData/{dataId}', {
      params: { path: { dataId } },
    });
    if (error) throw new Error(`Error fetching historical data for id ${dataId}`);
    return data;
  },
};
