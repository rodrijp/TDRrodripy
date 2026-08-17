using System;
using System.Collections.Generic;

namespace FinPredictData.Models;

public partial class DataStadistic
{
    public short DataId { get; set; }

    public float? Cagr { get; set; }

    public float? Volatilidadcruda { get; set; }

    public float? Volatilidaddetendenciada { get; set; }

    public float? Sortino { get; set; }

    public float? Sharpe { get; set; }

    public float? Cagr20y { get; set; }

    public float? Volatilidadneg30y { get; set; }

    public float? Sortino20y { get; set; }

    public float? CorrelationGen30y { get; set; }

    public virtual Datum Data { get; set; } = null!;
}
