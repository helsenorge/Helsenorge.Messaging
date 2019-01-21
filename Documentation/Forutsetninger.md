## Forutsetninger

Før du kan ta i bruk denne pakken, så er det en del forutsetninger som må være på plass. 

- Løsningen din må støtte .NET 4.6
- Din organisasjon må være registrert i adresseregisteret.
- Du må ha brukernavn og passord til adresseregisteret og CPA-tjenesten
- Du må ha brukernavn og passord til køsystemet, samt connection string.
- Du må vite hva din her-id er
- Du må ha private sertifikater for kryptering og signering. 
- Systemet ditt må være på helsenettet. 
- Det må ha blitt opprettet et sett med køer for deg. Dersom du skal sende synkrone meldinger, så må hver mottagende server ha sin egen kø.  
- Meldingene du skal sende må ha profiler registert i CPP.
- Det må være opprettet en CPP profil for din organisasjon.

Adresseregisteret, CPA-tjenesten og køsystemet driftes av Norsk Helse Nett. 

## Eksterne pakkeavhengigheter

Koden i denne pakken benytter 
- [Microsoft.Extensions.Caching.Abstractions.IDistributedCache](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Abstractions/)
- [Microsoft.Extensions.Logging.Abstractions.ILogger](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/)

Disse tilbyr generelle grensesnitt for logging og caching. Pakkene er en del av den nye ASP.NET Core-stacken, men fungerer fint med .NET 4.6. 

For faktisk implementasjon av disse grensesnittene så kan man enten bruke noe som allerede er laget, eller benytte en egen implementasjon.
