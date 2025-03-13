[<AutoOpen>]
module AIProvider.Services.Culture.Domain

open Infrastructure.Domain

type RequestItem = { Id: string; Value: string }

type Request =
    { Culture: Culture
      Items: RequestItem seq }

type Response = { Items: RequestItem list }
