[<AutoOpen>]
module AIProvider.Services.Domain.Culture

open Infrastructure.Domain

type Placeholder =
    | Placeholder of (char * char)

    member this.Values =
        match this with
        | Placeholder(left, right) -> left, right

    static member create item = Placeholder(item, item)

type RequestItem = { Value: string }

type Request =
    { Culture: Culture
      Placeholder: Placeholder
      Items: RequestItem seq }

type ResponseItem =
    { Value: string; Result: string option }

type Response =
    { Placeholder: Placeholder
      Items: ResponseItem list }
