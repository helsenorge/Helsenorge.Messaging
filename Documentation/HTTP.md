## Meldingsutveksling over HTTP

### Bakgrunn

* Vanskelig å integrasjonsteste meldingsutveksling


### Konfigurasjon

Dersom `ConnectionString` starter med `http` så vil Helsenorge.Messaging benytte HTTP for å sende og motta meldinger.

### Protokollen

I eksemplene under er ConnectionString = http://server/queues/

Legge en melding på køen q1:

* POST til http://server/queues/q1
* Request body er slik XML:

```
<AMQPMessage>	
	<MessageFunction>msgfunc</MessageFunction>
	<FromHerId>123</FromHerId>
	<ToHerId>456</ToHerId>
	<MessageId>msgid</MessageId>
	<CorrelationId>correlationid</CorrelationId>
	<EnqueuedTimeUtc>2017-01-01T13:30:10Z</EnqueuedTimeUtc>
	<ContentType>text/plain</ContentType>
	<Payload>
		<foo>a</foo>
	</Payload>
	<ApplicationTimestamp>2017-01-01T13:30:10</ApplicationTimestamp>
	<CpaId>cpaid</CpaId>
</AMQPMessage>
```


Hente melding fra køen q1:

* GET til http://server/queues/q1
* 404-respons dersom køen er tom
* Melding som lagt til dersom køen ikke er tom (og meldingen fjernes fra køen)

### Implementasjoner

TODO