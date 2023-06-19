## Feils�kingsveiledning

### Mottak av duplikate meldinger

Dersom dette skjer innenfor korte intervaller vil det i de aller fleste tilfeller handle om at man ikke klarer prosessere
innkommende meldinger raskt nok. Man finner gjerne en korresponderende feilmelding i loggene av type `PRECONDITION FAILED`
n�r denne situasjonen oppst�r.

Oppst�r dette problemet hyppig anbefales det � �ke verdien p� `ProcessingTasks`. For `AsynchronousSettings.ProcessingTasks`
er default verdien satt til 5, dette skal i de aller fleste tilfeller v�re nok. Dersom problemet fortsatt best�r anbefales 
det � �ke denne ytterligere. Hovedsaklig vil dette v�re et problem for Asynkrone meldinger, i de aller fleste tilfeller
vil dette ikke v�re et problem for Synkrone eller Error meldinger.

Meldinger som timer ut havner tilbake p� egen k� og blir p� nytt tilgjenglig for egen `Consumer`. Det er dette som oppleves
som at man mottar duplikater, men i praksis er dette den samme meldingen som har havnet tilbake p� k�en.
