## Konfigurasjon

Det finnes en god del parametere man kan sette gjennom konfigurasjon. Noen har en default verdi, andre må settes eksplisitt. Dersom noen mangler, så får man exception når ting startes opp. 

Objektet som brukes for å konfigurere systemet støtter den nye konfigurasjonsmodellen som er introdusert via .NET Core; [JSON baserte filer](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json/). 

Man kan definere fellesting i en fil, og overstyre/legge til verdier fra en annen fil. Dette brukes i referanseeksemplene våre. 

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
				"Synchronous": {
					"ReplyQueueMapping": {
						"MACHINE-NAME": "1111_syncreply"
					}
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