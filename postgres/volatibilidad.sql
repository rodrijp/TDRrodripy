SELECT d."DataId", d."DataName", ds."VOLATILIDADCruda", ds."VOLATILIDADDetendenciada" FROM public."Data" d
INNER JOIN public."DataStadistics" ds ON d."DataId" =ds."DataId"
ORDER BY d."DataId" ASC 