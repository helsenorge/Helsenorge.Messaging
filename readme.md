# Helsenorge Messaging

Ett av integrasjonalternativene mot Helsenorge er å sende XML meldinger via et sett med køer. Misjonen til denne pakken er å lette dette arbeidet ved å dra nytte av eksisterende kode og erfaringer.

I dag så støtter pakken bare AMQP protokollen, men på sikt så kan andre protokoller som SOAP og SMTP bli støttet.   

Meldingene som sendes sikres gjennom krypering signering. System støtter både funksjonell asynkron og synkron meldingsutveksling.



1. [Forutsetninger](Documentation/Forutsetninger.md "Forutsetninger")
2. [Registre](Documentation/Registre.md "Registere")
3. [CPP og CPA](Documentation/CPPA.md "CPP og CPA")
4. [Sending av meldinger](Documentation/SendeMeldinger.md "Sending av meldinger")
5. [Mottak av meldinger](Documentation/MottaMeldinger.md "Mottak av meldinger")
6. [Feilhåndtering og logging](Documentation/FeilOgLogging.md "Feilhåndtering og logging")
7. [Kryptering og Signering](Documentation/KrypteringOgSignering.md "Kryptering og Signering")
8. [Konfigurasjon](Documentation/Konfigurasjon.md "Konfigurasjon")
9. [Nuget](Documentation/Nuget.md "Nuget")
10. [Referanseeksempler](Documentation/ReferanseEksempler.md "Referanseeksempler")
11. [Meldingsutveksling over HTTP](Documentation/HTTP.md "Meldingsutveksling over HTTP")