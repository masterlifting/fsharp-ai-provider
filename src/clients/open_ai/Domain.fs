module AIProvider.Clients.Domain.OpenAI

open System
open System.Collections.Concurrent
open System.Text.Json
open Web.Clients.Domain

type Client = Http.Client
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
