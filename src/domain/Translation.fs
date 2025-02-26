[<AutoOpen>]
module Multilang.Domain.Translation

type Item =
    { Id: string
      Value: string }
    
type Request =
    { Culture: Culture
      Items: Item list }

type Response =
    { Items: Item list }
