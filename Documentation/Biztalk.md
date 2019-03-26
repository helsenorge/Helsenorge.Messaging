## Biztalk

Biblioteket er signert med en Strong Name Key (SNK) og kan derfor benyttes i applikasjoner og verktøy der det kreves installasjon i Global Assembly Cache (GAC).

### Generelt om bruk
- Referer til Helsenorge.Messaging direkte i Biztalk, Helsenorge.Messaging, Helsenorge.Registries og ev. avhengigheter må da installeres i GAC.
- Abstraher bort direktereferansen til Helsenorge.Messaging ved å benytte en REST-tjeneste og la Biztalk kommuniserer med Helsenorge.Messaging via denne, unngår da GAC.