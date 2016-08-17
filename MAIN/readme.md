# Helsenorge Messaging

## Oversikt

Et av integrasjonalternativene mot Helsenorge er å sende XML meldinger via et sett med køer. Denne pakken gjør det lettere å integrere seg via denne kanalen. 
Denne pakken håndterer følgende funksjoner:

### Sending og mottak av meldinger
Pakken støtter det som kalles asynkrone og synkrone meldinger. Asynkrone meldinger sendes og en gang i fremtiden så kan det komme et svar. 
Synkrone meldinger er teknisk sett asynkrone, men funksjonelt synkrone; man blokkerer inntil man får et svar eller timer ut.

Pakken støtter både mottak og sending av meldinger. Noen ganger så kan man tenke seg at man bare trenger sending eller mottak, men i praksis så trenger man begge. 
Ofte så vil applikasjonslaget kreve kvitteringmeldinger som skal mottas dersom man sender, eller sendes dersom man mottar. 

Pakken er bygd slik at man kan registrer callbacks når man mottar de forskjellige meldingstypene. Applikasjonslaget står da fritt til å gjøre det den vil med meldingen.

Dersom en melding feiler i mottak, så blir meldingen liggende igjen på køen og vi prøver igjen (maks 10 ganger). Unntaket er feil som skal varsles til avsender. 

### Registre
For å kunne kommunisere med andre parter, så trenger vi å vite hvor meldinger skal sendes. Dette er informasjon som hentes ut fra forskjellige registre.
Pakken er avhengig av disse registerintegrasjonene.

### Kryptering og signering
Dataene som ligger i meldingene blir først signert, så kryptert. Sertifikatinfrastrukturen baserer seg på informasjon i adresseregisteret samt private sertifikater.
Pakken støtter kryptering, dekryptering, signering, og signatur validering.

### Feilhåndtering
Pakken har bred støtte rundt feilhåndtering. 
- Alle kommunikasjons parter har en error-kø der meldinger som feilet blir sendt. Ekstra data om feilen blir inkludert i meldings headeren.
- Avsender blir varslet dersom sertifikatene de benytter ikke lenger er gyldige. 
- Applikasjonslaget kan varsle avsender dersom XML ikke validerer
- Applikasjonslaget kan varsle avsender dersom den får data som ikke stemmer. f.eks. forskjell mellom det som kommer i header og det som ligger i XML.
- Applikasjonslaget kan varsle avsender om mere generelle feil.

Feil som mottas på vår error-kø varsles via en egen callback. Hva systemet ønsker å gjøre med disse kan variere. Helsenorge logger disse slik at driftspersonell kan feilsøke.

### Logging
All kommunikasjon med køer og registre logges. Feil blir også logget med egen definert EventId.

## Forutsetninger

Før du kan ta i bruk denne pakke, så er det en del forutsetninger som må være på plass. 

- Løsningen din må støtte .NET 4.6
- Din organisasjon må være registrert i adresseregisteret.
- Du må ha brukernavn og passord til adresseregisteret og CPA tjenesten
- Du må ha brukernavn og passord til kø systemet, samt connection string.
- Du må vite hva din her-id er for noe
- Du må ha private sertifikater for kryptering og signering. 
- Systemet ditt må være på helsenettet. 
- Det må ha blitt opprettet et sett med køer for deg. Dersom du skal sende synkrone meldinger, så må hver mottagene server ha sin egen kø.  

Adresseregisteret, CPA tjenesten og kø systemet driftes av Norsk Helse Nett. 

## Integrering med din kode

### Eksterne pakkeavhengigheter

Koden i denne pakken benytter 
- Microsoft.Extensions.Caching.Abstractions.IDistributedCache
- Microsoft.Extensions.Logging.Abstractions.ILogger

Disse tilbyr generelle grensesnitt for logging og caching. Pakkene er en del av den nye ASP.NET Core stacken, men fungerer fint med .NET 4.6. 
For faktisk implementasjon av disse grensesnittene så kan man enten bruke noe som allerede er laget, eller benytte en egen implementasjon.

### Register integrasjon

Før man kan sette opp infrastrukturen for meldinger, så må man ha register integrasjonen på plass. 
Denne koden bruker klasser fra andre Microsoft.Extensions.* pakkker.

```cs
var loggerFactory = new LoggerFactory();
loggerFactory.AddConsole();
var logger = loggerFactory.CreateLogger<Program>();

var distributedCache = new MemoryDistributedCache(new MemoryCache(new MemoryCacheOptions()));

var addressRegistrySettings = new AddressRegistrySettings();
addressRegistrySettings.UserName = "user name";
addressRegistrySettings.Password = "password";
addressRegistrySettings.WcfConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
addressRegistrySettings.CachingInterval = TimeSpan.FromHours(12);			
			
var addressRegistry = new AddressRegistry(addressRegistrySettings, distributedCache);

var collaborationProtocolRegistrySettings = new CollaborationProtocolRegistrySettings();
collaborationProtocolRegistrySettings.MyHerId = "1234;
collaborationProtocolRegistrySettings.UserName = "user name";
collaborationProtocolRegistrySettings.Password = "password";
collaborationProtocolRegistrySettings.WcfConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
collaborationProtocolRegistrySettings.CachingInterval = TimeSpan.FromHours(12);			
			
var collaborationProtocolRegistry = new CollaborationProtocolRegistry(collaborationProtocolRegistrySettings, distributedCache);
```

