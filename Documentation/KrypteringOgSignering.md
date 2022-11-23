## Kryptering og signering
Dataene som ligger i meldingene blir først signert, så kryptert. Sertifikatinfrastrukturen baserer seg på informasjon i adresseregisteret samt private sertifikater.
Pakken støtter kryptering, dekryptering, signering, og signaturvalidering.

Public-sertifikatene som brukes, hentes fra adresseregisteret. Private sertifikater må installeres lokalt på maskinen. 

- Signeringsertifikatet må ha NonRepudiation satt som usage.
- Kryperingsertifikatet må ha KeyEncipherment satt som usage. 

Det er mulig å gjenbruke det samme sertifikatet for både kryptering og signering, men i koden vår antar vi at dette er to forskjellige sertifikater. Dersom man bruker det samme, så er det bare å spesifisere samme thumbprint i konfigurasjonen.

## Bytting av sertifikater
Fra tid til annen så vil sertifikater gå ut på dato og må byttes. Det beste er å ha overlappende støtte for flere sertifikater slik at man aldri kommer i en situasjon der man ikke kan stole på dataene som blir sendt. 

Hvordan skal dette håndteres? Det er stor sannsynlighet for at det ligger meldinger på køen som er beskyttet med gamle sertifikater under en bytting.

### Bytting av lokalt dekrypteringssertifikat
Det primære sertifikatet som benyttes under dekryptering er spesifisiert i konfigurasjon via `DecryptionCertificate`. Det finnes en tilsvarende verdi som heter `LegacyDecryptionCertificate`. 
Når man skal bytte, så kan man i en periode ha verdi på begge to. Begge vil da benyttes til dekryptering. 

Dersom man glemmer å fjerne verdien fra LegacyDecryptionCertificate, så vil loggene dine begynne å fylle seg opp med feilmeldinger når det sertifikatet ikke lenger er gyldig.

Man må selvsagt også oppdatere informasjonen som ligger i adresseregisteret. 

### Håndtering av sertifikatbytte hos motpart
Når en motpart bytter sertifikat, så kan det by på problemer. Hva med meldinger som allerede har blitt sendt, men ikke blitt behandlet? Hva med sertifikater som har blitt cachet?

Den eneste måten å håndtere dette på er å sende med CPA-IDen som var i bruk når meldingen ble sendt. Basert på denne verdien, så kan man få tilgang til sertifikatene som var gyldige på det tidspunktet meldingen ble sendt.   
