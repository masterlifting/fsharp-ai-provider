[<AutoOpen>]
module AIProvider.Services.Domain.Culture

open Infrastructure.Domain

type RequestItem = { Id: string; Value: string }

type Request =
    { Culture: Culture
      Items: RequestItem seq }

type ResponseItem =
    { Id: string
      Value: string
      Result: string option }

    member this.ToRequestItem() = { Id = this.Id; Value = this.Value }

type Response = { Items: ResponseItem list }