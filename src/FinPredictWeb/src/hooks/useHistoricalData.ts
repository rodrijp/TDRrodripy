import { useEffect, useState } from 'react';
import type { components } from '../api/schema';
import { api } from '../api/client';

export type HistoricalDatum = components['schemas']['HistoricalDatum'];

export const useHistoricalData = (isOpen: boolean, dataId: number | null) => {
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

  return { historicalData, loading, error };
};
