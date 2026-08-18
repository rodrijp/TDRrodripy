import { useEffect, useMemo, useState } from 'react';
import {
  columnFilteringFeature,
  columnVisibilityFeature,
  createFilteredRowModel,
  createSortedRowModel,
  filterFn_includesString,
  flexRender,
  globalFilteringFeature,
  rowSortingFeature,
  sortFn_alphanumeric,
  tableFeatures,
  useTable,
} from '@tanstack/react-table';
import type { components } from '../api/schema';
import { api } from '../api/client';

type HistoricalDatum = components['schemas']['HistoricalDatum'];

type DatumRow = {
  historicalDataId?: number | string;
  date: string;
  value?: number;
};

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
  const [columnFilters, setColumnFilters] = useState<any[]>([]);
  const [sorting, setSorting] = useState<any[]>([]);

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

  const tableData = useMemo<DatumRow[]>(() => historicalData.map((item) => ({
    historicalDataId: item.historicalDataId ?? `${item.date ?? 'fecha'}-${item.value ?? 'valor'}`,
    date: item.date ?? '',
    value: item.value,
  })), [historicalData]);

  const features = useMemo(
    () =>
      tableFeatures({
        columnFilteringFeature,
        columnVisibilityFeature,
        globalFilteringFeature,
        rowSortingFeature,
        filteredRowModel: createFilteredRowModel(),
        sortedRowModel: createSortedRowModel(),
        filterFns: { includesString: filterFn_includesString },
        sortFns: { alphanumeric: sortFn_alphanumeric },
      }),
    [],
  );

  const columns = useMemo(
    () => [
      {
        accessorKey: 'date',
        header: 'Fecha',
        cell: ({ row }: { row: { original: DatumRow } }) => formatDate(row.original.date),
        sortFn: 'alphanumeric' as const,
        filterFn: (row: any, columnId: string, filterValue: string) => {
          const rawValue = String(row.getValue(columnId) ?? '');
          const formattedValue = formatDate(rawValue);
          const search = String(filterValue ?? '').trim().toLowerCase();

          if (!search) {
            return true;
          }

          return rawValue.toLowerCase().includes(search) || formattedValue.toLowerCase().includes(search);
        },
      },
      {
        accessorKey: 'value',
        header: isValue === true ? 'Valor' : 'Porcentaje',
        cell: ({ row }: { row: { original: DatumRow } }) => formatHistoricalValue(row.original.value),
        sortFn: 'alphanumeric' as const,
        filterFn: (row: any, columnId: string, filterValue: string) => {
          const rawValue = row.getValue(columnId);
          const formattedValue = formatHistoricalValue(typeof rawValue === 'number' ? rawValue : Number(rawValue));
          const search = String(filterValue ?? '').trim().toLowerCase();

          if (!search) {
            return true;
          }

          return String(rawValue ?? '').toLowerCase().includes(search) || formattedValue.toLowerCase().includes(search);
        },
      },
    ],
    [formatDate, formatHistoricalValue, isValue],
  );

  const table = useTable({
    data: tableData,
    columns,
    features,
    state: {
      columnFilters,
      sorting,
    },
    onColumnFiltersChange: setColumnFilters,
    onSortingChange: setSorting,
  });

  if (!isOpen || dataId == null) {
    return null;
  }

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
              <>
                <table className="datum-visualizer-table">
                  <thead>
                    {table.getHeaderGroups().map((headerGroup: any) => (
                      <tr key={headerGroup.id}>
                        {headerGroup.headers.map((header: any) => (
                          <th
                            key={header.id}
                            onClick={header.column.getCanSort() ? header.column.getToggleSortingHandler() : undefined}
                            style={{ cursor: header.column.getCanSort() ? 'pointer' : 'default' }}
                          >
                            <div className="datum-visualizer-header-cell">
                              {header.isPlaceholder ? null : flexRender(header.column.columnDef.header, header.getContext())}
                              {header.column.getCanSort() ? (
                                <span className="datum-visualizer-sort-indicator">
                                  {header.column.getIsSorted() === 'asc' ? ' ↑' : header.column.getIsSorted() === 'desc' ? ' ↓' : ' ↕'}
                                </span>
                              ) : null}
                            </div>
                            {header.column.getCanFilter() ? (
                              <div className="datum-visualizer-column-filter">
                                <input
                                  type="text"
                                  value={String(header.column.getFilterValue() ?? '')}
                                  onChange={(event) => header.column.setFilterValue(event.target.value || undefined)}
                                  placeholder={
                                    header.column.id === 'date'
                                      ? 'Filtrar fecha'
                                      : 'Filtrar valor'
                                  }
                                />
                              </div>
                            ) : null}
                          </th>
                        ))}
                      </tr>
                    ))}
                  </thead>
                  <tbody>
                    {table.getRowModel().rows.length === 0 ? (
                      <tr>
                        <td colSpan={columns.length} className="datum-visualizer-no-results">
                          No hay resultados para el filtro aplicado.
                        </td>
                      </tr>
                    ) : (
                      table.getRowModel().rows.map((row: any) => (
                        <tr key={row.id}>
                          {row.getVisibleCells().map((cell: any) => (
                            <td key={cell.id}>{flexRender(cell.column.columnDef.cell, cell.getContext())}</td>
                          ))}
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </>
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
