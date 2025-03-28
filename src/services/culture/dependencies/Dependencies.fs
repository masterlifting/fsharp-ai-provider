[<RequireQualifiedAccess>]
module AIProvider.Services.Dependencies.Culture

open Infrastructure.Domain
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess

type Dependencies = {
    Provider: AIProvider.Client.Provider
    Storage: Culture.Response.Storage
}
