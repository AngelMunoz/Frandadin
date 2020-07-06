namespace Frandadin.Client

module Services =
    open Bolero.Remoting
    open Types

    type RecipeService =
        {
            find: PaginationParams -> Async<Result<PaginationResult<Recipe>, ErrorResponse>>
            findOne: int -> Async<Result<Recipe, ErrorResponse>>
            create: {| title: string 
                       imageUrl: Option<string>
                       description: Option<string>
                       notes: Option<string>
                       ingredients: list<Ingredient>
                       steps: list<RecipeStep> |} -> Async<Result<Recipe, ErrorResponse>>
            update: Recipe -> Async<Result<bool, ErrorResponse>>
            destroy: int -> Async<Result<bool, ErrorResponse>>
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
