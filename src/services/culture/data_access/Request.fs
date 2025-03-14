[<AutoOpen>]
module AIProvider.Services.Culture.DataAccess.Request

open System
open AIProvider.Services.Culture.DataAccess.CultureItem

type RequestEntity () =
    member val Culture = String.Empty with get, set
    member val Items = Array.empty<CultureItemEntity> with get, set