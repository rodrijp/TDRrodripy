import { useState } from 'react';
import { DatumSelector } from './components/DatumSelector';
import { DataRelationCard } from './components/DataRelationCard';
import './App.css';

function App() {
  const [leftDataId, setLeftDataId] = useState<number | null>(null);
  const [rightDataId, setRightDataId] = useState<number | null>(null);

  return (
    <main className="app-shell container-fluid py-4">
      <div className="row g-4 align-items-stretch">
        <aside className="col-12 col-lg-3">
          <div className="asset-panel h-100">
            <div className="panel-header">
              <span className="panel-badge">Activo</span>
              <h2>Izquierda</h2>
            </div>

            <DatumSelector
              selectedDataId={leftDataId}
              onDataIdChange={(dataId) => setLeftDataId(dataId)}
              label="Selecciona un activo:"
            />

            <div className="mt-3">
              {/* placeholder for left statistics */}
            </div>
          </div>
        </aside>

        <section className="col-12 col-lg-6">
          <div className="correlation-panel h-100">
            <div className="panel-header">
              <span className="panel-badge">Correlación</span>
              <h2>Relación entre activos</h2>
            </div>

            <DataRelationCard leftDataId={leftDataId} rightDataId={rightDataId} />
          </div>
        </section>

        <aside className="col-12 col-lg-3">
          <div className="asset-panel h-100">
            <div className="panel-header">
              <span className="panel-badge">Activo</span>
              <h2>Derecha</h2>
            </div>

            <DatumSelector
              selectedDataId={rightDataId}
              onDataIdChange={(dataId) => setRightDataId(dataId)}
              label="Selecciona un activo:"
            />

            <div className="mt-3">
              {/* placeholder for right statistics */}
            </div>
          </div>
        </aside>
      </div>
    </main>
  );
}

export default App;
