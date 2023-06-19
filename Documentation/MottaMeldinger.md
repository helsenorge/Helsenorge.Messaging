## Mottak av meldinger

For å kunne prosessere meldinger ved mottak registrerer man callbacks.

Det eksisterer 3 kategorier av callbacks for synkrone og asynkrone meldinger.

#### ReceivedStaring
Dette er den første som kalles. På dette tidspunktet så har man bare tilgang til metadata; meldingen har ikke blitt
dekryptert eller validert.

Her kan man f.eks. sette CorrelationId som brukes i forbindelse med logging på egen side.


#### Received
Når denne kalles, så har meldingen blitt dekryptert og validert. Meldingen som ble mottat er nå tilgjengelig for videre
prossesering av eget forretningslag.

For synkrone meldinger, så skal denne returnere et XDocument som biblioteket da vil kryptere, signere, og sende tilbake
til avsender.


#### ReceivedCompleted
Denne kalles når meldingen er ferdig prosessert uten feil. Her kan man gjøre ev. tasks som er nødvendig for egen
forretningslogikk ved ferdigprosessert melding.


#### Eksempel på registrering av callbacks

```cs
var server = new MessagingServer(messagingSettings, logger, collaborationProtocolRegistry, addressRegistry);

// Callbacks for asynkrone meldinger
server.RegisterAsynchronousMessageReceivedStartingCallback((message) => /* do something */ );
server.RegisterAsynchronousMessageReceivedCallback((message) => /* do something */ );
server.RegisterAsynchronousMessageReceivedCompletedCallback((message) => /* do something */ );

// Callbacks for synkrone meldinger.
server.RegisterSynchronousMessageReceivedStartingCallback((message) => /* do something */ );
server.RegisterSynchronousMessageReceivedCallback((message) => /* do something */ { return new XDocument();} );
server.RegisterSynchronousMessageReceivedCompletedCallback((message) => /* do something */ );
```

#### Dead letter queue
Dersom en exeption intreffer når vi proseserer en melding, så vil meldingen havne tilbake på køen etter en timeout. Etter
10 feilede forsøk, så havner meldingen på DLQ.
