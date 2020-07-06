namespace Frandadin.Server

open System
open System.Security.Claims
open Microsoft.AspNetCore.Hosting
open Bolero
open Bolero.Remoting
open Bolero.Remoting.Server
open AccidentalFish.FSharp.Validation
open Frandadin
open Frandadin.Client.Types
open Frandadin.Client.Validators

module private Recipes = 
    open Npgsql
    open Npgsql.FSharp
    open Database

    let find (pagination: PaginationParams) (userId: int) : Async<Result<PaginationResult<Recipe>, exn>> =
        failwith "Not Implemented"


    let findRecipe (id: int) (withSubs: bool) : Async<Result<Recipe, exn>> = 
        failwith "Not Implemented"

    let createRecipe 
        (preRecipe: {| title: string 
                       imageUrl: Option<string>
                       description: Option<string>
                       notes: Option<string>
                       ingredients: list<Ingredient>
                       steps: list<RecipeStep> |})
        (userId: int)
        : Async<Result<Recipe, exn>> =
        async {
            let! result =
                defaultConnection
                |> Sql.connect
                |> Sql.query 
                    @"INSERT INTO recipes(userId, title, imageUrl, description, notes)
                      VALUES(@userId, @title, @imageUrl, @description, @notes) RETURNING id"
                |> Sql.parameters
                    [ "userId", Sql.int userId
                      "title", Sql.string preRecipe.title
                      "imageUrl", Sql.stringOrNone preRecipe.imageUrl
                      "description", Sql.stringOrNone preRecipe.description
                      "notes", Sql.stringOrNone preRecipe.notes
                    ]
                |> Sql.executeAsync(fun read -> read.int "id")
            
            match result with 
            | Result.Ok rows ->
                let! subInserts = 
                    defaultConnection
                    |> Sql.connect
                    |> Sql.executeTransactionAsync
                        [ @"INSERT INTO ingredients(recipeId, name, quantity) VALUES (@recipeId, @name, @quantity)",
                            preRecipe.ingredients 
                            |> List.map(fun i -> 
                                [ "recipeId", Sql.int rows.Head
                                  "name", Sql.string i.name
                                  "quantity", Sql.string i.quantity ] )
                            @"INSERT INTO recipestep(recipeId, stepOrder, imageUrl, directions) VALUES (@recipeId, @stepOrder, @imageUrl, @directions)",
                            preRecipe.steps
                            |> List.map(fun s ->
                                [ "recipeId", Sql.int rows.Head
                                  "stepOrder", Sql.int s.order
                                  "imageUrl", Sql.stringOrNone s.imageUrl
                                  "directions", Sql.string s.directions ] )
                        ]
                match subInserts with 
                | Result.Ok -> return! findRecipe rows.Head true
                | Error ex -> return Error ex
            | Error ex ->
                return Error ex
        }

    let update (recipe: Recipe) : Async<Result<bool, exn>> = 
        failwith "Not Implemented"

    let delete (recipeId: int) : Async<Result<bool, exn>> = 
        failwith "Not Implemented"

    let extractUserId (ctx: IRemoteContext) : int =
        let userNameItendifierClaim = 
            ctx.HttpContext.User.Claims |> Seq.find (fun c -> c.Type = ClaimTypes.NameIdentifier)
        userNameItendifierClaim.Value |> int

