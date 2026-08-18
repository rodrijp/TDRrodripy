import { createPortal } from 'react-dom';
import { DatumVisualizerChart } from './DatumVisualizerChart';
import { DatumVisualizerTable } from './DatumVisualizerTable';
import { useHistoricalData } from '../hooks/useHistoricalData';

interface DatumVisualizerModalProps {
  isOpen: boolean;
  dataId: number | null;
  isValue?: boolean;
  position?: 'left' | 'right' | null;
  onClose: () => void;
}

export const DatumVisualizerModal: React.FC<DatumVisualizerModalProps> = ({
  isOpen,
  dataId,
  isValue,
  position,
  onClose,
}) => {
  const { historicalData, loading, error } = useHistoricalData(isOpen, dataId);

  if (!isOpen || dataId == null) {
    return null;
  }

  const modal = (
    <div 
      className={`datum-visualizer-popup-backdrop${position ? ` datum-visualizer-${position}` : ''}`} 
      onClick={onClose}
    >
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
          <DatumVisualizerChart data={historicalData} isValue={isValue} />

          <div className="datum-visualizer-table-container">
            <DatumVisualizerTable data={historicalData} isValue={isValue} loading={loading} error={error} />
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

  return createPortal(modal, document.body);
};
