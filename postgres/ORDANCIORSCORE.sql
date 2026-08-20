SELECT B."DataName", A."RScore" FROM public."DataStadistics" A
INNER JOIN public."Data" B ON A."DataId" = B."DataId"
WHERE A."RScore" is not null
ORDER BY A."RScore" DESC 