using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinPredictCore.Service.Data;
using FinPredictCore.Service.DataRelation;
using FinPredictCore.Service.DataStadistic;
using FinPredictData.Models;

namespace FinPredictCore.Jobs
{
    public class RAsignationCalculate : IRAsignationCalculate
    {
        private readonly IDataService _dataService;
        private readonly IDataStadisticService _dataStadisticService;
        private readonly IDataRelationService _dataRelationService;

        private List<AssetState> _assets = new();

        public RAsignationCalculate(
            IDataService dataService,
            IDataStadisticService dataStadisticService,
            IDataRelationService dataRelationService)
        {
            _dataService = dataService;
            _dataStadisticService = dataStadisticService;
            _dataRelationService = dataRelationService;
        }

        private sealed class AssetState
        {
            public short DataId { get; set; }
            public string Name { get; set; } = string.Empty;
            public float RScore { get; set; }
            public double RAsignation { get; set; }
            public bool IsTop { get; set; }
            public DataStadistic Stadistic { get; set; } = null!;
        }

        private List<AssetState> Top => _assets.Where(a => a.IsTop).OrderByDescending(a => a.RScore).ToList();

        public async Task Do()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine(">>> INICIANDO RAsignationCalculate <<<");
            Console.WriteLine(new string('=', 60));

            await LoadAssets();

            if (_assets.Count == 0)
            {
                Console.WriteLine("❌ No hay activos con RScore calculado. Ejecute antes RScoreEngineCalculate.");
                return;
            }

            InicialDistibution();
            FirstFilter();

            var top4Before = Top.Count >= 4 ? Top[3].RAsignation : (double?)null;
            var top1Before = Top.Count >= 4 ? Top[0].RAsignation : (double?)null;
            SecondFilter();

            if (top4Before.HasValue && top1Before.HasValue)
            {
                Console.WriteLine($"  >> CHECK SecondFilter: top1 antes={top1Before.Value:F4} después={Top[0].RAsignation:F4} | top4 antes={top4Before.Value:F4} después={Top[3].RAsignation:F4}");
            }

            await ThirdFilter();
            FourthFilter();
            FifthFilter();
            await AntiNeg();

            Console.WriteLine(new string('=', 60));
            Console.WriteLine("✓ RAsignationCalculate finalizado");
            Console.WriteLine(new string('=', 60) + "\n");
        }

        private async Task LoadAssets()
        {
            var excludeIds = new HashSet<int>
            {
                DataUtil.INFLATION, DataUtil.UNEMPLOYMENT, DataUtil.DEBT_GDP, DataUtil.M2,
                DataUtil.DOW_JONES, DataUtil.TREASURY_30Y, DataUtil.TREASURY_10Y
            };

            var datums = _dataService
                .GetAllDatums()
                .Where(d => !excludeIds.Contains(d.DataId))
                .ToList();

            var assets = new List<AssetState>();

            foreach (var datum in datums)
            {
                var stadistic = await _dataStadisticService.GetByDataId(datum.DataId);

                if (stadistic?.Rscore == null)
                {
                    continue;
                }

                assets.Add(new AssetState
                {
                    DataId = datum.DataId,
                    Name = datum.DataName,
                    RScore = stadistic.Rscore.Value,
                    RAsignation = 0,
                    IsTop = false,
                    Stadistic = stadistic
                });
            }

            _assets = assets;
            Console.WriteLine($"[1] Activos con RScore cargados: {_assets.Count}");
        }

        public void InicialDistibution()
        {
            Console.WriteLine("\n--- InicialDistibution ---");

            var ranked = _assets
                .Where(a => a.DataId != DataUtil.BITCOIN)
                .OrderByDescending(a => a.RScore)
                .ToList();
            float[] assignments = { 40f, 30f, 20f, 10f };

            for (var i = 0; i < ranked.Count; i++)
            {
                ranked[i].IsTop = i < assignments.Length;
                ranked[i].RAsignation = i < assignments.Length ? assignments[i] : 0;
            }

            for (var i = 0; i < ranked.Count; i++)
            {
                var tag = ranked[i].IsTop ? $"top {i + 1}" : "    ";
                Console.WriteLine($"  {tag} {ranked[i].Name,-20} | RScore={ranked[i].RScore:F4} | RAsignation={ranked[i].RAsignation}");
            }
        }

