## Oppdatering av Proxy-klasser ved oppdatert WSDL

### Autongenerer proxy-klasser for å støtte ny funksjonalitet (Eksempel: ServiceBusManager)
Når en tjenesteleverandør oppdaterer sin tjeneste (WSDL) må det genereres nye proxy-klasser før ny funksjonalitet kan tas i bruk. 
Dette kan gjøres via UI i Visual Studio eller ved å benytte Visual Studio CLI og benytte verktøyet SVCUTIL. 
Her beskrives det hvordan dette kan utføres via VS CLI eksemplifisert gjennom ServiceBusManager-APIet som tilbys av Grunndata. 

1. Last ned nyeste WSDL fra tjenesteleverandør https://ws-web.test.nhn.no/v1/Business?singleWsdl
2. Åpne Visual Studio CLI: Visual Studio > Tools > Command Line > Developer Command Prompt
3. Naviger til ServiceBusManagerServfolder: `cd "src\Helsenorge.Registries\Connected Services\ServiceBusManagerServiceV2"`
4. Kjør scriptet:	`GenerateClientServicebusManangerServiceV2.cmd` 
