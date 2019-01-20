## Sending av meldinger

Hovedvekten av meldingene som sendes vil være av asynkron natur; man sender og forventer et svar en gang i fremtiden. Om det tar minutter eller timer kan variere. 

For noen typer tjenester, så trenger man svaret med en gang; funksjonelt synkron melding. Teknisk så betyr dette at vi sender en melding på lik måte som asynkront, men vi blokkerer tråden som sender inntil vi får et svar. Dersom dette ikke skjer innen x antall sekunder, så går vi i timeout. 

Den synkrone håndteringen tar utgangspunkt i at hver prosess/maskin har sin egen kø der svar på synkrone meldinger kommer; kjent som syncreply-kø. Dersom man bare hadde én synkreply-kø for flere maskiner, så vil det vesentlig øke kompleksiteten. Hva skjer dersom en maskin tar meldingen, som en annen har en blokkerende tråd på? 

Når vi sender er en synkron melding, så setter vi eksplisitt hvilken kø svaret skal tilbake på.    
Koblingen mellom maskin og kønavn settes opp i konfigurasjonen.

    "Synchronous": {
        "CallTimeout":  "00:00:15",
        "ReplyQueueMapping": {
            "MACHINE-NAME": "11111_syncreply"
            }
    }
 
Siden synkrone meldinger har begrenset levetid og alle trådene som venter er blokkert, så er koden skrevet slik at de samarbeider. Alle tråder som venter, henter meldinger fra køen og legger de i et internt minnebuffer. Før en tråd sjekker køen, så sjekker de om en annen tråd har hentet den de er interessert i. 

Basert på denne strukturne, så **må MessagingClient brukes som singleton**.

```cs
var messagingSettings = new MessagingSettings(); // denne har mange verdier satt som standard som man kan overstyre
messagingSettings.MyHerId = "1234";			
messagingSettings.DecryptionCertificate = "aaaaabbbbbbb";
messagingSettings.SigningCertificate = "ccccccddddd";
messagingSettings.ServiceBus.Synchronous.ReplyQueue = "MyReplyQueue";
messagingSettings.ServiceBus.ConnectionString = "connection string";

var client = new MessagingClient(messagingSettings, collaborationProtocolRegistry, addressRegistry);

var outgoingMessage = new OutgoingMessage()
{
    ToHerId = 789,
    CpaId = Guid.Empty,
    Payload = new XDocument(),
    MessageFunction = "DUMMY_MESSAGE_FUNCTION",
    MessageId = Guid.NewGuid().ToString("D"),
    ScheduledSendTimeUtc = DateTime.Now,
    PersonalId = "12345",
};

// for asynkrone meldinger
await client.SendAndContinueAsync(logger, outgoingMessage);

// for synkrone meldinger
var xml = await client.SendAndWaitAsync(logger, outgoingMessage);
```
