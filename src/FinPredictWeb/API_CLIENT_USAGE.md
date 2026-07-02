# API Client con OpenAPI-Fetch

## Configuración

El cliente está configurado en [src/api/client.ts](src/api/client.ts) y usa `openapi-fetch` para llamadas type-safe a tu API.

### URL de la API

La URL se configura mediante la variable de entorno `VITE_API_URL` en `.env.local`:
```
VITE_API_URL=https://localhost:7269
```

## Uso

### En tus componentes

```tsx
import { api } from '../api/client';

// Obtener todos los datos
const data = await api.getData();

// Obtener datos históricos
const historicalData = await api.getHistoricalData(dataId);
```

### Ventajas de openapi-fetch

✅ **Type-safe**: El cliente genera tipos automáticamente del schema OpenAPI  
✅ **Autocompletado**: IDE muestra sugerencias de endpoints disponibles  
✅ **Validación**: Errores de tipo detectados en tiempo de desarrollo  
✅ **Menos código**: No necesitas escribir URLs manualmente  

## Agregar nuevos endpoints

Cuando agregues nuevos endpoints a tu API:

1. Asegúrate de que tu WebApi genere un swagger.json actualizado
2. Regenera el schema:
   ```bash
   npx openapi-typescript swagger.json --output src/api/schema.ts
   ```
3. Agrega métodos helper en [src/api/client.ts](src/api/client.ts):
   ```tsx
   export const api = {
     // ... métodos existentes
     
     async getNuevoEndpoint(param: string) {
       const { data, error } = await apiClient.GET('/api/nuevo/{param}', {
         params: { path: { param } },
       });
       if (error) throw new Error('Error');
       return data;
     }
   };
   ```
