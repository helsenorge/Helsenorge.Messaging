## Feilhåndtering og logging

I utgangspunktet så prøver vi å gi relevante feilmeldinger som et menneske klarer å forstå; disse kan endre seg over tid. 
For feilmeldinger så legger vi ved en ID som ikke endrer seg; denne kan da benyttes av maskiner for å identifisere en gitt feilmelding. 

Drift kan da benytte disse feilkodene til å bestemme hva slag varsling som man anser som nødvendig. 

[Følgende feilkoder kan rapporteres](Feilkoder.md)


Følgende ting implementert for feilhåndtering.
- Alle kommunikasjons parter har en error-kø der meldinger som feilet blir sendt. Ekstra data om feilen blir inkludert i meldings headeren.
- Avsender blir varslet dersom sertifikatene de benytter ikke lenger er gyldige. 
- Applikasjonslaget kan varsle avsender dersom XML ikke validerer
- Applikasjonslaget kan varsle avsender dersom den får data som ikke stemmer. f.eks. avvik mellom metadata og innhold i fagmelding.
- Applikasjonslaget kan varsle avsender om mer generelle feil.

Feil som mottas på vår error-kø varsles via en egen callback. Hva systemet ønsker å gjøre med disse kan variere. Helsenorge logger disse slik at driftspersonell kan feilsøke.

### Logging
All kommunikasjon med køer og registre logges. Feil blir også logget med egen definert EventId.

Det er verdt å nevne litt rundt logging. Når man ser på ASP.NET core koden, så blir det opprettet en ILogger instans når en controller blir instansiert. 
ILogger instansen representerer hvor i koden man begynte, og får et navn basert på dette. For vår MessagingServer, så har hver tråd sin egen ILogger instans.

Disse instansene kan være kortlevd, og annen informasjon på nettet indikerer at disse trenger ikke være thread safe. https://msdn.microsoft.com/magazine/mt694089

Siden MessagingClient og MessagingServer er singleton, så er det ikke naturlig å bruke en logger instans for alle requester. 
Derfor er det veldig mange metoder som tar inn en ILogger referanse slik at alt som skjer knyttet til en request havner i samme kategori.

### Correlation Id
Når man begynner å tenke på correlation id'er så er det noe ILogger systemet ikke har støtte for. Man kan tenke seg at man bruker 
RegisterAsynchronousMessageReceivedStartingCallback til å sette en correlation id som tagger alle logginnslagene knyttet til en spesifik melding. 

NLog er en implementasjon som støtter både ILogger og har støtte for AsyncLocal verdier via Mapped Diagnostic Logical Context. Denne brukes internt i Helsenorge koden. 

### Feilhåndtering
For å bli varslet om feil, så benyttes samme callback modell som for mottak av meldinger. 

```cs
server.RegisterErrorMessageReceivedCallback((message) => /* do something */ );
server.RegisterUnhandledExceptionCallback((message, exception) => /* do something */ );
server.RegisterHandledExceptionCallback((message, exception) => /* do something */ );
```

Dersom vi mottaker ønsker å sende feilmelding tilbake til avsender, så vil typen exeption avgjøre hva som skjer videre. 

Avsender skal varsles om XML fei.  errorCondition = transport:not-well-formed-xml
```cs
throw new XmlSchemaValidationException("XML-Error");
```
Avsender skal varsles om data som mangler/ikke stemmer. errorCondition = transport:invalid-field-value
```cs
throw new ReceivedDataMismatchException("Mismatch") 
{ 
    ExpectedValue = "Expected", 
    ReceivedValue = "Received"
};
``` 

Avsender skal varsles om en annen feil. errorCondition = transport:internal-error 
```cs
throw new NotifySenderException("Error description");
```
Avsender blir ikke varslet, og meldingen blir liggende på køen før vi prøver på nytt.
```cs
throw new ArgumentOutOfRangeException();
```