## Kryptering og signering
Dataene som ligger i meldingene blir først signert, så kryptert. Sertifikatinfrastrukturen baserer seg på informasjon i adresseregisteret samt private sertifikater.
Pakken støtter kryptering, dekryptering, signering, og signatur validering.

Public sertifikatene som brukes hentes fra adresseregisteret. Private sertifikater må installeres lokalt på maskinen. 

Signeringsertifikatet må ha NonRepudiation satt som usage.
Kryperingsertifikatet må ha DataEncipherment satt som usage. 

Det er mulig å gjennbruke det samme sertifikatet for både kryptering og signering, men i koden vår så antar vi at dette er to forskjellige sertifikater. Dersom man bruker det samme, så er det bare å spesifisere samme thumbprint i konfigurasjonen.