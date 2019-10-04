module KupiClient
open Model
open AngleSharp
open System

let private getUrl =
    function
    | KupiItemSource.Search query ->
        sprintf "https://www.kupi.cz/hledej?f=%s" (System.Uri.EscapeDataString query)
    | KupiItemSource.Category c ->
        sprintf "https://www.kupi.cz/slevy/%s?ord=0" c
    | KupiItemSource.Page c ->
        sprintf "https://www.kupi.cz/%s" c

let getDocument (url: string) =
    let config = Configuration.Default.WithDefaultLoader();
    let context = BrowsingContext.New config;
    context.OpenAsync(url).Result;

let parsePrice (str: string) =
    str.Trim().Replace("Kč", "").Replace(",", ".").Trim() |> float

let parseValidityRange (str: string) =
    // (DateTime.Today, DateTime.Today.AddDays 1.)
    str.Trim()


let getItemsFromSource (def: KupiItemSource) =
    let url = def |> getUrl
    use doc = url |> getDocument

    doc.QuerySelectorAll ".group_discounts"
    |> Seq.collect (fun item ->
        let linkElement = item.QuerySelector(".product_name a.product_link_history") :?> Html.Dom.IHtmlAnchorElement
        let (title, detailLink) =
            if isNull linkElement then
                // we're on the detail page
                (item.QuerySelector(".product_detail_headline").TextContent.Trim(), url)
            else
                // on search/category page
                (linkElement.Title, linkElement.Href)
        item.QuerySelectorAll ".discounts_table tr.discount_row"
        |> Seq.filter (fun o -> o.QuerySelector ".discounts_price .discount_amount.left" |> isNull |> not)
        |> Seq.map (fun offer ->
            let price = (offer.QuerySelector ".discounts_price div.left strong").TextContent |> parsePrice
            let size = (offer.QuerySelector ".discounts_price .discount_amount.left").TextContent.Trim().TrimStart('/').Trim()
            let unitPrice = (offer.QuerySelector ".discounts_price .discount_price_value").TextContent |> parsePrice
            let offeredBy = offer.QuerySelector(".discounts_shop_name").TextContent.Trim()
            let productID = offer.GetAttribute "data-product"
            let valid = (offer.QuerySelector ".discounts_validity").TextContent |> parseValidityRange
            let note = (offer.QuerySelector ".discount_note") |> Option.ofObj |> Option.map (fun x -> x.TextContent.Trim())

            {
                Title = title
                Id = productID
                DetailLink = detailLink
                Size = size
                Price = price
                UnitPrice = unitPrice
                OfferedBy = offeredBy
                ValidRange = valid
                Note = note
            }
        )
    )
    |> Seq.toArray

let private itemMatches =
    let options = System.Text.RegularExpressions.RegexOptions.IgnoreCase
    function
    | ItemTextFilter.IncludeByRegex pattern -> System.Text.RegularExpressions.Regex(pattern, options).IsMatch
    | ItemTextFilter.ExcludeByRegex pattern -> System.Text.RegularExpressions.Regex(pattern, options).IsMatch >> not

let getItems (def: KupiSearchDefinition) =
    try
        let f = List.map itemMatches def.Filters
        def.Sources
        |> Seq.collect getItemsFromSource
        |> Seq.distinct
        |> Seq.filter (fun i -> f |> List.forall (fun x -> x i.Title))
        |> Seq.toArray
    with ex ->
        eprintfn "Error while loading %A: %O" def ex
        [||]
