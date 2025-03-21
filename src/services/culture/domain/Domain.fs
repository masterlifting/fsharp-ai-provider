[<AutoOpen>]
module AIProvider.Services.Domain.Culture

open Infrastructure.Domain

type RequestItem =
    { Value: string }

type Request =
    { Culture: Culture
      Items: RequestItem seq }

type ResponseItem =
    { Value: string
      Result: string option }

    member this.ToRequestItem() = { Value = this.Value }

type Response = { Items: ResponseItem list }
