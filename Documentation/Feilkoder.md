# Helsenorge Messaging Feilkoder

## Helsenorge.Registries

### REG-1
Problemer med å hente kommunikasjons detaljer.
 
### REG-2
Problemer med å finne CPP for en motpart

### REG-3
Problemer med å finne CPA for en motpart.

## Helsenorge.Messaging

### MUG-1
Generell feil med å motta meldinger.

### MUG-2
Generell feil med å sende meldinger.

### MUG-3
Kønavnet er tomt.

### MUG-4
Avsender mangler i adresseregisteret.

### MUG-10
Mer enn en feil med avsenders sertifikat.

### MUG-11
Avsenders sertifikat har ugyldig start dato.

### MUG-12
Avsenders sertifikat har ugyldig slutt dato.

### MUG-13
Avsenders sertifikat har blitt revokert.

### MUG-14
Avsenders sertifikat har ugyldig type. f.eks. signeringssertifikat som brukes for kryptering.

### MUG-15
Mer enn en feil med lokalt sertifikat.

### MUG-16
Lokalt sertifikat har ugyldig start dato.

### MUG-17
Lokalt sertifikat har ugyldig slutt dato.

### MUG-18
Lokalt sertifikat har blitt revokert.

### MUG-19
Lokalt sertifikat har ugyldig type. f.eks. signeringssertifikat som brukes for kryptering.

### MUG-20
Mottatt melding er ikke XML.

### MUG-21
Mottatt melding mangler data i AMQP header.

### MUG-22
Mottatt melding har feil data i header kontra det som ligger i meldingen. Denne brukes av hodemelding for å sjekke at avsender id i fagmeldingen stemmer med det som står i AMQP header. 

### MUG-23
Meldingsmottaket har rapportert en feil som skal sendes til avsender. 

### MUG-30
Avsender svarte ikke på en synkron melding innen en gitt tid (timeout).

### MUG-31
Avsender svarte på en synkron melding etter at tiden gikk ut. Meldingen har ikke blitt prosessert.

### MUG-33
Ugyldig meldingsfunksjon.

### MUG-34
Feil som avsender har rapportert. Ting som kommer inn på error køen.

### MUG-35
Ukjent feil har oppstått.
