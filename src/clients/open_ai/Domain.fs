[<AutoOpen>]
module AIProvider.Domain.OpenAI

open System
open System.Collections.Concurrent
open System.Text.Json

type Client = Web.Http.Domain.Client.Client
type ClientFactory = ConcurrentDictionary<string, Client>

let internal jsonOptions =
    JsonSerializerOptions(PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

type Connection = { Token: string; ProjectId: string }

type Model =
    | Gpt3_5Turbo

    member this.Name =
        match this with
        | Gpt3_5Turbo -> "gpt-3.5-turbo"

type Message = { Role: string; Content: string }

type Request =
    { Model: Model
      Store: bool
      Messages: Message list }

type Response = { Messages: Message list }
