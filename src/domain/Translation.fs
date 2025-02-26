[<RequireQualifiedAccess>]
module Multilang.Domain.Translation

type Item =
    { Id: string
      Value: string }
    
type Input =
    { Culture: Culture
      Items: Item list }
