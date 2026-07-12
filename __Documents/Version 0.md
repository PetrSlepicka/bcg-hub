# BCG HUB — Version 0

## Cíl

Version 0 dokončuje všechny základní ručně ovládané části aplikace. Uživatel musí být schopen evidovat a spravovat celý základ zakázky bez přímých zásahů do databáze. Pokročilé automatizace zůstávají v navazujících verzích.

## Rozsah Version 0

- Kompletní CRUD zákazníků, leadů, dodavatelů, skladů, dopravců, celních deklarantů a spolupracujících osob.
- Kompletní CRUD kontaktních osob partnera včetně primárního kontaktu.
- Kompletní CRUD zakázek včetně zákazníka, kontaktu, skladu, dopravce, deklaranta, termínů, hodnoty, hmotnosti, objemu a pokynů skladu.
- Použitelná správa všech 15 kroků workflow zakázky.
- Kompletní CRUD nabídek dopravy včetně výběru jediné vítězné nabídky.
- Evidence obecné komunikace typu e-mail, telefonát, schůzka a poznámka u partnera nebo zakázky.
- Komentáře ke všem podporovaným entitám.
- Nahrávání, zobrazení, stažení a odstranění souborových příloh.
- Stahování a ukládání příloh z importovaných e-mailů.
- Pravidelná synchronizace aktivních e-mailových schránek na pozadí vedle ruční synchronizace.
- Vyhledávání, řazení a stránkování všech seznamových obrazovek.
- Bezpečné přihlášení, autorizace, CSRF ochrana, auditovatelná identita autora a ochrana souběžných změn.

## Mimo Version 0 — navazující pokročilé funkce

- Integrace s programem Pohoda pro automatický import objednávek, fakturaci a vyskladnění.
- Automatické odesílání e-mailů zákazníkům, skladům, dopravcům a deklarantům.
- Generování CMR, VDD a dalších vývozních dokumentů.
- Automatické poptávání dopravy, AI vytěžování nabídek a doporučování dopravce.
- OCR a AI klasifikace dokumentů a zpráv.
- Externí potvrzovací portál pro sklady a další partnery.
- Plnohodnotný workflow engine, notifikace a práce pouze podle výjimek.

## Akceptační stav

Version 0 je hotová, když každá položka v rozsahu má databázový model, autorizované API, funkční UI a minimální automatické testy; všechna řešení se sestaví bez varování a databázová změna je pokryta EF migrací.
