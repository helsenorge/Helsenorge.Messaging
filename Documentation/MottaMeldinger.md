## Mottak av meldinger

Mottaksystemet er basert på å registrere callbacks for de tingene man er interessert i. Det finnes forskjellige sett for synkrone og asynkrone meldinger. Det finnes tre forskjellige typer callbacker. 

### ReceivedStaring
Dette er den første som kalles. På dette tidspunktet så har man bare tilgang til metadata; meldingen har ikke blitt dekrypter eller validert

Denne kan brukes til å sett correlation id for logging.

### Received
Når denne kalles, så har meldingen blitt dekryptert og validert. XML'en som ble mottat er tilgjengelig for videre prossesering. For synkrone meldinger, så må denne returnere XML som skal sendes tilbake til avsender. 

I noen tilfeller så har vi opplevd at informasjon om avsender ikke er tilgjengelig i metadata. Det gjør det umulig å hente ned riktig sertifikat for signaturvalidering. For å gi applikasjonslaget en sjans for å finne ut av hvem som sendte meldingen, så ignorerer vi signaturen og setter et flag som indikerer at sertifikatvalidering feilet. 

**Mottak bør sjekket at sertifikatvalidering er OK før den aksepterer innholdet i meldingen.**

### ReceivedCompleted
Denne kalles når meldingen er ferdig prossessert. 

### Dead letter queue
Dersom en exeption intreffer når vi prosseserer en melding, så vil meldingen havne tilbake på køen etter en timeout. Etter 10 feilede forsøk, så havner meldingen på DLQ.

```cs
var server = new MessagingServer(messagingSettings, logger, collaborationProtocolRegistry, addressRegistry);

server.RegisterAsynchronousMessageReceivedStartingCallback((message) => /* do something */ );
server.RegisterAsynchronousMessageReceivedCallback((message) => /* do something */ );
server.RegisterAsynchronousMessageReceivedCompletedCallback((message) => /* do something */ );

server.RegisterSynchronousMessageReceivedStartingCallback((message) => /* do something */ );
server.RegisterSynchronousMessageReceivedCallback((message) => /* do something */ { return new XDocument();} );
server.RegisterSynchronousMessageReceivedCompletedCallback((message) => /* do something */ );
```