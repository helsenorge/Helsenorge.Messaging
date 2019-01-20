## Registre
For å kunne kommunisere riktig med en mottpart, så er vi avhengig av å vite en del om protokoller, sertifikater, og kønavn. All denne informasjonen ligger i adresseregisteret og CPA-registeret.  

Meldingsutvekslingen er ekstremt avhengig av adresseregisteret og CPA-registeret. 

### Registerintegrasjon

Før man kan sette opp infrastrukturen for meldinger, så må man ha registerintegrasjonen på plass. 
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
collaborationProtocolRegistrySettings.MyHerId = "1234";
collaborationProtocolRegistrySettings.UserName = "user name";
collaborationProtocolRegistrySettings.Password = "password";
collaborationProtocolRegistrySettings.WcfConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
collaborationProtocolRegistrySettings.CachingInterval = TimeSpan.FromHours(12);			
            
var collaborationProtocolRegistry = new CollaborationProtocolRegistry(collaborationProtocolRegistrySettings, distributedCache);
```
