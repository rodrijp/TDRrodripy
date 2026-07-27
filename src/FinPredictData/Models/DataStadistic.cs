using System;
using System.Collections.Generic;

namespace FinPredictData.Models;

public partial class DataStadistic
{
    public short DataId { get; set; }

    public float? Cagr { get; set; }

    public virtual Datum Data { get; set; } = null!;
}
