[<AutoOpen>]
module AIProvider.Services.Culture.Domain.Request

open Infrastructure.Domain

type Request =
    { Culture: Culture
      Items: CultureItem seq }