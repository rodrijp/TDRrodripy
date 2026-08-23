import { useMemo } from 'react';
import {
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LineElement,
  LinearScale,
  PointElement,
  Tooltip,
  type TooltipItem,
} from 'chart.js';
import { Line } from 'react-chartjs-2';
import { useHistoricalData, type HistoricalDatum } from '../hooks/useHistoricalData';

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Tooltip, Legend);

interface DataRelationChartProps {
  leftDataId: number;
  rightDataId: number;
  leftDataName?: string | null;
  rightDataName?: string | null;
}

const formatChartDate = (value: string) => {
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

const getDateKey = (item: HistoricalDatum) => item.date ?? '';

const sortHistoricalData = (data: HistoricalDatum[]) =>
  [...data].sort((left, right) => {
    const leftTime = left.date ? new Date(left.date).getTime() : 0;
    const rightTime = right.date ? new Date(right.date).getTime() : 0;
    return leftTime - rightTime;
  });

export const DataRelationChart: React.FC<DataRelationChartProps> = ({
  leftDataId,
  rightDataId,
  leftDataName,
  rightDataName,
}) => {
  const left = useHistoricalData(true, leftDataId);
  const right = useHistoricalData(true, rightDataId);

  const chartData = useMemo(() => {
    const dates = [...new Set(
      [...sortHistoricalData(left.historicalData), ...sortHistoricalData(right.historicalData)]
        .map(getDateKey)
        .filter(Boolean),
    )].sort((first, second) => new Date(first).getTime() - new Date(second).getTime());

    const valuesByDate = (data: HistoricalDatum[]) =>
      new Map(sortHistoricalData(data).map((item) => [getDateKey(item), item.value ?? null]));

    const leftValues = valuesByDate(left.historicalData);
    const rightValues = valuesByDate(right.historicalData);

    return {
      labels: dates.map(formatChartDate),
      datasets: [
        {
          label: leftDataName || `Activo ${leftDataId}`,
          data: dates.map((date) => leftValues.get(date) ?? null),
          borderColor: '#60a5fa',
          backgroundColor: '#60a5fa',
          borderWidth: 2,
          tension: 0.25,
          pointRadius: 2,
          pointHoverRadius: 4,
          spanGaps: true,
        },
        {
          label: rightDataName || `Activo ${rightDataId}`,
          data: dates.map((date) => rightValues.get(date) ?? null),
          borderColor: '#fbbf24',
          backgroundColor: '#fbbf24',
          borderWidth: 2,
          tension: 0.25,
          pointRadius: 2,
          pointHoverRadius: 4,
          spanGaps: true,
        },
      ],
    };
  }, [left.historicalData, leftDataId, leftDataName, right.historicalData, rightDataId, rightDataName]);

  const chartOptions = useMemo(
    () => ({
      responsive: true,
      maintainAspectRatio: false,
      interaction: { mode: 'index' as const, intersect: false },
      plugins: {
        legend: {
          labels: { color: '#e2e8f0' },
        },
        tooltip: {
          callbacks: {
            label: (context: TooltipItem<'line'>) => `${context.dataset.label}: ${Number(context.parsed.y ?? 0).toFixed(4)}`,
          },
        },
      },
      scales: {
        x: {
          title: { display: true, text: 'Tiempo', color: '#cbd5e1' },
          ticks: { color: '#cbd5e1', maxRotation: 0, autoSkip: true },
          grid: { color: 'rgba(148, 163, 184, 0.15)' },
        },
        y: {
          title: { display: true, text: 'Valor', color: '#cbd5e1' },
          ticks: { color: '#cbd5e1' },
          grid: { color: 'rgba(148, 163, 184, 0.15)' },
        },
      },
    }),
    [],
  );

  if (left.loading || right.loading) {
    return <p className="data-relation-chart-message">Cargando datos históricos...</p>;
  }

  if (left.error || right.error) {
    return <p className="data-relation-chart-message data-relation-card-error">{left.error ?? right.error}</p>;
  }

  if (chartData.labels.length === 0) {
    return <p className="data-relation-chart-message">Sin datos históricos para graficar.</p>;
  }

  return (
    <div className="data-relation-chart" aria-label="Gráfico comparativo de datos históricos">
      <Line data={chartData} options={chartOptions} />
    </div>
  );
};