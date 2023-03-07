## Migrere fra 4.0 til 5.0

Hovedendringen for 5.0 er støtte for arv av køsett fra virksomheten slik at flere kommunikasjonsparter kan dele det samme settet med køer fra virksomheten.
I tillegg er det fjernet ubrukte metoder og egenskaper som hadde relevans for MS ServiceBus og gjort visse navneendringer. Alle endringer er
dokumentert i dette migreringsdokumentet.

Alle `async` metoder har nå endelsen Async. F.eks `MessagingServer.Start` er nå endret til `MessagingServer.StartAsync`.

### Konfigurasjonsrelaterte endringer

- `MessagingSettings.MyHerId` &rarr; `MessagingSettings.MyHerIds`
  - Datatypen er endret fra `int` til `List<int>`
- `CollaborationProtocolRegistrySettings.MyHerId` er fjernet
- `MessagingSettings.ServiceBus` &rarr; `MessagingSettings.AmqpSettings`
- Standardverdien tilhørende `MessagingSettings.AmqpSettings.MessageBrokerDialect` er endret fra `MessageBrokerDialect.ServiceBus`
  til `MessageBrokerDialect.RabbitMQ`

### Nye features

For å avhjelpe oppbygging av Connection String fra kode har klassen AmqpConnectionString blitt lagt til.

### Navneendring og endring av datatype for egenskapen MessagingSettings.MyHerId

Egenskapen `MyHerId` på klassen `MessagingSettings` har endret navn til `MyHerIds`, i tillegg har datatypen blitt endret fra
`int` til `List<int>`.

### Fjernet CollaborationProtocolRegistrySettings.MyHerId

Egenskapen `MyHerId` på klassen `CollaborationProtocolRegistrySettings` er fjernet. Internt benytter biblioteket nå egenskapene
`IncomingMessage.ToHerId` og `OutgoingMessage.FromHerId` for å resolve egen HerId. 

### Endringer på interfacet ICollaborationProtocolRegistry og klassen CollaborationProtocolRegistry

ILogger argumentet har blitt fjernet fra metodene og er flyttet som en avhengighet til konstruktøren på `CollaborationProtocolRegistry`. 
Metodene har blitt utvidet med parameteret `myHerId` av type `int` (uthevet i fet kursiv tekst) og `ILogger` argumentet er fjernet:

- FindAgreementByIdAsync(ILogger, Guid) &rarr; FindAgreementByIdAsync(Guid, **_int_**)
- FindAgreementByIdAsync(ILogger, Guid, bool) &rarr; FindAgreementByIdAsync(Guid, **_int_**, bool)
- FindAgreementForCounterpartyAsync(ILogger, int) &rarr; FindAgreementForCounterpartyAsync(**_int_**, int)
- FindAgreementForCounterpartyAsync(ILogger, int, bool) &rarr; FindAgreementForCounterpartyAsync(ILogger, **_int_**, int, bool)
- GetCollaborationProtocolProfileAsync(ILogger, Guid, bool) &rarr; GetCollaborationProtocolProfileAsync(Guid, bool)

### Endringer på interfacet IAddressRegistry og klassen AddressRegistry

ILogger argumentet har blitt fjernet fra metodene og er flyttet som en avhengighet til konstruktøren på `AddressRegistry`.

- FindCommunicationPartyDetailsAsync(ILogger, int herId) &rarr; FindCommunicationPartyDetailsAsync(int herId)
- FindCommunicationPartyDetailsAsync(ILogger, int herId, bool forceUpdate) &rarr; FindCommunicationPartyDetailsAsync(int herId, bool forceUpdate)
- GetCertificateDetailsForEncryptionAsync(ILogger, int herId) &rarr; GetCertificateDetailsForEncryptionAsync(int herId)
- GetCertificateDetailsForEncryptionAsync(ILogger, int herId, bool forceUpdate) &rarr; GetCertificateDetailsForEncryptionAsync(int herId, bool forceUpdate)
- GetCertificateDetailsForValidatingSignatureAsync(ILogger, int herId) &rarr; GetCertificateDetailsForValidatingSignatureAsync(int herId)
- GetCertificateDetailsForValidatingSignatureAsync(ILogger, int herId, bool forceUpdate) &rarr; GetCertificateDetailsForValidatingSignatureAsync(int herId, bool forceUpdate)
- PingAsync(ILogger) &rarr; PingAsync()