### Sending av meldinger

MessagingClient klassen er designet for å være singleton. Dersom den ikke er det, så vil det skape problemer med synkrone meldinger.

```cs
var messagingSettings = new MessagingSettings(); // denne har mange verdier satt som standard som man kan overstyre
messagingSettings.MyHerId = "1234;			
messagingSettings.DecryptionCertificate = "aaaaabbbbbbb";
messagingSettings.SigningCertificate = "ccccccddddd";
messagingSettings.ServiceBus.Synchronous.ReplyQueue = "MyReplyQueue;
messagingSettings.ServiceBus.ConnectionString = "connection string";

var client = new MessagingClient(messagingSettings, collaborationProtocolRegistry, addressRegistry);

var outgoingMessage = new OutgoingMessage()
{
	ToHerId = 789,
	CpaId = Guid.Empty,
	Payload = new XDocument(),
	MessageFunction = "DUMMY_MESSAGE_FUNCTION",
	MessageId = Guid.NewGuid().ToString("D"),
	ScheduledSendTimeUtc = DateTime.Now,
	PersonalId = "12345",
};

// for asynkrone meldinger
client.SendAndContinue(logger, outgoingMessage);

// for synkrone meldinger
client.SendAndWait(logger, outgoingMessage);
```

### Mottak av meldinger

```cs
var server = new MessagingServer(messagingSettings, logger, collaborationProtocolRegistry, addressRegistry);

server.RegisterAsynchronousMessageReceivedStartingCallback((message) => /* do something */ );
server.RegisterAsynchronousMessageReceivedCallback((message) => /* do something */ );
server.RegisterAsynchronousMessageReceivedCompletedCallback((message) => /* do something */ );

server.RegisterSynchronousMessageReceivedStartingCallback((message) => /* do something */ );
server.RegisterSynchronousMessageReceivedCallback((message) => /* do something */ { return new XDocument();} );
server.RegisterSynchronousMessageReceivedCompletedCallback((message) => /* do something */ );
```

### Feilhåndtering

```cs
server.RegisterErrorMessageReceivedCallback((message) => /* do something */ );
server.RegisterUnhandledExceptionCallback((message, exception) => /* do something */ );
server.RegisterHandledExceptionCallback((message, exception) => /* do something */ );

 // avsender blir varslet om xml feil. errorCondition = transport:not-well-formed-xml
throw new XmlSchemaValidationException("XML-Error");

// avsender blir varslet om at dataene som kom ikke stemte. errorCondition = transport:invalid-field-value
throw new ReceivedDataMismatchException("Mismatch") { ExpectedValue = "Expected", ReceivedValue = "Received"};

// avsender blir varslet om annen feil. errorCondition = transport:internal-error 
throw new NotifySenderException("NotifySender");

// avsender blir ikke varslet, og meldingen blir liggende på køen og vi prøver på nytt
throw new ArgumentOutOfRangeException();
```

### Logging
Det er verdt å nevne litt rundt logging. Når man ser på ASP.NET core koden, så blir det opprettet en ILogger instans når en controller blir instansiert. 
ILogger instansen representerer hvor i koden man begynte, og får et navn basert på dette. For vår MessagingServer, så har hver tråd sin egen ILogger instans.

Disse instansene kan være kortlevd, og annen informasjon på nettet indikerer at disse trenger ikke være thread safe. https://msdn.microsoft.com/magazine/mt694089

Siden MessagingClient og MessagingServer er singleton, så er det ikke naturlig å bruke en logger instans for alle requester. 
Derfor er det veldig mange metoder som tar inn en ILogger referanse slik at alt som skjer knyttet til en request havner i samme kategori.

Når man begynner å tenke på correlation id'er så er det noe ILogger systemet ikke har støtte for. Man kan tenke seg at man bruker 
RegisterAsynchronousMessageReceivedStartingCallback til å sette en correlation id som tagger alle logginnslagene knyttet til en spesifik melding. 

NLog støtter både ILogger og har støtte for AsyncLocal verdier via Mapped Diagnostic Logical Context.

### Eksempler
Det finnes to konsoll applikasjoner som viser en klient og server komponent. 
Kliententen sender xml filer som ligger på disk, og serveren skriver mottatte meldinger til disk. 

Helsenorge.Messaging.Client
Helsenorge.Messaging.Server

