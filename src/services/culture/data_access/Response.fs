[<AutoOpen>]
module AIProvider.Services.Culture.DataAccess.Response

open AIProvider.Services.Culture.DataAccess.CultureItem

type ResponseEntity () =
    member val Items = Array.empty<CultureItemEntity> with get, set