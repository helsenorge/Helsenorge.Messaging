## Registre
For å kunne kommunisere riktig med en mottpart, så er vi avhengig av å vite en del om protokoller, sertifikater, og kønavn. All denne informasjonen ligger i adresseregisteret og CPA-registeret.  

Meldingsutvekslingen er avhengig av adresseregisteret og CPA-registeret. 

### Registerintegrasjon

Før man kan sette opp infrastrukturen for meldinger, så må man ha registerintegrasjonen på plass og HelseId.
Denne koden bruker klasser fra andre Microsoft.Extensions.* pakkker. Se også Helsenorge.Messaging.Client prosjektet for eksempler på hvordan det kan settes opp.

```cs
var serviceCollection = new ServiceCollection();
serviceCollection.AddLogging(loggerConfiguration =>
            {
                loggerConfiguration.AddConsole();
            });

var distributedCache = new MemoryDistributedCache(new MemoryCache(new MemoryCacheOptions()));

var addressRegistrySettings = new AddressRegistrySettings();
addressRegistrySettings.UserName = "user name";
addressRegistrySettings.Password = "password";
addressRegistrySettings.WcfConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
addressRegistrySettings.CachingInterval = TimeSpan.FromHours(12);			
            
var addressRegistry = new AddressRegistry(addressRegistrySettings, distributedCache, new Logger());

var collaborationProtocolRegistrySettings = new CollaborationProtocolRegistryRestSettings();
collaborationProtocolRegistrySettings.RestConfiguration.Address = "adresse til CPPA tjenesten";
collaborationProtocolRegistrySettings.RestConfiguration.IsDpopEnabled = false; //eller true for endepunkt med dpop støtte
collaborationProtocolRegistrySettings.CachingInterval = TimeSpan.FromHours(12);			

var cppaHelseidConfiguratrion = new HelseIdConfiguration("HelseId klient id", "scopet til endepunktet", "IssuerUri");

ISecurityKeyProvider provider = new () //Implementasjon av ISecurityKeyProvider. Se HelseId.Library.HelseIdServiceCollectionExtensions for alternativer
var jsonWebKey = provider.GetSecurityKey() as JsonWebKey;

var helseIdBuilder = serviceCollection.AddHelseIdClientCredentials(cppaHelseidConfiguratrion)
            .AddHelseIdInMemoryCaching() 
            .AddSigningCredentialForClientAuthentication(new SigningCredentials(jsonWebKey, jsonWebKey.Alg));
            
// Register a service that will call HelseID
helseIdBuilder.Services.AddHttpClient();

var serviceProvider = serviceCollection.BuildServiceProvider();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger<Program>();

```
