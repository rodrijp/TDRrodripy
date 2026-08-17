using System;
using System.Collections.Generic;

namespace FinPredictData.Models;

public partial class Source
{
    public short SourceId { get; set; }

    public string SourceName { get; set; } = null!;

    public virtual ICollection<Datum> Data { get; set; } = new List<Datum>();
}
