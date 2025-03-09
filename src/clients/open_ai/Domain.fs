[<AutoOpen>]
module AIProvider.OpenAI.Domain

open System
open System.Collections.Concurrent

type Client = Web.Http.Domain.Client.Client
type ClientFactory = ConcurrentDictionary<string, Client>

type Connection = { Token: string }

type Model =
    | Gpt4o
    | Gpt4oMini
    | Gpt4_5Preview

    member this.Name =
        match this with
        | Gpt4o -> "gpt-4o"
        | Gpt4oMini -> "gpt-4o-mini"
        | Gpt4_5Preview -> "gpt-4.5-preview"

type Content =
    { System: string
      User: string
      Assistant: string }

type Request =
    { Model: Model
      Store: bool
      Content: Content }

type Response = { Role: string; Content: string }