        public void FirstFilter()
        {
            Console.WriteLine("\n--- FirstFilter (CAGR10Y - CAGR20Y >= 0,04) ---");
            var top = Top;

            if (top.Count == 0)
            {
                return;
            }

            var complying = new List<AssetState>();
            var notComplying = new List<AssetState>();

            foreach (var asset in top)
            {
                var cagr10 = asset.Stadistic.Cagr10y;
                var cagr20 = asset.Stadistic.Cagr20y;

                if (cagr10.HasValue && cagr20.HasValue && cagr10.Value - cagr20.Value >= 0.04f)
                {
                    complying.Add(asset);
                    Console.WriteLine($"  {asset.Name,-20} | CAGR10Y-CAGR20Y={cagr10.Value - cagr20.Value:F4} → CUMPLE (-5)");
                }
                else
                {
                    notComplying.Add(asset);
                    var diff = cagr10.HasValue && cagr20.HasValue ? cagr10.Value - cagr20.Value : float.NaN;
                    Console.WriteLine($"  {asset.Name,-20} | CAGR10Y-CAGR20Y={diff:F4} → no cumple (+reparto)");
                }
            }

            foreach (var asset in complying)
            {
                asset.RAsignation -= 5;
            }

            if (complying.Count > 0 && notComplying.Count > 0)
            {
                var share = 5.0 * complying.Count / notComplying.Count;

                foreach (var asset in notComplying)
                {
                    asset.RAsignation += share;
                }
            }

            PrintTop();
        }

        public void SecondFilter()
        {
            Console.WriteLine("\n--- SecondFilter (VOLATILIDADNeg 30Y del top 1 vs media del top) ---");
            var top = Top;

            if (top.Count < 4)
            {
                return;
            }

            var vols = top
                .Where(a => a.Stadistic.Volatilidadneg30y.HasValue)
                .Select(a => (double)a.Stadistic.Volatilidadneg30y!.Value)
                .ToList();

            if (vols.Count == 0 || vols.Count != top.Count)
            {
                Console.WriteLine("  ⚠ Faltan VOLATILIDADNeg 30Y, se omite el filtro");
                return;
            }

            var meanVol = vols.Average();
            var volTop1 = (double)top[0].Stadistic.Volatilidadneg30y!.Value;

            if (meanVol == 0)
            {
                Console.WriteLine("  ⚠ Media de VOLATILIDADNeg 30Y igual a 0, se omite el filtro");
                return;
            }

            var result = (volTop1 / meanVol - 1.0) * 100.0;
            Console.WriteLine($"  Media VolNeg30Y top={meanVol:F6} | VolNeg30Y top1={volTop1:F6} | Resultado={result:F4}");

            if (result >= 50)
            {
                top[0].RAsignation -= 10;
                top[3].RAsignation += 10;
                Console.WriteLine($"  Resultado >= 50 → top1 ({top[0].Name}) -10 y top4 ({top[3].Name}) +10");
            }
            else
            {
                Console.WriteLine("  Resultado < 50 → sin cambios");
            }

            PrintTop();
        }

        public async Task ThirdFilter()
        {
            Console.WriteLine("\n--- ThirdFilter (CorrelationSum) ---");
            var top = Top;

            if (top.Count < 2)
            {
                return;
            }

            var correlationSums = new Dictionary<AssetState, double>();

            foreach (var asset in top)
            {
                double sum = 0;

                foreach (var other in top.Where(o => o.DataId != asset.DataId))
                {
                    var correlation = await GetCorrelation(asset.DataId, other.DataId);

                    if (!double.IsNaN(correlation))
                    {
                        sum += correlation;
                    }
                }

                correlationSums[asset] = sum;
                Console.WriteLine($"  {asset.Name,-20} | CorrelationSum={sum:F6}");
            }

            var maxEntry = correlationSums.OrderByDescending(kvp => kvp.Value).First();
            var minEntry = correlationSums.OrderBy(kvp => kvp.Value).First();

            maxEntry.Key.RAsignation -= 10;
            minEntry.Key.RAsignation += 10;

            Console.WriteLine($"  Mayor CorrelationSum: {maxEntry.Key.Name} → -10 | Menor CorrelationSum: {minEntry.Key.Name} → +10");

            PrintTop();
        }

