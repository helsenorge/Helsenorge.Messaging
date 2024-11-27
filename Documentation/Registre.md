## Registre
For å kunne kommunisere riktig med en mottpart, så er vi avhengig av å vite en del om protokoller, sertifikater, og kønavn. All denne informasjonen ligger i adresseregisteret og CPA-registeret.  

Meldingsutvekslingen er avhengig av adresseregisteret og CPA-registeret. 

### Registerintegrasjon

Før man kan sette opp infrastrukturen for meldinger, så må man ha registerintegrasjonen på plass og HelseId.
Denne koden bruker klasser fra andre Microsoft.Extensions.* pakkker. Se også Helsenorge.Messaging.Client prosjektet for eksempler på hvordan det kan settes opp.

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
            
var addressRegistry = new AddressRegistry(addressRegistrySettings, distributedCache, new Logger());

var helseidConfiguratrion = new HelseIdConfiguration();
helseidConfiguratrion.ClientId = "HelseId klient id"
helseidConfiguratrion.TokenEndpoint = "Endepunkt til helseid"
helseidConfiguratrion.ScopeName = "scopet til endepunktet"
ISecurityKeyProvider provider = new () //Implementasjon av ISecurityKeyProvider
var helseIdClient = new HelseIdClient(helseidConfiguratrion, provider);

var collaborationProtocolRegistrySettings = new CollaborationProtocolRegistryRestSettings();
collaborationProtocolRegistrySettings.RestConfiguration.Address = "adresse til CPPA tjenesten";
collaborationProtocolRegistrySettings.CachingInterval = TimeSpan.FromHours(12);			
            
var collaborationProtocolRegistry = new CollaborationProtocolRegistryRest(collaborationProtocolRegistrySettings, distributedCache, addressRegistry, new Logger(), helseIdClient);
```
