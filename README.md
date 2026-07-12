# BCG HUB

BCG HUB je interní systém pro koordinaci obchodu, backoffice, skladů, dopravy a vývozních dokumentů v rámci jedné zakázky Bohemi Crystall Glass.

## Architektura

- `BcgHub.Api` — .NET 10 Web API, Entity Framework Core a PostgreSQL.
- `BcgHub.UI` — React 19, TypeScript, Vite a Lucide ikony.
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

## Lokální spuštění

1. Spusťte PostgreSQL a vytvořte databázi `bcg_hub`.
2. Upravte `ConnectionStrings:DefaultConnection` podle lokálního PostgreSQL.
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

V development režimu API vytvoří schéma a vloží jednu ukázkovou zakázku. Pro nasazení se používají EF migrace.

## Navazující etapy

- Přihlášení uživatele a synchronizace jeho e-mailové schránky.
- Automatické i ruční přiřazování e-mailů k zákazníkům a zakázkám.
- CRUD formuláře všech entit, komentářů a příloh v UI.
- Integrace Pohody pro objednávky, faktury a vyskladnění.
- Úložiště dokumentů, auditní historie a oprávnění.
- Notifikace, termíny a automatizace jednotlivých kroků zakázky.

Původní adresáře `RadixalATS.*` jsou dočasně ponechaná výchozí šablona. Po schválení jejich odstranění nemají být součástí výsledného BCG HUB repozitáře.
