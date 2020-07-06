namespace Frandadin.Client

module Services =
    open Bolero.Remoting
    open Types

    type RecipeService =
        {
            find: PaginationParams -> Async<PaginationResult<Recipe>>
            findOne: int -> Async<Recipe>
            create: {| title: string 
                       imageUrl: Option<ImageUrl>
                       description: Option<string>
                       notes: Option<string>
                       ingredients: list<Ingredient>
                       steps: list<RecipeStep> |} -> Async<Recipe>
            update: Recipe -> Async<bool>
            destroy: int -> Async<bool>
        }

        interface IRemoteService with 
            member __.BasePath = "/api/recipes"

    type AuthService = 
        {
            getUser: unit -> Async<string>
            logout: unit -> Async<unit>
            login: LoginPayload -> Async<Result<AuthResponse, ErrorResponse>>
            signup: SignUpPayload -> Async<Result<AuthResponse, ErrorResponse>>
        }

        interface IRemoteService with 
            member __.BasePath = "/api/auth"
