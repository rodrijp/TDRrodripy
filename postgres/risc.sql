SELECT d."DataId", d."DataName", ds."Sortino", ds."Sharpe" FROM public."Data" d
INNER JOIN public."DataStadistics" ds ON d."DataId" =ds."DataId"
ORDER BY d."DataId" ASC 