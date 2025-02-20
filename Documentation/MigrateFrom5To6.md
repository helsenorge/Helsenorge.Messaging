## Migrere fra 5.0 til 6.0

Hovedendringen for 6.0 er utvidelse av IMessagingClient og IMessagingServer for å kunne kontrollere synkron meldingsutveksling utenfor Helsenorge.Messaging. Dette er spesielt implementert for et konkret behov for bruk på helsenorge.no. 
For brukere som ikke er avhengig av å kunne kontrollere syncreply-køen, så kan det oppgraderes til 6.0 uten å gjøre noen konfigurasjonsendringer.

Alle endringer er dokumentert i dette migreringsdokumentet.

`IMessagingClient` er utvidet med `SendWithoutWaitingAsync` for å kunne sende en melding til en sync-kø. Hvis denne brukes tar man selv ansvar for å hente ned svaret fra 
køen spesifisert i `MessagingSettings:AmqpSettings:Synchronous:StaticReplyQueue`
Hvis man ønsker å bruke den allerede eksisterende `SendAndWaitAsync` metoden, vil denne fortsatt fungere som før og trenger ikke populere opp den nye verdien.

### Konfigurasjonsrelaterte endringer

`MessagingSettings:AmqpSettings:Synchronous:StaticReplyQueue` er lagt til for å kunne spesifisere en svar-kø for synkron meldingsutveksling. Denne er ikke påkrevd

### Nye features

`IMessagingClient` er utvidet med `SendWithoutWaitingAsync`
IMessagingServer er utvidet med muligheten for å lytte på syncreply-køen spesifisert i `MessagingSettings:AmqpSettings:Synchronous:StaticReplyQueue`
