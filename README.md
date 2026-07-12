# BCG HUB

BCG HUB je interní systém pro koordinaci obchodu, backoffice, skladů, dopravy a vývozních dokumentů v rámci jedné zakázky Bohemi Crystall Glass.

## Architektura

- `BcgHub.Api` — .NET 10 Web API, Entity Framework Core a PostgreSQL.
- `BcgHub.UI` — React 19, TypeScript, Vite a Lucide ikony.
- `BcgHub.Api.Tests` — unit testy doménových invariantů a aplikačních služeb.
- `BcgHub.slnx` — solution nového projektu.

## Implementovaný základ

- Doména zákazníků, leadů, dodavatelů, skladů, dopravců, celních deklarantů a spolupracujících osob.
- Kontaktní osoby a společná historie komunikace s možností vazby na konkrétní zakázku.
- Zakázka s vazbou na zákazníka, sklad, dopravce a celního deklaranta, termíny, hodnotou, hmotností a objemem.
- Checklist všech 15 popsaných kroků od založení objednávky v Pohodě po přijetí potvrzených vývozních dokladů.
- Nabídky dopravy a označení vybrané nabídky.
- Obecný model komentářů a příloh použitelný pro všechny entity.
- API pro seznam a detail zakázek, změny stavu workflow a seznamy partnerů.
- UI s ikonovým railem a dvoupanelovým rozložením seznam/detail.
- Vyhledávání a řazení seznamu zakázek a vyhledávání/řazení partnerů.
- Přihlášení uživatele a uživatelské menu s nastavením schránky a odhlášením.
- Ruční IMAP synchronizace e-mailů přihlášeného uživatele, automatické rozpoznání vazeb a ruční přiřazení k partnerům a zakázkám.

## Lokální spuštění

1. Spusťte PostgreSQL a vytvořte databázi `bcg_hub`.
2. Nastavte konfiguraci pomocí user-secrets nebo environment variables. Hesla nejsou součástí `appsettings.json`:

```powershell
$env:ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=bcg_hub;Username=bcg_hub;Password=...'
$env:BootstrapAdmin__Email='admin@example.com'
$env:BootstrapAdmin__FullName='Administrátor'
$env:BootstrapAdmin__Password='alespon-12-znaku'
```

Pro produkci nastavte také `AllowedHosts`, `Cors__Origins__0` a trvalou cestu `DataProtection__KeysPath` sdílenou všemi instancemi API.
3. Spusťte API:

```powershell
dotnet run --project BcgHub.Api/BcgHub.Api.csproj
```

4. Nainstalujte a spusťte UI:

```powershell
cd BcgHub.UI
pnpm install
pnpm dev
```

API při startu aplikuje EF migrace a volitelně vytvoří bootstrap účet. Přihlašovací cookie je `HttpOnly` a změnové endpointy vyžadují antiforgery token. Heslo k IMAP schránce se ukládá šifrovaně pomocí ASP.NET Data Protection.

Frontend ve všech prostředích komunikuje výhradně se serverovou API na `https://dev.radixal.net/bcg-hub/api`.

## Ověření

```powershell
dotnet build BcgHub.slnx
dotnet test BcgHub.slnx
cd BcgHub.UI
pnpm install --frozen-lockfile
pnpm run build
```

## Navazující etapy

- Pravidelná synchronizace e-mailové schránky na pozadí.
- CRUD formuláře všech entit, komentářů a příloh v UI.
- Integrace Pohody pro objednávky, faktury a vyskladnění.
- Úložiště dokumentů, auditní historie a oprávnění.
- Notifikace, termíny a automatizace jednotlivých kroků zakázky.

Původní adresáře `RadixalATS.*` jsou dočasně ponechaná výchozí šablona. Po schválení jejich odstranění nemají být součástí výsledného BCG HUB repozitáře.
