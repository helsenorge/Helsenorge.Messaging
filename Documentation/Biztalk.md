## Biztalk

Biblioteket er signert med en Strong Name Key (SNK) og kan derfor benyttes i applikasjoner og verktøy der det kreves installasjon i Global Assembly Cache (GAC).

### Generelt om bruk
Det er to måter å benytte Helsenorge.Messaging fra Biztalk, enten ved å:
1. Refere til Helsenorge.Messaging i Biztalk, Helsenorge.Messaging, Helsenorge.Registries og ev. avhengigheter må da installeres i GAC.

eller ved å:

2. Abstraher borte direktereferansen til Helsenorge.Messaging ved å benytte en REST-tjeneste og la Biztalk kommuniserer med Helsenorge.Messaging via REST-tjenesten, unngår da GAC.
