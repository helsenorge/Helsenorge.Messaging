## Konfigurasjon

Det finnes en god del parametere man kan sette gjennom konfigurasjon. Noen har en default verdi, andre må settes eksplisitt. Dersom noen mangler, så får man exception under oppstart. 

Objektet som brukes for å konfigurere systemet støtter den nye konfigurasjonsmodellen som er introdusert via .NET Core; [JSON baserte filer](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json/). 

Man kan definere felles egenskaper i én fil, og overstyre/legge til verdier fra en annen fil. Dette gjøres i referanseeksemplene våre. 

ProcessingTask definerer hvor mange tråder man skal spinne opp for de forskjellige meldingstypene. 
Max* bestemmer hvor mange aktive objekter vi har for Azure Servicebus.

Dette er et eksempel på hvordan en slik fil kan se ut. 

    "AddressRegistrySettings": {
        "EndpointName": "BasicHttpBinding_ICommunicationPartyService",
        "CachingInterval": "00:05:00"
    },
    "CollaborationProtocolRegistrySettings": {
        "EndpointName": "BasicHttpBinding_ICPPAService",
        "CachingInterval": "00:05:00",
        "MyHerId": "1234"
    },
    "MessagingSettings": {
        "MyHerId": "1234",
        "IgnoreCertificateErrorOnSend": "false",
        "ServiceBus": {
            "ConnectionString": "",
            "MaxReceivers": 5,
            "MaxSenders": 200,
            "MaxFactories": 5,
            "Asynchronous": {
                "ProcessingTasks": 5,
                "TimeToLive": "4.00:00:0",
                "ReadTimeout": "00:00:01"
            },
            "Synchronous": {
                "ProcessingTasks": 2,
                "TimeToLive": "00:00:15",
                "ReadTimeout": "00:00:01",
                "ReplyQueue": "",
                "CallTimeout": "00:00:15",
                "ReplyQueueMapping": {
                    "MACHINE-NAME": "1111_syncreply"
                }
            },
            "Error": {
                "ProcessingTasks": 1,
                "TimeToLive": "10675199.02:48:05",
                "ReadTimeout": "00:00:01"
            }
        },
        "DecryptionCertificate": {
            "Thumbprint": "hex",
            "StoreName": "My",
            "StoreLocation": "LocalMachine"
        },
        "SigningCertificate": {
            "Thumbprint": "hex",
            "StoreName": "My",
            "StoreLocation": "LocalMachine"
        }
    }

### NHN-miljøer
NHN tilbyr to forsjellige miljøer for meldingsutveksling; test og produksjon. Man kan kontakte meldingsutveksleren via .NET-protokoll, eller AMPQ. Sistnevnte vil bli benyttet av Java-applikasjoner.

Connection-stringen, som er vist nedenfor, illustrerer .NET-versjonen. Den inneholder en del porter som må være åpne. For AMQP så benyttes port 5671. 

For mer informasjon, kontakt NHN Kundeservice.  
#### Test
Dette miljøet er tilgjengelig på Internett. Det er Her-Iden som bestemer hvilken kø som skal benyttes. Man kan ha fint ha 10 forskjellige testmiljøer og gå mot samme meldingsutveksler så lenge man har unike Her-Ider. 

```
Endpoint=sb://sb.test.nhn.no/NHNTESTServiceBus;StsEndpoint=https://sb.test.nhn.no:9355/NHNTESTServiceBus;RuntimePort=9354;ManagementPort=9355;OAuthUsername=[username];OAuthPassword=[password]
```
#### Produksjon
Dette miljøet er bare tilgjengelig på helsenettet. NHN må involveres for å komme inn på dette. 

```
Endpoint=sb://sb.nhn.no/NHNPRODServiceBus;StsEndpoint=https://sb.nhn.no:9355/NHNPRODServiceBus;RuntimePort=9354;ManagementPort=9355;OAuthUsername=[username];OAuthPassword=[password]
```
