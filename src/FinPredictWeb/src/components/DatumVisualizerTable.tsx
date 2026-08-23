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
import { useMemo, useState } from 'react';
import type { HistoricalDatum } from '../hooks/useHistoricalData';

type DatumRow = {
  historicalDataId?: number | string;
  date: string;
  value?: number;
};

interface DatumVisualizerTableProps {
  data: HistoricalDatum[];
  isValue?: boolean;
  loading?: boolean;
  error?: string | null;
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

const formatHistoricalValue = (value?: number, isValue?: boolean) => {
  if (value == null) {
    return '—';
  }

  if (isValue === true) {
    return value.toFixed(4);
  }

  return `${value.toFixed(2)} %`;
};

export const DatumVisualizerTable: React.FC<DatumVisualizerTableProps> = ({
  data,
  isValue,
  loading = false,
  error = null,
}) => {
  const [columnFilters, setColumnFilters] = useState<any[]>([]);
  const [sorting, setSorting] = useState<any[]>([]);

  const tableData = useMemo<DatumRow[]>(
    () =>
      data.map((item) => ({
        historicalDataId: item.historicalDataId ?? `${item.date ?? 'fecha'}-${item.value ?? 'valor'}`,
        date: item.date ?? '',
        value: item.value,
      })),
    [data],
  );

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
        cell: ({ row }: { row: { original: DatumRow } }) => formatHistoricalValue(row.original.value, isValue),
        sortFn: 'alphanumeric' as const,
        filterFn: (row: any, columnId: string, filterValue: string) => {
          const rawValue = row.getValue(columnId);
          const formattedValue = formatHistoricalValue(typeof rawValue === 'number' ? rawValue : Number(rawValue), isValue);
          const search = String(filterValue ?? '').trim().toLowerCase();

          if (!search) {
            return true;
          }

          return String(rawValue ?? '').toLowerCase().includes(search) || formattedValue.toLowerCase().includes(search);
        },
      },
    ],
    [isValue],
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

  if (loading) {
    return <p className="datum-visualizer-status">Cargando historial...</p>;
  }

  if (error) {
    return <p className="datum-visualizer-status datum-visualizer-error">{error}</p>;
  }

  if (data.length === 0) {
    return <p className="datum-visualizer-status">No hay datos históricos para este activo.</p>;
  }

  return (
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
                      placeholder={header.column.id === 'date' ? 'Filtrar fecha' : 'Filtrar valor'}
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
  );
};
