[<RequireQualifiedAccess>]
module AIProvider.Services.Culture.Service

open System
open System.Text.RegularExpressions
open AIProvider

let tokenize (value: string) ([<ParamArray>] args: obj[]) =
    Regex.Replace(
        value,
        @"\{(\d+)\}",
        fun m ->
            let index = int m.Groups.[1].Value

            if index < args.Length then
                args.[index].ToString()
            else
                m.Value
    )

let translate request ct =
    function
    | Client.Provider.OpenAI client -> client |> OpenAI.Service.translate request ct