type RecipeService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<Client.Services.RecipeService>()

    override __.Handler = 
        { find = 
            ctx.Authorize 
            <| fun pagination ->
                async {
                    let user = Recipes.extractUserId ctx
                    let! result = Recipes.find pagination user
                    match result with 
                    | Result.Ok result ->
                        return Result.Ok result
                    | Error ex ->
                        eprintfn "%O" ex
                        ctx.HttpContext.Response.StatusCode <- 422
                        return Error { message = "Unexpected error in [find:recipes]"
                                       code= Some 422
                                       errors = List.empty }
                }

          findOne = 
            ctx.Authorize 
            <| fun recipeId -> 
                async {
                    let! recipe = Recipes.findRecipe recipeId true
                    match recipe with 
                    | Result.Ok recipe ->
                        let userId = Recipes.extractUserId ctx
                        if recipe.userid <> userId then 
                            ctx.HttpContext.Response.StatusCode <- 403
                            return Error { message = "You don't have access to this recipe"
                                           code = Some 403
                                           errors = List.empty }
                        else 
                            return Result.Ok recipe
                    | Error ex ->
                        eprintfn "%O" ex
                        ctx.HttpContext.Response.StatusCode <- 422
                        return Error { message = "Unexpected error in [findOne:recipes]"
                                       code= Some 422
                                       errors = List.empty }
                }

          create = 
            ctx.Authorize
            <| fun payload -> 
                async {
                    let result = validateAnonymusRecipe payload
                    match result with 
                    | ValidationState.Ok ->
                        let userId = Recipes.extractUserId ctx
                        let payload = 
                            {| title = payload.title
                               imageUrl = payload.imageUrl
                               description = payload.description
                               notes = payload.notes
                               ingredients = payload.ingredients
                               steps = payload.steps |}
                        let! recipe = Recipes.createRecipe payload userId
                        match recipe with 
                        | Result.Ok recipe ->
                            ctx.HttpContext.Response.StatusCode <- 201
                            return Result.Ok recipe
                        | Error ex ->
                            eprintf "%O" ex
                            ctx.HttpContext.Response.StatusCode <- 422
                            return Error { message = "Failed to create recipe"
                                           code = Some 422
                                           errors = List.empty }
                    | ValidationState.Errors errors ->
                        let error = stringifyErrors errors
                        let list = objectifyList errors
                        ctx.HttpContext.Response.StatusCode <- 400
                        return Error { message = error
                                       code = Some 400
                                       errors = list }
                }

          update = 
              ctx.Authorize
              <| fun recipe -> 
                  async {
                      let userId = Recipes.extractUserId ctx
                      if recipe.userid <> userId then 
                          ctx.HttpContext.Response.StatusCode <- 403
                          return Error { message = "You don't have access to this recipe"
                                         code = Some 403
                                         errors = List.empty }
                      else 
                          let! result = Recipes.update recipe
                          match result with 
                          | Result.Ok result ->
                              match result with 
                              | true -> return Result.Ok result
                              | false -> 
                                  return Error { message = sprintf "Failed to update recipe %i" recipe.id
                                                 code = Some 422
                                                 errors = List.empty }
                          | Error ex ->
                              eprintfn "%O" ex
                              ctx.HttpContext.Response.StatusCode <- 422
                              return Error { message = "Unexpected Error in [update:recipes]"
                                             code= Some 422
                                             errors = List.empty }
                  }

          destroy = 
            ctx.Authorize
            <| fun recipeId -> 
                async {
                    let userId = Recipes.extractUserId ctx
                    let! result = 
                        Recipes.findRecipe recipeId false
                    match result with 
                    | Result.Ok recipe ->
                        if recipe.userid <> userId then 
                            ctx.HttpContext.Response.StatusCode <- 403
                            return Error { message = "You don't have access to this recipe"
                                           code = Some 403
                                           errors = List.empty }
                        else 
                            let! result = Recipes.delete recipe.id
                            match result with 
                            | Result.Ok result ->
                                match result with 
                                | true -> return Result.Ok result
                                | false -> 
                                    return Error { message = sprintf "Failed to update recipe %i" recipe.id
                                                   code = Some 422
                                                   errors = List.empty }
                            | Error ex ->
                                eprintfn "%O" ex
                                ctx.HttpContext.Response.StatusCode <- 422
                                return Error { message = "Unexpected Error in [delete:recipes]"
                                               code= Some 422
                                               errors = List.empty }
                    | Error ex -> 
                        eprintfn "%O" ex
                        ctx.HttpContext.Response.StatusCode <- 422
                        return Error { message = "Unexpected Error in [delete:findRecipe:recipes]"
                                       code= Some 422
                                       errors = List.empty }
                }
        }
