# Snapr

Een kleine screenshot-tool voor Windows, geïnspireerd op [Shottr](https://shottr.cc/). Geen gedoe: druk op een sneltoets, kies een gebied, zet er een pijl op of maak tekst onleesbaar, en plak het waar je wilt.

## Functies

- **Sneltoets**: standaard `PrintScreen`, instelbaar via het tray-icoon › *Instellingen…*
- **Gebied kiezen**: sleep een rechthoek, klik voor het hele scherm onder de muis, `Enter` voor alle schermen, `Esc` om te annuleren.
- **Pijlen** (`A`) in vijf kleuren (`1`–`5`).
- **Pixeleren** (`P`): sleep over tekst om die onleesbaar te maken. Elk blok krijgt een beetje willekeurige ruis, zodat de tekst ook niet met tools als Depix terug te rekenen is.
- **Nummers** (`N`): elke klik plaatst het volgende nummer (1, 2, 3, …) in een gekleurd afgerond vierkantje; slepen om de plek te verfijnen.
- Ongedaan maken / opnieuw (`Ctrl+Z` / `Ctrl+Y`), kopiëren (`Ctrl+C`), opslaan als PNG (`Ctrl+S`), sluiten (`Esc`).
- De screenshot staat direct op het klembord; sluit je de editor na bewerken, dan staat de bewerkte versie op het klembord.
- Optioneel automatisch starten met Windows.
- **Taal**: Engels, of Nederlands als de weergavetaal van Windows Nederlands is.

## Downloaden

De nieuwste versie staat onder *Releases* als losse `Snapr.exe` (self-contained, geen .NET-installatie nodig). De exe is niet ondertekend, dus Windows SmartScreen waarschuwt de eerste keer: kies *Meer info › Toch uitvoeren*.

Een release maken: push een versietag, bijvoorbeeld `git tag v0.2.0 && git push origin v0.2.0`. De workflow bouwt de exe met dat versienummer en zet hem bij de release.

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
