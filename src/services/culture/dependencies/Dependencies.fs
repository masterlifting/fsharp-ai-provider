[<RequireQualifiedAccess>]
module AIProvider.Services.Dependencies.Culture

open AIProvider.Services.Domain
open AIProvider.Services.DataAccess

type Dependencies =
    { Provider: AIProvider.Client.Provider
      Storage: Culture.Response.Storage
      Placeholder: Culture.Placeholder }
