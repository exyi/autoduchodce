# AutoDůchodce

Systémek, který stáhne z kupi.cz aktuální slevy, prožene je nadefinovanými pravidly a pošle emailem report co je kde za zajímavé ceny.


## Konfigurace

Autodůchodce se konfiguruje oeditováním souboru `Config.fs` (ano je to zdroják, ale nechtělo se mi vymýšlet nějaké mapování do JSONu), přesný formát je popsaný níže.

V principu se tam napíše seznam "triggerů", které říkají čím se vyznačuje zajímavá nabidka.
* Jedna možnost je určit zboží a výhodnout jednotkovou cenu.
    - Když botík najde nabídku daného zboží za zadanou cenu (nebo nižší), tak jej přidá do seznamu.
    - Tato možnost je výhodná na trvanlivé věci.
* Druhá možnost je určit zboží, jeho normální cenu typický objem ve kterém bych jej koupil
    - Pak je nabídka zahrnuta do reportu jen když se v jednom obchodě dá nakoupit se slevou větší než zadaný limit (`DealLimit`)
    - Toto se hodí na netrvanlivé věci, které se nadají nakoupit ve velkém.

Definice zboží je taky trochu netriviální, protože na kupi.cz není vždy snadné vybrat prostě "surové kakao", když tam na to není kategorie. Je možné zboží zadat jako seznam zdrojů, což jsou prakticky stránky na kupi.cz a pak se dají dále filtrovat regulárními výrazů (podle názvu).

Formát vypadá následovně. Toto seznam konfigurací (je možné najednou spustit několik definic pro různé lidi nebo tak)

```F#
let configs = [
    {
        Settings.Recipients = [ "autoduchodce@mailinator.com" ] // https://www.mailinator.com/v3/index.jsp?zone=public&query=autoduchodce
        SaveFile = "laststuff.json" // soubor kam se ukládá poslední stav
        DealLimit = 100.0 // limit na kombinovanou slevu (v korunách)
        Defs = [
            ... // definice triggerů
        ]
    }
]
```

Trigger na cenu vypadá asi takto:

```F#
Defs = [
    KupiPriceTrigger (68.0, { Sources = [ Page "sleva/sirup-yo" ]; Filters = [] }) // https://www.kupi.cz/sleva/sirup-yo za méně než 68.0Kč za litr
]
```

Trigger na celkovou výší slevy může vypadat takto:

```F#
Defs = [
    KupiAggregatedDealTrigger (3.5, 15.0, { Sources = [ Search "vejce"]; Filters = [] }) // "normálně" kupuju 15 vajec za 3.5 koruny, kolik na tom ušetřím?
    KupiAggregatedDealTrigger (15., 10., { Sources = [ Category "medy" ]; Filters = [] }) // "normálně" kupuju 10x100g medu za 150 korun
]
```

Definice zboží už byla vidět výše, ale je to objekt se seznamem zdrojů (`Sources`) a filtrů (`Filters`). V `Sources` může být:
* `Search "vyhledávaný text"` - V podstatě `https://www.kupi.cz/hledej?f=vyhledávaný text`
* `Category "název-kategorie"` - V podstatě kategorie `https://www.kupi.cz/slevy/název-kategorie`
* `Page "sleva/med-kvetovy-pastovany-medokomerc"` - přesná adresa na `https://kupi.cz`

Je možné mít zdrojů víc, v F# se oddělují novým řádkem nebo středníkem.

Filtry se koukají regulárním výrazem na název výrobku a jsou dvou typů:
* `IncludeByRegex "regex, který musí ta věc splňovat"`
* `ExcludeByRegex "regex, který nesmí splňovat"`

Protože je konfigurace F# kód, je samozřejmě možné konfiguraci kdejak vygenerovat nebo třeba jenom používat proměnné:

```F#
let musli = { Sources = [ Page "sleva/musli-mysli-na-zdravi-emco"; Page "sleva/musli-zapekane-bonavita"; Page "sleva/musli-bonavita" ]; Filters = [] }

let configs = [
    {
        Settings.Recipients = [ "autoduchodce@mailinator.com" ]
        SaveFile = "laststuff.json"
        DealLimit = 100.0
        Defs = [
            KupiPriceTrigger (7.60, musli)
        ]
    }
]
```

## Spouštění

Program se spustí pomocí příkazu `dotnet run -- smtp-server:port username password` (vyžaduje nainstalované dotnet SDK).
