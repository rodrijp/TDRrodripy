using System;
using System.Collections.Generic;

namespace FinPredictData.Models;

public partial class Datum
{
    public short DataId { get; set; }

    public string DataName { get; set; } = null!;

    public short SourceId { get; set; }

    public string? SourceAccess { get; set; }

    public bool IsValue { get; set; }

    public virtual DataStadistic? DataStadistic { get; set; }

    public virtual ICollection<HistoricalDatum> HistoricalData { get; set; } = new List<HistoricalDatum>();

    public virtual Source Source { get; set; } = null!;
}
