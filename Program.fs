open System
open Model
open EmailClient

let private pollStuff dealLimit (stuff: PollDefinition list) =
    let triggers =
        stuff
        |> Seq.choose(
            function
            | PollDefinition.KupiPriceTrigger (p, sd) ->
                let offers = KupiClient.getItems sd |> Seq.filter (fun i -> i.UnitPrice <= p) |> Seq.sortByDescending (fun i -> i.UnitPrice) |> Seq.toList
                if offers.Length > 0 then
                    Some (PointOfInterest.CheapShit offers)
                else None
            | _ -> None
        )

    let deals =
        stuff
        |> Seq.choose(
            function
            | PollDefinition.KupiAggregatedDealTrigger (p, amm, sd) ->
                KupiClient.getItems sd
                |> Seq.choose (fun i ->
                    let value = (p - i.UnitPrice) * amm
                    if value <= 0.0 then None
                    else Some (value, i)
                )
                |> Seq.sortByDescending fst
                |> Seq.tryHead
            | _ -> None
        )
        |> Seq.groupBy (fun (_, offer) -> offer.OfferedBy)
        |> Seq.choose (fun (offeredBy, deals) ->
            let value = deals |> Seq.sumBy fst
            if value <= dealLimit then
                None
            else
                Some (PointOfInterest.BucketOfDeals (value, offeredBy, Seq.toList deals))
        )

    Seq.append triggers deals |> Seq.toList

let formatEmail (stuff: PointOfInterest list) =

    let formatOffer (offer: OfferedItem) =
        sprintf "%s v %s za %.2f Kč / %s = %.2f Kč (%s %s %s)" offer.Title offer.OfferedBy offer.Price offer.Size offer.UnitPrice (offer.Note |> Option.defaultValue "") offer.DetailLink offer.ValidRange

    let formatItem =
        function
        | PointOfInterest.CheapShit offers ->
            sprintf "Levná nabídka:%s" (offers |> Seq.map formatOffer |> Seq.map (sprintf "\n * %s") |> String.Concat)
        | PointOfInterest.BucketOfDeals (value, offeredBy, offers) ->
            sprintf "Slevy v hodnotě ~%.2f Kč v %s:%s" value offeredBy (offers |> Seq.map (fun (value, offer) -> sprintf "\n * %.2f za %s" value (formatOffer offer)) |> String.Concat)

    sprintf "Nové zajímavé věci na Kupi.cz:\n\n%s" (String.Join("\n\n", Seq.map formatItem stuff))

let execute (config: Settings) emailConfig =
    let saveFile = config.SaveFile

    if not (IO.File.Exists saveFile) then
        IO.File.WriteAllText(saveFile, "[]")
    let stuff = pollStuff config.DealLimit config.Defs
    let lastStuff = Newtonsoft.Json.JsonConvert.DeserializeObject<PointOfInterest list>(IO.File.ReadAllText saveFile)
    let newStuff = List.except lastStuff stuff

    printfn "new stuff: %A" newStuff

    if newStuff.Length > 0 then
        let body = formatEmail newStuff
        EmailClient.sendEmail emailConfig "Nové nesmysly na Kupi.cz" body config.Recipients
        IO.File.WriteAllText(saveFile, Newtonsoft.Json.JsonConvert.SerializeObject stuff)

[<EntryPoint>]
let main argv =

    let emailConfig =
        let [|server; port|] = argv.[0].Split(':')
        { EmailConfig.SmtpServer = server; SmtpPort = Int32.Parse port; Username = argv.[1]; Password = argv.[2] }

    for c in Config.configs do
        execute c emailConfig

    0
