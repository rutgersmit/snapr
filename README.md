# Snapr

Een kleine screenshot-tool voor Windows, geïnspireerd op [Shottr](https://shottr.cc/). Geen gedoe: druk op een sneltoets, kies een gebied, zet er een pijl op of maak tekst onleesbaar, en plak het waar je wilt.

## Functies

- **Sneltoets**: standaard `PrintScreen`, instelbaar via het tray-icoon › *Instellingen…*
- **Gebied kiezen**: sleep een rechthoek, klik voor het hele scherm onder de muis, `Enter` voor alle schermen, `Esc` om te annuleren.
- **Pijlen** (`A`) in vijf kleuren (`1`–`5`).
- **Markeren** (`H`): een markeerstift die over tekst kleurt; de tekst blijft donker en leesbaar. Een bibberige, ongeveer rechte sleepbeweging wordt één strakke streep, die recht trekt naar horizontaal (of verticaal) als hij daar dichtbij ligt; alleen een duidelijke bocht wordt gevolgd, en dan glad gestreken. De markeerstift onthoudt zijn eigen kleur (eerst geel) en ligt altijd onder pijlen, nummers en gepixelde stukken.
- **Pixeleren** (`P`): sleep over tekst om die onleesbaar te maken. Elk blok krijgt een beetje willekeurige ruis; dat maakt terugrekenen met tools als Depix veel lastiger, maar is geen garantie. Voor echt gevoelige gegevens (wachtwoorden, sleutels) kun je die beter helemaal buiten beeld houden.
- **Nummers** (`N`): elke klik plaatst het volgende nummer (1, 2, 3, …) in een gekleurd afgerond vierkantje; slepen om de plek te verfijnen.
- **Verplaatsen**: klik op een pijl, markering of nummer om hem te selecteren en sleep hem naar een andere plek. Sleep een uiteinde van een geselecteerde pijl of rechte markering om richting en lengte aan te passen. `Delete` verwijdert de selectie, `Esc` deselecteert.
- Ongedaan maken / opnieuw (`Ctrl+Z` / `Ctrl+Y`), kopiëren (`Ctrl+C`), opslaan als PNG (`Ctrl+S`), sluiten (`Esc`).
- De screenshot staat direct op het klembord; sluit je de editor na bewerken, dan staat de bewerkte versie op het klembord.
- Optioneel automatisch starten met Windows.
- **Taal**: Engels, of Nederlands als de weergavetaal van Windows Nederlands is.

## Downloaden

De nieuwste versie staat onder *Releases* als losse `Snapr.exe` (self-contained, geen .NET-installatie nodig). De exe is niet ondertekend, dus Windows SmartScreen waarschuwt de eerste keer: kies *Meer info › Toch uitvoeren*.

Een release maken: maak op GitHub een nieuwe release aan (*Releases › Draft a new release*) met een tag als `0.2` of `v0.2.0` en publiceer hem. De Release-workflow bouwt dan de exe met dat versienummer en hangt `Snapr.exe` er een paar minuten later aan. Voor een bestaande release zonder exe: *Actions › Release › Run workflow* met de tag.

Tussentijdse builds van elke push staan onder *Actions › Build › Artifacts*.

## Zelf bouwen

Vereist de [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet build Snapr.slnx -c Release
dotnet run --project src/Snapr

# Losse exe:
dotnet publish src/Snapr/Snapr.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

## PrintScreen werkt niet?

Windows 11 koppelt `PrtSc` standaard aan het Knipprogramma. Zet dat uit via
*Instellingen › Toegankelijkheid › Toetsenbord › "Knipprogramma openen met de knop Print Screen"*,
of kies in Snapr een andere sneltoets (bijvoorbeeld `Ctrl+Shift+S`).

## Instellingen

Worden opgeslagen in `%AppData%\Snapr\settings.json`.

## Problemen

Onverwachte fouten worden gelogd in `%AppData%\Snapr\snapr.log`; Snapr blijft daarna gewoon draaien.
