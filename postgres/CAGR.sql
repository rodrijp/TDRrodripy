SELECT d."DataId", d."DataName", ds."CAGR" FROM public."Data" d
INNER JOIN public."DataStadistics" ds ON d."DataId" =ds."DataId"
ORDER BY d."DataId" ASC 