# Helsenorge Messaging Feilkoder

## Helsenorge.Registries

### REG-000001
Problemer med å hente kommunikasjons detaljer.
 
### REG-000002
Problemer med å finne CPP for en motpart

### REG-000003
Problemer med å finne CPA for en motpart.

## Helsenorge.Messaging

### MUG-000001
Generell feil med å motta meldinger.

### MUG-000002
Generell feil med å sende meldinger.

### MUG-000003
Kønavnet er tomt.

### MUG-000004
Avsender mangler i adresseregisteret.

### MUG-000010
Mer enn en feil med avsenders sertifikat.

### MUG-000011
Avsenders sertifikat har ugyldig start dato.

### MUG-000012
Avsenders sertifikat har ugyldig slutt dato.

### MUG-000013
Avsenders sertifikat har blitt revokert.

### MUG-000014
Avsenders sertifikat har ugyldig type. f.eks. signeringssertifikat som brukes for kryptering.

### MUG-000015
Mer enn en feil med lokalt sertifikat.

### MUG-000016
Lokalt sertifikat har ugyldig start dato.

### MUG-000017
Lokalt sertifikat har ugyldig slutt dato.

### MUG-000018
Lokalt sertifikat har blitt revokert.

### MUG-000019
Lokalt sertifikat har ugyldig type. f.eks. signeringssertifikat som brukes for kryptering.

### MUG-000020
Mottatt melding er ikke XML.

### MUG-000021
Mottatt melding mangler data i AMQP header.

### MUG-000022
Mottatt melding har feil data i header kontra det som ligger i meldingen. Denne brukes av hodemelding for å sjekke at avsender id i fagmeldingen stemmer med det som står i AMQP header. 

### MUG-000023
Meldingsmottaket har rapportert en feil som skal sendes til avsender. 

### MUG-000030
Avsender svarte ikke på en synkron melding innen en gitt tid (timeout).

### MUG-000031
Avsender svarte på en synkron melding etter at tiden gikk ut. Meldingen har ikke blitt prosessert.

### MUG-000033
Ugyldig meldingsfunksjon.

### MUG-000034
Feil som avsender har rapportert. Ting som kommer inn på error køen.

### MUG-000035
Ukjent feil har oppstått.

### MUG-000036
The Messaging Entity Cache failed to close an entity.

### MUG-000037
Non-successful release of message.

### MUG-001001
Informasjonsformål når mottaksprosessen starter/avslutter

### MUG-001002
Informasjonsformål når sendeprosessen starter / avslutter 

### MUG-001003
Informasjonsformål når meldingen fjernes fra køen

### MUG-001004
Informasjonsformål ved opprettelse, endring og fjerning av cache entiter i Message Entity Cache

### MUG-001005
Informasjonsformål ved notifikasjonskall til ekstern kode (brukere av biblioteket / tredjeparter), gir muligheter til å måle responstiden til tredjepartene.

### MUG-001006
Informasjonsformål ved validering av sertifiakter

### MUG-001007
Informasjonsformål ved kryptering av payload

### MUG-001008
Informasjonsformål ved interaksjon mot Factory Pool cache og opprettelse av tom melding, skjer rett før sending av melding til ServiceBus
