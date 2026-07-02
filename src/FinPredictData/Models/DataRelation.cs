using System;
using System.Collections.Generic;

namespace FinPredictData.Models;

public partial class DataRelation
{
    public short DataIdSource { get; set; }

    public short DataIdTarget { get; set; }

    public short DataRelationId { get; set; }

    public float? Correlation { get; set; }

    public float? Covariance { get; set; }

    public float? CorrelationLog { get; set; }
}
