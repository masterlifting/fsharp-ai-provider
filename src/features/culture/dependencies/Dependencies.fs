[<RequireQualifiedAccess>]
module AIProvider.Features.Dependencies.Culture

open Infrastructure.Domain
open AIProvider.Features.Domain
open AIProvider.Features.DataAccess

type Dependencies = {
    Provider: AIProvider.Client.Provider
    Storage: Culture.Storage
}
