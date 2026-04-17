# Migrere fra 6.0 til 7.0
Velkommen til versjon 7.0 av Helsenorge.Messaging med støtte for DPOP autentisering for kall mot CPPA. Støtte for .NET standard 2.0 er fjernet og kravet til minimum .NET Core 8 er satt.
Denne versjonen av Helsenorge.Messaging tar i bruk [HelseId v1.1.3](https://www.nuget.org/packages/HelseID.Library.ClientCredentials/) sin bibliotek for å håndtere DPOP autentisering. 
Det er nødvendig å gjøre seg kjent med biblioteket for å forstå endringene som er gjort og det individuelle oppsettet som må gjøres for å bruke v7.0. 

## Konfigurasjonsrelaterte endringer
### Hva er nytt
- Nytt felt i RestConfiguration
	- IsDpopEnabled: styrer om endepunktet (Restconfiguration.Address) krever Dpop autentisering
- Helsenorge.Registries.Configuration.HelseIdConfiguration erstattes med HelseId.Library.Configuration.HelseIdConfiguration
- Test prosjekter bruker MSTest
### Hva er fjernet
- Alle referanser til xUnit pakker i test prosjekter



> Når du er klar til å bruke CPPA sin Dpop-enabled endepunktet, sørg for at:
1) RestConfiguration.Address har / på slutten (https://cppa.grunndata.nhn.no/v2/) 
2) Deres HelseID klient har tilgang til scopet ```nhn:cppa/access-with-dpop``` og legges inn i konfigureringen HelseId.Library.Configuration.HelseIdConfiguration.Scope
