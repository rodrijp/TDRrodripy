import { useEffect, useState } from 'react';
import { api, type DataRelation } from '../api/client';

interface DataRelationCardProps {
  leftDataId: number | null;
  rightDataId: number | null;
}

const formatValue = (value: number | null | undefined) =>
  value == null ? '—' : value.toFixed(4);

export const DataRelationCard: React.FC<DataRelationCardProps> = ({
  leftDataId,
  rightDataId,
}) => {
  const [relation, setRelation] = useState<DataRelation | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (leftDataId == null || rightDataId == null) {
      setRelation(null);
      setError(null);
      setLoading(false);
      return;
    }

    let active = true;

    const fetchRelation = async () => {
      setLoading(true);
      setError(null);

      try {
        const result = await api.getDataRelation(leftDataId, rightDataId);
        if (active) {
          setRelation(result);
        }
      } catch (err) {
        if (active) {
          setRelation(null);
          setError(
            err instanceof Error
              ? err.message
              : 'Error desconocido al cargar la relación de datos.'
          );
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    };

    fetchRelation();

    return () => {
      active = false;
    };
  }, [leftDataId, rightDataId]);

  return (
    <section className="data-relation-card">
      {leftDataId == null || rightDataId == null ? (
        <p>Selecciona ambos activos para ver su relación.</p>
      ) : loading ? (
        <p>Cargando relación entre {leftDataId} y {rightDataId}...</p>
      ) : error ? (
        <p className="data-relation-card-error">Error: {error}</p>
      ) : relation ? (
        <dl>
          <div className="relation-row">
            <dt>Covariancia</dt>
            <dd>{formatValue(relation.covariance)}</dd>
          </div>

          <div className="relation-row">
            <dt>Correlacion</dt>
            <dd>{formatValue(relation.correlation)}</dd>
          </div>

          <div className="relation-row">
            <dt>Correlacion Logaritmica</dt>
            <dd>{formatValue(relation.correlationLog)}</dd>
          </div>
        </dl>
      ) : (
        <p>No se encontró información de relación para los activos seleccionados.</p>
      )}
    </section>
  );
};
