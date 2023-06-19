## Eksempler
Eksemplene i dette prosjektet er kun ment som eksempler for å komme i gang og skal i hovedsak ansees som naive, vi anbefaler ikke å bruke eksemplene
direkte i produksjon.

Selv om eksemplene er svært nyttig for en utvikler for å forstå programflyt og oppsett anbefales det at man gjøre en grundig analyse av eget behov og
følger de retningslinjene som er beskrevet i [Sending av meldinger](SendeMeldinger.md) og [Mottak av meldinger](MottaMeldinger.md).

#### Eksempelapplikasjoner på bruk av APIene `MessagingClient` og `MessagingServer`

- Helsenorge.Messaging.Client
  - Meldingsprodusent.
  - Benytter APIet `MessagingClient`.
  - Viser oppsett for sending av asynkrone og synkrone meldinger.
- Helsenorge.Messaging.Server
  - Meldingskonsument
  - Benytter APIet `MessagingServer`.
  - Viser oppsett for mottak av asynkrone og synkrone melding.
  - Enkle eksempler på oppsett av callbacks/events eksponert på `MessagingServer`.

#### Andre eksempelapplikasjoner

- PooledReceiver
  - Enkel meldingskonsument med pooling kapabiliteter for Connections Receivers, bruker APIene `LinkFactoryPool` og `AmqpReceiver`.
- PooledSender
  - Enkel meldingsprodusent med pooling kapabiliteter for Connections Receivers, bruker APIene `LinkFactoryPool` og `AmqpSender`.
- ReceiveDecryptAndValidate
  - Enkel meldingskonsument, bruker APIene `SignThenEncryptMessageProtection`, `CertificateStore`, `LinkFactoryPool` og `AmqpReceiver`.
- SignEncryptAndSend
  - Enkel meldingsprodusent, bruker APIene `SignThenEncryptMessageProtection`, `CertificateStore`, `LinkFactoryPool` og `AmqpSender`.
- RepublishExample
  - Enkelt eksempel for republisering av meldinger fra Deadletter. Bruker av APIet `QueueClient`.
- SearchByIdAndGetOrganizationDetailsExample
  - Eksempel på bruk av APIene `AddressRegistry.SearchByIdAsync` og `AddressRegistry.GetOrganizationDetailsAsync`.
- SimpleReceiver
  - Enkel meldingskonsument, bruker APIene `LinkFactory` og `AmqpReceiver`.
- SimpleSender
  - Enkel meldingsprodusent, bruker APIene `LinkFactory` og `AmqpSender`.
- SubscriptionExample
  - Eksempel på oppsett av subscription og konsumenere meldinger fra subscription, bruker APIene `BusManager`, `LinkFactoryPool` og `AmqpReceiver`.