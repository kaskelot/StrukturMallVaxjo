# AnpassaStrukturset

ESAPI-script för Varian Eclipse som anpassar en patients strukturset efter en vald strukturmall. Strukturer som inte ingår i mallen tas bort och strukturer som saknas läggs till automatiskt. Utvecklat av Henrik Rudendal, Region Kronoberg baserat på ett originalscript av Sara Tjärnberg, Region Västmanland.

## Krav
- Varian Eclipse med ESAPI v18
- Behörighet att modifiera strukturset i ARIA

## Användning
1. Öppna scriptet i Eclipse Script Wizard
2. Välj önskad strukturmall i listan
3. Kör scriptet – struktursetet uppdateras automatiskt

## Notering
Scriptet tar bort strukturer som inte ingår i vald mall. Kontrollera att rätt mall är vald innan körning. För att få scriptet att fungera måste det godkännas i Script Administration i Eclipse