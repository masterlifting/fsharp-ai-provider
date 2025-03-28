[<AutoOpen>]
module AIProvider.Services.Domain.Culture

open Infrastructure.Domain

type Shield =
    | Shield of (char * char)

    member this.Values =
        match this with
        | Shield(left, right) -> left, right

    static member create left right = Shield(left, right)

type RequestItem = { Value: string }

type Request = {
    Culture: Culture
    Shield: Shield
    Items: RequestItem seq
}

type ResponseItem = { Value: string; Result: string option }

type Response = {
    Shield: Shield
    Items: ResponseItem list
}
