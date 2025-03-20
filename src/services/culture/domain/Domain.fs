[<AutoOpen>]
module AIProvider.Services.Domain.Culture

open System.Text.RegularExpressions
open Infrastructure.Domain

type RequestItem =
    { Value: string }

    // Input: "Hello, my name is 'John'. I like to 'study'."
    // Output: "Hello, my name is '[0]'. I like to '[1]'.", ["John"; "study"]
    member this.Tokenize() =
        let pattern = "'([^']*)'"
        let matches = Regex.Matches(this.Value, pattern)
        let values = matches |> Seq.cast |> Seq.map _.Value |> Seq.toList
        let result = Regex.Replace(this.Value, pattern, "'[%d]'")
        result, values
        
    static member Restore (value: string) (values: string list) =
        let mutable result = value
        for i in 0 .. values.Length - 1 do
            result <- result.Replace( $"'[%d{i}]'", values[i])
        result

type Request =
    { Culture: Culture
      Items: RequestItem seq }

type ResponseItem =
    { Value: string
      Result: string option }

    member this.ToRequestItem() = { Value = this.Value }

type Response = { Items: ResponseItem list }
