# Migrere fra 7.0 til 7.1
Ingen breaking changes er gjort i 7.1.0, men det er lagt til støtte for RabbitMQ sitt nye AMQP 1.0 adresseformat v2.

## Støtte for RabbitMQ AMQP Address format v2 (`UseAmqpAddressV2`)
Fra og med 7.1.0 er det lagt til støtte for RabbitMQ sitt nye [AMQP 1.0 Address format v2](https://www.rabbitmq.com/docs/amqp#target-address-v2) via `UseAmqpAddressV2`.

### Bakgrunn
RabbitMQ 4.0 introduserte et nytt adresseformat (v2) for AMQP 1.0, og det gamle v1-formatet er merket som deprecated og vil bli fjernet i en fremtidig RabbitMQ-versjon (ser ut til å bli RabbitMQ v5). 

### Hvordan ta i bruk
`UseAmqpAddressV2` er tilgjengelig i `MessagingSettings`:

```csharp
var settings = new MessagingSettings
{
    UseAmqpAddressV2 = true,
    // ...øvrige innstillinger...
};

// eller via AmqpSettings:
settings.AmqpSettings.UseAmqpAddressV2 = true;
```

### Viktig
- **Address format v2 krever RabbitMQ 4.0 eller nyere.** Ikke aktiver denne innstillingen mot brokere som kjører en eldre versjon.
- Standardverdien er `false` (v1-format), så eksisterende oppsett fungerer uendret etter oppgradering.

