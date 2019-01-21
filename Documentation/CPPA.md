## CPP og CPA

### CPP
En CPP definerer en del ting om en kommunikasjonspart. 
- Sertifikater
- Kønavn
- Hvilke kommunikasjonsprosseser og versjon av prossene som støttes

### CPA
En CPA er avhengig av at begge partene har en CPP registrert. For prossesen, så vil en CPA inneholde de versjonene begge parter støtter. 

| Prosess | Part A	| Part B | Resultat |
| --------|---------| ------ | -------- |
| P1      | V1,V2	| V1 	 | V1		|
| P2      | V1,V2   | V1,V2  | V2		|
| P2      | V1,V2   | ingen	 | ingen 	|


### Hvordan brukes CPA informasjonen?
Kønavn og sertifikater skulle være rimelig selvforklerende. Tjenester og versjoner er mere difust.

#### Protokoll
Når man skal sende en melding, så trenger vi å vite hvilken protokoll som skal brukes. Siden vi pr dags dato bare støtter AMQP, så virker dette kanskje unødvendig. Erfaringen vår viser at dersom det finnes en vei rundt å opprette nye prosseser, så blir den ofte benyttet. For å tvinge prosjekter i å opprette nye CPP prosseser, så har vi satt dette som et krav i koden vår. 

Dersom du skulle ønske å klone koden og gjøre lokal endring, så er det fult mulig å komme rundt dette. I MessagingClient så vil man finne følgende kode som man kan endre på. 

**Dette er å anse som en veldig midlertidig fiks siden du ikke vil få oppdateringer vi gjør.**  

```cs
var collaborationProtocolMessage = await PreCheck(logger, message).ConfigureAwait(false);

switch (collaborationProtocolMessage.DeliveryProtocol)
{
    case DeliveryProtocol.Amqp:
        await _asynchronousServiceBusSender.SendAsync(logger, message).ConfigureAwait(false);
        return;
    case DeliveryProtocol.Unknown:
    default:
        throw new NotImplementedException();
}
```

Et mere avansert alternativ er å lage en egen implementasjon av ICollaborationProtocolRegistry som pakker inn vår implementasjon. Man kan så legge på eller endre dataene som vi returnerer.  


#### Prossesversjoner og XML-versjoner

En prossess kan få ny versjon av to grunner.

1. Ny funksjonalitet
2. Underliggende XML har endret seg. 

I tillegg til protokoll, så spesifiserer en kommunikasjonsprosses hvilke XSDer og XML namespace som skal benyttes.

Et eksempel på alternativ 1 er Digital Dialog Fastleges endring av time. Denne funksjonaliteten krever ingen endring i meldingsformatet, men er en funksjon hver mottpart kan støtte. 

Et eksempel på alternativ 2 er Dialogmelding; spesialist bruker 1.0, mens fastlege benytter 1.1.

Når man mottar en melding via MessagingServer, så vil det ligge ved informasjon om CPA-profilen til avsender. Basert på versjonene og XML-informasjonen som er definert, så vil man være i stand til å bygge opp en XML som avsender kan konsumere.
   
