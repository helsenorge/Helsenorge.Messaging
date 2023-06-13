REM 1. Download latest WSDL from https://ws-web.test.nhn.no/v1/Business?singleWsdl
REM 2. Open VS CLI: Visual Studio > Tools > Command Line > Developer Command Prompt
REM 3. Navigate to folder: cd "src\Helsenorge.Registries\Connected Services\CPAService"
REM 4. Execute	GenerateClientServicebusManangerServiceV2.cmd 

svcutil "servicebusmanager_v2_singleWsdl.wsdl" /out:"ServiceBusManagerServiceV2.cs" /namespace:*,"Helsenorge.Registries"

