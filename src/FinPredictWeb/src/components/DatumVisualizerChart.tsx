import { useMemo } from 'react';
import {
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LineElement,
  LinearScale,
  PointElement,
  Tooltip,
} from 'chart.js';
import { Line } from 'react-chartjs-2';
import type { HistoricalDatum } from '../hooks/useHistoricalData';

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Tooltip, Legend);

interface DatumVisualizerChartProps {
  data: HistoricalDatum[];
  isValue?: boolean;
}

const formatChartDate = (value?: string) => {
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

export const DatumVisualizerChart: React.FC<DatumVisualizerChartProps> = ({ data, isValue }) => {
  const chartData = useMemo(() => {
    const sortedData = [...data].sort((left, right) => {
      const leftTime = left.date ? new Date(left.date).getTime() : 0;
      const rightTime = right.date ? new Date(right.date).getTime() : 0;
      return leftTime - rightTime;
    });

    return {
      labels: sortedData.map((item) => formatChartDate(item.date)),
      datasets: [
        {
          label: isValue === true ? 'Valor' : 'Porcentaje',
          data: sortedData.map((item) => item.value ?? 0),
          borderColor: '#60a5fa',
          backgroundColor: 'rgba(96, 165, 250, 0.2)',
          borderWidth: 2,
          tension: 0.3,
          fill: true,
          pointRadius: 3,
          pointHoverRadius: 5,
        },
      ],
    };
  }, [data, isValue]);

  const chartOptions = useMemo(
    () => ({
      responsive: true,
      maintainAspectRatio: false,
      interaction: {
        mode: 'index' as const,
        intersect: false,
      },
      plugins: {
        legend: {
          display: false,
        },
        tooltip: {
          callbacks: {
            label: (context: any) => {
              const value = Number(context.parsed.y ?? 0);
              return `${context.dataset.label}: ${value.toFixed(isValue === true ? 4 : 2)}${isValue === true ? '' : '%'}`;
            },
          },
        },
      },
      scales: {
        x: {
          title: {
            display: true,
            text: 'Fecha',
            color: '#cbd5e1',
          },
          ticks: {
            color: '#cbd5e1',
            maxRotation: 0,
            autoSkip: true,
          },
          grid: {
            color: 'rgba(148, 163, 184, 0.15)',
          },
        },
        y: {
          title: {
            display: true,
            text: isValue === true ? 'Valor' : 'Valor (%)',
            color: '#cbd5e1',
          },
          ticks: {
            color: '#cbd5e1',
          },
          grid: {
            color: 'rgba(148, 163, 184, 0.15)',
          },
        },
      },
    }),
    [isValue],
  );

  if (data.length === 0) {
    return <span>Sin datos para graficar</span>;
  }

  return (
    <div className="datum-visualizer-chart" aria-label="Gráfico de datos históricos">
      <Line data={chartData} options={chartOptions} />
    </div>
  );
};
