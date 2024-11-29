## Migrere fra SOAP til REST i 5.2 og senere versjoner

Hovedendringer for 5.2 er nytt endepunkt for CPPA tjenesten. Det gamle endepunktet vil fortsatt fungere, men
støtten for SOAP-endepunktet vil fjernes i en fremtidig versjon. Derfor anbefaler vi at alle oppgraderer
til det nye REST-endepunktet så tidlig som mulig.

For å autentisere mot det nye REST endepunktet så krever det at man har HelseId klient satt opp med scopet 
"nhn:cppa/access".

### Konfigurasjonsrelaterte endringer
Nye konfigurasjoner er CollaborationProtocolRegistryRestSettings og HelseIdConfiguration. 
CollaborationProtocolRegistryRestSettings inneholder RestConfiguration,CachingInterval, UseOnlineRevocationCheck 
og ThrowMessageIfNoCpp. Det brukes i stedet for CollaborationProtocolRegistrySettings når man skal ta i bruk
nye REST endepunktet.

I RestConfiguration setter man URL adressen til CPPA (https://cppa.test.grunndata.nhn.no og 
https://cppa.grunndata.nhn.no). Man kan også sette opp proxy instillinger ved hjelp av UseDefaultWebProxy, 
BypassProxyOnLocal og ProxyAddress. 

For å autentisere seg mot det nye endepunktet må man sette opp HelseIdConfiguration. HelseIdConfiguration inneholder
ClientId, TokenEndpoint (https://helseid-sts.test.nhn.no/connect/token og https://helseid-sts.nhn.no/connect/token) og 
ScopeName. HelseIdConfiguration blir brukt i HelseIdClient.

For oppdatert liste over URLer kan du se her: 
https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/690913297/Meldingsutveksling+med+Helsenorge

### Ny klasse CollaborationProtocolRegistryRest som implementerer ICollaborationProtocolRegistry
Det er lagt inn en ny klasse CollaborationProtocolRegistryRest for å kommunisere med det nye REST endepunktet, som 
implementerer ICollaborationProtocolRegistry. Når config er satt opp så kan du ganske enkelt endre fra 
CollaborationProtocolRegistry til CollaborationProtocolRegistryRest.

Endring fra CollaborationProtocolRegistry til CollaborationProtocolRegistryRest så tar konstuktøren inn konfigurasjonen
CollaborationProtocolRegistryRestSettings og IHelseIdClient. IHelseIdClient settes opp til å bruke HelseIdClient,
beskrevet nærmere nedenfor. 

### Sette opp autentisering for nye endepunktet via HelseId
For å autentisere via HelseId for the nye endepunktet må man sette opp ISecurityProvider for bruk i HelseIdClient.
Det er lagt et eksempel på hvordan man kan sette opp denne ved bruk av PEM key i SecurityKeyProvider.cs under 
Helsenorge.Messaging.Client. Om man har en PEM Key så base64 encoder man PEM keyen før man leser den ut. 
