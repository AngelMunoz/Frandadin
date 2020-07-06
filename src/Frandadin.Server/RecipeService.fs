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
        async {
            let offset = pagination.limit * (pagination.page - 1)
            let! countQuery = 
                defaultConnection
                |> Sql.connect
                |> Sql.query @"SELECT COUNT(*) as total FROM recipes WHERE userId = @userid"
                |> Sql.parameters  [ "userid", Sql.int userId ]
                |> Sql.executeRowAsync (fun read -> read.int "total")
            let! listQuery =
                defaultConnection
                |> Sql.connect
                |> Sql.query @"SELECT * FROM recipes WHERE userId = @userid LIMIT @limit OFFSET @offset"
                |> Sql.parameters 
                    [ "userid", Sql.int userId
                      "limit", Sql.int pagination.limit
                      "offset", Sql.int offset 
                    ]
                |> Sql.executeAsync 
                    (fun read -> 
                        { id = read.int "id"
                          userid = read.int "userid"
                          title = read.string "title"
                          imageUrl = read.stringOrNone "imageurl"
                          description = read.stringOrNone "description"
                          notes = read.stringOrNone "notes"
                          ingredients = List.empty
                          steps = List.empty })
            match countQuery, listQuery with 
            | Result.Ok count, Result.Ok list ->
                return Result.Ok { list = list; count = count }
            | exceptions ->
                let (count, query) = exceptions
                let msg = 
                    match count, query with 
                    | Error count, Error query ->
                        count.Message + "|"  + query.Message
                    | Error count, _ ->
                        count.Message
                    | _, Error query ->
                        query.Message
                    | _, _ -> ""
                return Error (exn (sprintf "Either Count or the list query failed \"%s\"" msg))
                
        }


    let private findSubs (recipeId: int) =
        async {
            let queryParams = [ "recipeid" , Sql.int recipeId ]
            let! ingredientsQuery = 
                defaultConnection
                |> Sql.connect
                |> Sql.query @"SELECT * FROM ingredients where recipeId = @recipeid"
                |> Sql.parameters queryParams
                |> Sql.executeAsync
                    (fun read -> 
                        { id = read.int "id"
                          recipeid = read.int "recipeid"
                          name = read.string "name"
                          quantity = read.string "quantity"})
            let! stepsQuery = 
                defaultConnection
                |> Sql.connect
                |> Sql.query @"SELECT * FROM recipestep where recipeId = @recipeid"
                |> Sql.parameters queryParams
                |> Sql.executeAsync 
                    (fun read -> 
                        { id = read.int "id"
                          recipeid = read.int "recipeid"
                          order = read.int "order"
                          directions = read.string "directions"
                          imageUrl = read.stringOrNone "imageurl" })
            return (ingredientsQuery, stepsQuery)
        }

    let findRecipe (id: int) (withSubs: bool) : Async<Result<Recipe, exn>> = 
        async {
            let! recipeQuery =
                defaultConnection
                |> Sql.connect
                |> Sql.query @"SELECT * FROM recipes WHERE id = @id"
                |> Sql.parameters [ "id", Sql.int id ]
                |> Sql.executeAsync 
                    (fun read -> 
                        { id = read.int "id"
                          userid = read.int "userid"
                          title = read.string "title"
                          imageUrl = read.stringOrNone "imageurl"
                          description = read.stringOrNone "description"
                          notes = read.stringOrNone "notes"
                          ingredients = List.empty
                          steps = List.empty })

            match withSubs with 
            | true ->
                let! (ingredientsResult, stepsResult) = findSubs id
                match recipeQuery, ingredientsResult, stepsResult with 
                | Result.Ok recipes, Result.Ok ingredients, Result.Ok steps ->
                    return Result.Ok { recipes.Head with ingredients = ingredients; steps = steps }
                | Result.Ok recipes, Error ingredientErrs, Error stepsErrs ->
                    eprintfn "%O" ingredientErrs
                    eprintfn "%O" stepsErrs
                    return Result.Ok recipes.Head
                | Result.Ok recipes, Error ingredientErrs, Result.Ok steps -> 
                    eprintfn "%O" ingredientErrs
                    return Result.Ok { recipes.Head with steps = steps }
                | Result.Ok recipes, Result.Ok ingredients, Error stepErrs -> 
                    eprintfn "%O" stepErrs
                    return Result.Ok { recipes.Head with ingredients = ingredients }
                | Result.Error recipesErr, _, _ ->
                    return Error recipesErr
            | false ->
                match recipeQuery with 
                | Result.Ok recipes -> 
                    return Result.Ok recipes.Head
                | Error ex -> 
                    return Error ex

        }

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

    let private upsertIngredients (ingredients: list<Ingredient>) (recipeId: int) : Async<bool> = 
        async  {
            let! result = 
                defaultConnection
                |> Sql.connect
                |> Sql.executeTransactionAsync
                    [ @"UPDATE ingredients
                        SET name = @name,
                            quantity = @quantity,
                        WHERE recipeId = @recipeid", 
                        ingredients 
                        |> List.map(fun i -> 
                            [ "recipeid", Sql.int recipeId
                              "name", Sql.string i.name
                              "quantity", Sql.string i.quantity ])
                    ]
            match result with 
            | Result.Ok _ -> 
                return true
            | Error err -> 
                eprintfn "%O" err
                return false
        }

    let private upsertSteps (steps: list<RecipeStep>) (recipeId: int) : Async<bool> = 
        async  {
            let! result = 
                defaultConnection
                |> Sql.connect
                |> Sql.executeTransactionAsync
                    [ @"UPDATE steps
                        SET stepOrder = @steporder,
                            directions = @directions,
                            imageUrl = @imageurl,
                        WHERE recipeId = @recipeid", 
                        steps 
                        |> List.map(fun s -> 
                            [ "recipeid", Sql.int recipeId
                              "steporder", Sql.int s.order
                              "directions", Sql.string s.directions
                              "imageUrl", Sql.stringOrNone s.imageUrl ] )
                    ]
            match result with 
            | Result.Ok _ -> 
                return true
            | Error err -> 
                eprintfn "%O" err
                return false
        }

    let update (recipe: Recipe) : Async<Result<bool, exn>> = 
        async {
            let! recipeQuery =
                defaultConnection
                |> Sql.connect
                |> Sql.query 
                    @"UPDATE recipes
                      SET title = @title,
                          imageUrl = @imageurl,
                          description = @description,
                          notes = @notes
                      WHERE id = @id"
                |> Sql.parameters
                    [ "id", Sql.int recipe.id
                      "title", Sql.string recipe.title
                      "imageurl", Sql.stringOrNone recipe.imageUrl
                      "description", Sql.stringOrNone recipe.description
                      "notes", Sql.stringOrNone recipe.notes
                    ]
                |> Sql.executeNonQueryAsync

            let! ingredientsQuery =  upsertIngredients recipe.ingredients recipe.id
            let! stepsQuery =  upsertSteps recipe.steps recipe.id

            match recipeQuery, ingredientsQuery, stepsQuery with 
            | Result.Ok _, true, true ->
                return Result.Ok true
            | Result.Ok _, false, false  ->
                return Error (exn "Recipe Updated but Failed to update ingredients and steps")
            | Result.Ok _, true, false ->
                return Error (exn "Recipe Updated but Failed to update steps")
            | Result.Ok _, false, true ->
                return Error (exn "Recipe Updated but Failed to update ingredients")
            | Result.Error err, _, _ ->
                return Error err
        }

    let delete (recipeId: int) : Async<Result<bool, exn>> = 
        async  {
            let! result =
                defaultConnection
                |> Sql.connect
                |> Sql.query @"DELETE FROM recipes WHERE id = @id"
                |> Sql.parameters [ "id", Sql.int recipeId]
                |> Sql.executeNonQueryAsync
            match result with 
            | Result.Ok -> 
                return Result.Ok true
            | Error err -> 
                return Error err
        }

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
