import { useState } from 'react';
import { DatumSelector } from './components/DatumSelector';
import { DataStadisticCard } from './components/DataStadisticCard';
import './App.css';

function App() {
  const [selectedDataId, setSelectedDataId] = useState<number | null>(null);

  return (
    <>
      <section id="center">
        <DatumSelector
          selectedDataId={selectedDataId}
          onDataIdChange={(dataId) => {
            setSelectedDataId(dataId);
          }}
        />
        <DataStadisticCard dataId={selectedDataId} />
      </section>
    </>
  )
}

export default App
