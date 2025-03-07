[<AutoOpen>]
module Multilang.OpenAI.Domain

open System
open System.Collections.Concurrent

type Client = String
type ClientFactory = ConcurrentDictionary<string, Client>

type Connection = { Token: string }