### ServiceBusHttpClient er fjernet

ServiceBusHttpClient har vært merket som `Obsolete` i gjennom versjon 4.0 av biblioteket og fjernes nå i forbindelse med at støtte
for produktet Microsoft ServiceBus fases ut. Lignende funksjonalitet eksisterer ikke over AMQP 1.0 for RabbitMQ. 

#### Metodene RenewLock{Async} er fjernet fra IMessagingMessage og IncomingMessage

Disse var avhengig av funksjonalitet som lå i klassen ServiceBusHttpClient og hadde kun relevans mot en MS ServiceBus instans.
I tillegg dette var funksjonaliteten svært ustabil.

Følgende metoder er fjernet:

- `IMessagingMessage.RenewLock()`
- `IMessagingMessage.RenewLockAsync()`
- `IncomingMessage.RenewLock()`
- `IncomingMessage.RenewLockAsync()`

#### Andre endringer på IMessagingMessage

Følgende metoder og egenskaper har endringer:

Endret:
- `IMessagingMessage.GetValue()` &rarr; `IMessagingMessage.GetApplicationPropertyValue()`
- `IMessagingMessage.SetApplicationProperty()` &rarr; `IMessagingMessage.SetApplicationPropertyValue()`

Fjernet:
- `IMessagingMessage.ScheduledEnqueTimeUtc`
- `IMessagingMessage.RenewLock()`
- `IMessagingMessage.RenewLockAsync()`

#### Navneendring på namespace 

Følgende namespace er endret:

- `Helsenorge.Messaging.ServiceBus` &rarr; `Helsenorge.Messaging.Amqp` 

#### Navneendring på klasser, interfacer og egenskaper

Navneendringer på klasser og interfacer:
- `IMessagingMessage` &rarr; `IAmqpMessage`
- `IMessagingFactory` &rarr; `IAmqpFactory`
- `IMessagingReceiver` &rarr; `IAmqpReceiver`
- `IMessagingSender` &rarr; `IAmqpSender`
- `ICachedMessagingEntity` &rarr; `ICachedAmqpEntity`
- `MessagingEntityCache` &rarr; `AmqpEntityCache`
- `ServiceBusSettings` &rarr; `AmqpSettings`
- `ServiceBusConnection` &rarr; `AmqpConnection`
- `ServiceBusCore` &rarr; `AmqpCore`
- `ServiceBusException` &rarr; `AmqpException`
- `ServiceBusCommunicationException` &rarr; `AmqpCommunicationException`
- `ServiceBusException` &rarr; `AmqpException`
- `ServiceBusTimeoutException` &rarr; `AmqpTimeoutException`
- `RecoverableServiceBusException` &rarr; `RecoverableAmqpException`
- `UncategorizedServiceBusException` &rarr; `UncategorizedAmqpException`
- `IServiceBusManager` &rarr; `IBusManager`
- `ServiceBusManager` &rarr; `BusManager`
- `ServiceBusManagerSettings` &rarr; `BusManagerSettings`
- `IServiceBusFactoryPool` &rarr; `IAmqpFactoryPool`

Navneendringer på egenskaper:
- `MessagingCore.Core` &rarr; `MessagingCore.AmqpCore`
- `MessagingSettings.MyHerId` &rarr; `MessagingSettings.MyHerIds`
- `MessagingSettings.ServiceBus` &rarr; `MessagingSettings.AmqpSettings`


#### Message broker kompatibilitet

Helsenorge.Messaging 5.0 er fortsatt kompatibel med MS ServiceBus i tillegg til RabbitMQ.
