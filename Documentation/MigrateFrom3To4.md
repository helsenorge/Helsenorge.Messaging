## Migrere fra 3.0 til 4.0

### Nytt ConnectionString format
Formatet på ConnectionString er endret til: `amqps://<username>:<password>@sb.test.nhn.no:5671/NHNTESTServiceBus`.

Exchange/namespace er case-sensitive:
- Test: NHNTESTServiceBus
- Produksjon: NHNProdServiceBus

Se også mer utfyllende informasjon om URL, IP-adresser og porter på [Meldingsutveksling med Helsenorge](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/690913297/Meldingsutveksling+med+Helsenorge)

### Introduksjon av MessageBrokerDialect
Helsenorge.Messaging 4.x er kompatibel med både Microsoft ServiceBus og RabbitMQ. Det er derfor introdusert et nytt konfigurasjonsattributt `MessagingSettings.ServiceBus.MessageBrokerDialect`. Attributtet har to gyldige verdier `ServiceBus` og `RabbitMQ`. Verdien `ServiceBus` er standardverdien. For å kommunisere med RabbitMQ, og benytte tilhørende dialekt for kø-routing og -adressering, må verdien eksplisitt settes til `RabbitMQ`.

### Endringer i SOAP-konfigurasjon
SOAP-konfigurasjonen er flyttet fra klassisk .NET Framework SOAP konfigurasjon til en ny og enklere struktur.

#### Eksempler

Eksempel på SOAP-konfigurasjon:
```
{
    "UserName": "AUserName",
    "Password": "APassword",
    "Address": "https://ws-web.test.nhn.no/v1/AR/Basic",
    "MaxBufferSize": 2147483647,
    "MaxBufferPoolSize": 2147483647,
    "MaxReceivedMessageSize": 2147483647
}
```

Eksempel på SOAP-konfigurasjon i våre eksempel-applikasjoner, se [her](https://github.com/helsenorge/Helsenorge.Messaging/blob/56870226c20d83467df8eb78e1ccd72e165f663a/Examples/Helsenorge.Messaging.Server/appsettings.json#L11).

#### Konfigurasjon klasse
Dette er klassen som benyttes internt av Helsenorge.Messaging. Den kan også benyttes direkte i egen kode dersom man ønsker å sette konfigurasjonen i kode.

```csharp
public class WcfConfiguration
{
	/// <summary>
	/// Username used for connecting
	/// </summary>
	public string UserName { get; set; }
	/// <summary>
	/// Password used for connecting
	/// </summary>
	public string Password { get; set; }
	/// <summary>
	/// Endpoint address.
	/// </summary>
	public string Address { get; set; }
	public WfcHttpBinding HttpBinding { get; set; } = WfcHttpBinding.Basic;
	public int MaxBufferSize { get; set; }
	public int MaxBufferPoolSize { get; set; }
	public int MaxReceivedMessageSize { get; set; }
}
public enum WfcHttpBinding
{
	Basic = 1,
	WsHttp
}
```

### Andre endringer i biblioteket
Biblioteket er kompatibelt med .NET 6.0, .NET Framework 4.6.2 og senere versjoner.

Vi har flyttet store deler av kodebasen over til et async-pattern og derfor vil mange av metodene måtte awaites.
