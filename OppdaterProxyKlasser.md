## Oppdatering av Proxy-klasser ved oppdatert WSDL

### Autongenerer proxy-klasser for � st�tte ny funksjonalitet (Eksempel: ServiceBusManager)
N�r en tjenesteleverand�r oppdaterer sin tjeneste (WSDL) m� det genereres nye proxy-klasser f�r ny funksjonalitet kan tas i bruk. 
Dette kan gj�res via UI i Visual Studio eller ved � benytte Visual Studio CLI og benytte verkt�yet SVCUTIL. 
Her beskrives det hvordan dette kan utf�res via VS CLI eksemplifisert gjennom ServiceBusManager-APIet som tilbys av Grunndata. 

1. Last ned nyeste WSDL fra tjenesteleverand�r https://ws-web.test.nhn.no/v1/Business?singleWsdl
2. �pne Visual Studio CLI: Visual Studio > Tools > Command Line > Developer Command Prompt
3. Naviger til ServiceBusManagerServfolder: `cd "src\Helsenorge.Registries\Connected Services\ServiceBusManagerServiceV2"`
4. Kj�r scriptet:	`GenerateClientServicebusManangerServiceV2.cmd` 
