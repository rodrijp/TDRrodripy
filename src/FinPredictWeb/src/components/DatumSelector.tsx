import { useEffect, useState } from 'react';
import type { components } from '../api/schema';
import { api } from '../api/client';

type Datum = components['schemas']['Datum'];

interface DatumSelectorProps {
  selectedDataId?: number | null;
  onDataIdChange: (dataId: number | null) => void;
  label?: string;
}

export const DatumSelector: React.FC<DatumSelectorProps> = ({
  selectedDataId = null,
  onDataIdChange,
  label = 'Seleccionar Activo:',
}) => {
  const [datums, setDatums] = useState<Datum[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDatums = async () => {
      try {
        setLoading(true);
        const data = await api.getData();
        setDatums(data || []);
        setError(null);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
        console.error('Error fetching datums:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchDatums();
  }, []);

  const selectedDatum = datums.find(d => d.dataId === selectedDataId);

  return (
    <div className="datum-selector">
      <label>{label}</label>
      {loading && <p>Cargando datos...</p>}
      {error && <p style={{ color: 'red' }}>Error: {error}</p>}
      {!loading && (
        <select
          value={selectedDataId ?? ''}
          onChange={(e) => {
            const value = e.target.value;
            onDataIdChange(value ? parseInt(value, 10) : null);
          }}
        >
          <option value="">-- Seleccionar --</option>
          {datums.map((datum) => (
            <option key={datum.dataId} value={datum.dataId}>
              {datum.dataName || 'Sin nombre'}
            </option>
          ))}
        </select>
      )}
    </div>
  );
};
