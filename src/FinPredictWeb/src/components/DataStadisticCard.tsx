import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { DataStadistic } from '../api/client';

interface DataStadisticCardProps {
  dataId: number | null;
}

const formatPercent = (value: number | null | undefined) =>
  value == null ? '—' : `${(value * 100).toFixed(2)} %`;

const formatValue = (value: number | null | undefined) =>
  value == null ? '—' : value.toFixed(4);

export const DataStadisticCard: React.FC<DataStadisticCardProps> = ({ dataId }) => {
  const [statistics, setStatistics] = useState<DataStadistic | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (dataId == null) {
      setStatistics(null);
      setError(null);
      setLoading(false);
      return;
    }

    let active = true;
    const fetchStats = async () => {
      setLoading(true);
      setError(null);

      try {
        const result = await api.getDataStadistic(dataId);
        if (active) {
          setStatistics(result);
        }
      } catch (err) {
        if (active) {
          setStatistics(null);
          setError(err instanceof Error ? err.message : 'Error desconocido al cargar estadísticas.');
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    };

    fetchStats();

    return () => {
      active = false;
    };
  }, [dataId]);

  return (
    <section className="stat-card">
      <h2>Estadísticas del activo</h2>

      {dataId == null ? (
        <p>Selecciona un activo para ver su CAGR, volatilidad, Sortino y Sharpe.</p>
      ) : loading ? (
        <p>Cargando estadísticas para el id {dataId}...</p>
      ) : error ? (
        <p className="stat-card-error">Error: {error}</p>
      ) : statistics ? (
        <dl>
          <dt>CAGR</dt>
          <dd>{formatPercent(statistics.cagr)}</dd>

          <dt>Volatilidad cruda</dt>
          <dd>{formatValue(statistics.volatilidadcruda)}</dd>

          <dt>Volatilidad detendenciada</dt>
          <dd>{formatValue(statistics.volatilidaddetendenciada)}</dd>

          <dt>Sortino</dt>
          <dd>{formatValue(statistics.sortino)}</dd>

          <dt>Sharpe</dt>
          <dd>{formatValue(statistics.sharpe)}</dd>
        </dl>
      ) : (
        <p>No se encontraron estadísticas para el id {dataId}.</p>
      )}
    </section>
  );
};
