[<AutoOpen>]
module Multilang.Domain.Translation

type Item =
    { Id: string
      Value: string }
    
type Request =
    { Culture: Culture
      Items: Item seq }

type Response =
    { Items: Item list }
