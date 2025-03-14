[<RequireQualifiedAccess>]
module AIProvider.Services.Culture.Service

open AIProvider.Services.Culture.DataAccess
open AIProvider.Services.Culture.Domain

let tryGetFromStorage (request: Request) (storage: CultureItemStorage)=
    let items = request.Items |> Seq.map (fun item -> { item with Value = item.Value.ToUpper() }) |> Seq.toList
    { Items = items }
    
let translate request ct =
    function
    | AIProvider.Client.Provider.OpenAI client -> client |> OpenAI.Service.translate request ct