        private async Task<double> GetCorrelation(short sourceId, short targetId)
        {
            var relation = await _dataRelationService.GetByDataIdSourceAndTarget(sourceId, targetId)
                ?? await _dataRelationService.GetByDataIdSourceAndTarget(targetId, sourceId);

            return relation?.Correlation ?? double.NaN;
        }

        public void FourthFilter()
        {
            Console.WriteLine("\n--- FourthFilter (VolDetendenciada / VolCruda entre 0,4 y 0,9) ---");
            var top = Top;

            if (top.Count == 0)
            {
                return;
            }

            var complying = new List<AssetState>();
            var notComplying = new List<AssetState>();

            foreach (var asset in top)
            {
                var detrended = asset.Stadistic.Volatilidaddetendenciada;
                var crude = asset.Stadistic.Volatilidadcruda;

                if (detrended.HasValue && crude.HasValue && crude.Value != 0)
                {
                    var ratio = (double)detrended.Value / (double)crude.Value;
                    Console.WriteLine($"  {asset.Name,-20} | Ratio={ratio:F4}");

                    if (ratio >= 0.4 && ratio <= 0.9)
                    {
                        complying.Add(asset);
                    }
                    else
                    {
                        notComplying.Add(asset);
                    }
                }
                else
                {
                    Console.WriteLine($"  {asset.Name,-20} | Faltan VOLATILIDADCruda/VOLATILIDADDetendenciada → no cumple");
                    notComplying.Add(asset);
                }
            }

            if (notComplying.Count > 0)
            {
                var penalty = 10.0 / notComplying.Count;

                foreach (var asset in notComplying)
                {
                    asset.RAsignation -= penalty;
                }
            }

            if (complying.Count > 0)
            {
                var reward = 10.0 / complying.Count;

                foreach (var asset in complying)
                {
                    asset.RAsignation += reward;
                }
            }

            PrintTop();
        }

        public void FifthFilter()
        {
            Console.WriteLine("\n--- FifthFilter (límite 45) ---");

            var capped = _assets.Where(a => a.RAsignation > 45).ToList();

            if (capped.Count == 0)
            {
                Console.WriteLine("  Ningún activo supera 45 → sin cambios");
                return;
            }

            double totalExcess = 0;

            foreach (var asset in capped)
            {
                var excess = asset.RAsignation - 45;
                asset.RAsignation = 45;
                totalExcess += excess;
                Console.WriteLine($"  {asset.Name,-20} | Excedente={excess:F4} → limitado a 45");
            }

            var others = _assets.Except(capped).ToList();

            if (others.Count > 0)
            {
                var share = totalExcess / others.Count;

                foreach (var asset in others)
                {
                    asset.RAsignation += share;
                }
            }

            PrintTop();
        }

        public async Task AntiNeg()
        {
            Console.WriteLine("\n--- AntiNeg (mínimo 0 + guardado en DB) ---");

            var floored = _assets.Where(a => a.RAsignation < 0).ToList();
            double totalNegative = 0;

            foreach (var asset in floored)
            {
                totalNegative += asset.RAsignation;
                Console.WriteLine($"  {asset.Name,-20} | Excedente negativo={asset.RAsignation:F4} → establecido a 0");
                asset.RAsignation = 0;
            }

            if (floored.Count > 0)
            {
                var others = Top.Except(floored).ToList();
                var third = totalNegative / 3;

                foreach (var asset in others)
                {
                    asset.RAsignation += third;
                    Console.WriteLine($"  {asset.Name,-20} | {third:F4} (tercio del excedente)");
                }
            }

            Console.WriteLine("\n--- Guardando RAsignation en DataStadistics ---");

            foreach (var asset in _assets)
            {
                var value = asset.IsTop ? (float)asset.RAsignation : 0f;

                await _dataStadisticService.CreateOrUpdate(new DataStadistic
                {
                    DataId = asset.DataId,
                    Rasignation = value
                });

                var tag = asset.IsTop ? "top" : "   ";
                Console.WriteLine($"  [{tag}] {asset.Name,-20} | RAsignation={value}");
            }
        }

        private void PrintTop()
        {
            foreach (var asset in Top)
            {
                Console.WriteLine($"  · {asset.Name,-20} | RAsignation={asset.RAsignation:F4}");
            }
        }
    }
}
