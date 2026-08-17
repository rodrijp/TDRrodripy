using System;
using System.Collections.Generic;

namespace FinPredictData.Models;

public partial class HistoricalDatum
{
    public long HistoricalDataId { get; set; }

    public DateOnly Date { get; set; }

    public short DataId { get; set; }

    public float Value { get; set; }

    public virtual Datum Data { get; set; } = null!;
}
