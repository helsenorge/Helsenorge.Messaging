## Feilsøkingsveiledning

### Mottak av duplikate meldinger

Dersom dette skjer innenfor korte intervaller vil det i de aller fleste tilfeller handle om at man ikke klarer prosessere
innkommende meldinger raskt nok. Man finner gjerne en korresponderende feilmelding i loggene av type `PRECONDITION FAILED`
når denne situasjonen oppstår.

Oppstår dette problemet hyppig anbefales det å øke verdien på `ProcessingTasks`. For `AsynchronousSettings.ProcessingTasks`
er default verdien satt til 5, dette skal i de aller fleste tilfeller være nok. Dersom problemet fortsatt består anbefales 
det å øke denne ytterligere. Hovedsaklig vil dette være et problem for Asynkrone meldinger, i de aller fleste tilfeller
vil dette ikke være et problem for Synkrone eller Error meldinger.

Meldinger som timer ut havner tilbake på egen kø og blir på nytt tilgjenglig for egen `Consumer`. Det er dette som oppleves
som at man mottar duplikater, men i praksis er dette den samme meldingen som har havnet tilbake på køen.
