import { useEffect, useState } from 'react';
import type { components } from '../api/schema';
import { api } from '../api/client';
import { DatumVisualizerModal } from './DatumVisualizerModal';

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
  const [isPopupOpen, setIsPopupOpen] = useState(false);

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

  const handleOpenPopup = () => {
    if (selectedDataId != null) {
      setIsPopupOpen(true);
    }
  };

  const selectedDatum = datums.find((d) => d.dataId === selectedDataId);

  return (
    <div className="datum-selector">
      <label>{label}</label>
      {loading && <p>Cargando datos...</p>}
      {error && <p style={{ color: 'red' }}>Error: {error}</p>}
      {!loading && (
        <div className="datum-selector-row">
          <select
            value={selectedDataId ?? ''}
            onChange={(e) => {
              const value = e.target.value;
              const nextDataId = value ? parseInt(value, 10) : null;
              onDataIdChange(nextDataId);
              if (nextDataId == null) {
                setIsPopupOpen(false);
              }
            }}
          >
            <option value="">-- Seleccionar --</option>
            {datums.map((datum) => (
              <option key={datum.dataId} value={datum.dataId}>
                {datum.dataName || 'Sin nombre'}
              </option>
            ))}
          </select>

          <button
            type="button"
            className={`datum-visualize-button${selectedDataId == null ? ' is-disabled' : ''}`}
            title={selectedDataId == null ? 'Debes seleccionar el activo' : 'Visualizar datos'}
            aria-label={selectedDataId == null ? 'Debes seleccionar el activo' : `Visualizar datos del activo ${selectedDataId}`}
            onClick={handleOpenPopup}
            disabled={selectedDataId == null}
          >
            <svg viewBox="0 0 64 64" aria-hidden="true" focusable="false">
              <rect x="8" y="10" width="22" height="18" rx="3" />
              <rect x="34" y="10" width="22" height="12" rx="3" />
              <rect x="8" y="34" width="22" height="20" rx="3" />
              <rect x="34" y="26" width="22" height="28" rx="3" />
              <circle cx="45" cy="41" r="9" />
              <path d="M51 47 L58 54" />
            </svg>
          </button>
        </div>
      )}

      <DatumVisualizerModal
        isOpen={isPopupOpen}
        dataId={selectedDataId}
        isValue={selectedDatum?.isValue}
        onClose={() => setIsPopupOpen(false)}
      />
    </div>
  );
};
