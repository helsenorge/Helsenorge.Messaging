## Samhandlingsprofiler (CPP) og samhandlingsavtaler (CPA)
Bruk av kommunikasjonsparametre med samhandlingsprofiler og samhandlingsavtaler er en forutsetning for meldingsutveksling med Helsenorge.

### Samhanslingsprofil
Kommunikasjonsparametre benyttes til å definere
- Hvilke kommunikasjonsprosesser en kommunikasjonspart støtter
- Hvilke versjoner av kommunikasjonsprosesser en kommunikasjonspart støtter
- Hvilket sertifikat som er benyttet for meldingsutveksling
- Endepunkt for hvor meldinger skal sendes

En kommunikasjonsprosess er en funksjonell modellering av hvem (hvilke roller) som kommuniserer, hva de to rollene i prosessen kan sende og innholdsformat (Skjemadefinisjon) for hva som kan sendes. 

### Samhandlingsavtale
En CPA er avhengig av at begge partene har en CPP registrert. For prossesen, så vil en CPA inneholde de versjonene begge parter støtter. 

| Prosess | Part A	| Part B | Resultat |
| --------|---------| ------ | -------- |
| P1      | V1,V2	| V1 	 | V1		|
| P2      | V1,V2   | V1,V2  | V2		|
| P2      | V1,V2   | ingen	 | ingen 	|

Samhandlingsavtalen vil altså kun inneholde versjoner som begge parter støtter og der partene støtter utlike roller i prosessen. Dersom begge parter støtter flere versjoner av en prosess, vil kun den nyeste vises i CPA og skal brukes for samhandling. Helsenorge vil typisk alltid ha rollen Innbygger i alle prosesser, alle andre har den andre rollen (typisk helsepersonell).

### Hvordan brukes CPA informasjonen?
Informasjon fra CPA benyttes blant annet til:
- Validering av innkommende meldinger, inkludert om kommunikasjonsprosess er støttet, verifisering av innhold i henhold til skjema og identifisering av om det skal sendes kvitteringer
- identifisering av hvilket sertifikat som skal benyttes for den spesifikke meldingen
- Visning i brukerflaten for hvilke funksjoner som er tilgjengelig

#### Versjonering av prosesser

En prossess kan få ny versjon av to grunner.

1. Ny funksjonalitet
2. Nye innholdsformat (xmlskjema) 

Et eksempel på type 1 er utvidet bruk som ikke krever endring i skjemadefinisjon, men der det er nødvendig å vite om en kommunikaskonspart støtter endring funksjonelt.
Et eksempel på type 2 er en prosess der nye meldingsformat legges til eller får oppdaterte versjon, for eksempel oppgradering av FHIR fra R3 tl R4. Slike emdringer vil typisk også inkludere funksjonelle endringer, det er sjelden hensiktsmessig å gjøre en ren teknsik endring.

Når man mottar en melding via MessagingServer, så vil det ligge ved informasjon om CPA-profilen til avsender. Basert på versjonene og XML-informasjonen som er definert, så vil man være i stand til å bygge opp en XML som avsender kan konsumere.
   
