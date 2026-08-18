import { useEffect, useState } from 'react';
import type { components } from '../api/schema';
import { api } from '../api/client';

type HistoricalDatum = components['schemas']['HistoricalDatum'];

interface DatumVisualizerModalProps {
  isOpen: boolean;
  dataId: number | null;
  isValue?: boolean;
  onClose: () => void;
}

export const DatumVisualizerModal: React.FC<DatumVisualizerModalProps> = ({
  isOpen,
  dataId,
  isValue,
  onClose,
}) => {
  const [historicalData, setHistoricalData] = useState<HistoricalDatum[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen || dataId == null) {
      setHistoricalData([]);
      setError(null);
      setLoading(false);
      return;
    }

    let ignore = false;

    const fetchHistoricalData = async () => {
      try {
        setLoading(true);
        setError(null);
        const response = await api.getHistoricalData(dataId);

        if (!ignore) {
          setHistoricalData(response ?? []);
        }
      } catch (err) {
        if (!ignore) {
          setError(err instanceof Error ? err.message : 'Error cargando los datos históricos');
          setHistoricalData([]);
        }
      } finally {
        if (!ignore) {
          setLoading(false);
        }
      }
    };

    fetchHistoricalData();

    return () => {
      ignore = true;
    };
  }, [isOpen, dataId]);

  if (!isOpen || dataId == null) {
    return null;
  }

  const formatDate = (value?: string) => {
    if (!value) {
      return '—';
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return new Intl.DateTimeFormat('es-ES', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    }).format(date);
  };

  const formatHistoricalValue = (value?: number) => {
    if (value == null) {
      return '—';
    }

    if (isValue === true) {
      return value.toFixed(4);
    }

    return `${(value).toFixed(2)} %`;
  };

  return (
    <div className="datum-visualizer-popup-backdrop" onClick={onClose}>
      <div
        className="datum-visualizer-popup"
        role="dialog"
        aria-modal="true"
        aria-labelledby="datum-visualizer-title"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="datum-visualizer-header">
          <h3 id="datum-visualizer-title">Visualizar datos</h3>
          <button type="button" className="datum-visualizer-close" onClick={onClose} aria-label="Cerrar popup">
            ×
          </button>
        </div>

        <div className="datum-visualizer-body">
          <div className="datum-visualizer-chart-placeholder" aria-label="Zona de gráfica reservada">
            <span>Gráfica</span>
          </div>

          <div className="datum-visualizer-table-container">
            {loading ? (
              <p className="datum-visualizer-status">Cargando historial...</p>
            ) : error ? (
              <p className="datum-visualizer-status datum-visualizer-error">{error}</p>
            ) : historicalData.length === 0 ? (
              <p className="datum-visualizer-status">No hay datos históricos para este activo.</p>
            ) : (
              <table className="datum-visualizer-table">
                <thead>
                  <tr>
                    <th>Fecha</th>
                    <th>{isValue === true ? 'Valor' : 'Porcentaje'}</th>
                  </tr>
                </thead>
                <tbody>
                  {historicalData.map((item) => (
                    <tr key={item.historicalDataId ?? `${item.date ?? 'fecha'}-${item.value ?? 'valor'}`}>
                      <td>{formatDate(item.date)}</td>
                      <td>{formatHistoricalValue(item.value)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>

        <div className="datum-visualizer-actions">
          <button type="button" className="datum-visualizer-primary" onClick={onClose}>
            Cerrar
          </button>
        </div>
      </div>
    </div>
  );
};
