module Model


type KupiItemSource =
    | Search of string
    | Category of string
    | Page of string

type ItemTextFilter =
    | IncludeByRegex of string
    | ExcludeByRegex of string

type KupiSearchDefinition = {
    Sources: KupiItemSource list
    Filters: ItemTextFilter list
}

type PollDefinition =
    | KupiPriceTrigger of price: double * KupiSearchDefinition
    | KupiAggregatedDealTrigger of mehPrice: double * amount: double * KupiSearchDefinition

type Settings = {
    SaveFile: string
    DealLimit: float
    Recipients: string list
    Defs: PollDefinition list
}

type OfferedItem = {
    Title: string
    Id: string
    DetailLink: string
    Size: string
    Price: float
    UnitPrice: float
    OfferedBy: string
    ValidRange: string
    Note: Option<string>
}


type PointOfInterest =
    | CheapShit of OfferedItem list
    | BucketOfDeals of value: double * offeredBy: string * (double * OfferedItem) list
