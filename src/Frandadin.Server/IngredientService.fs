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

module private Ingredients =
    open Npgsql
    open Npgsql.FSharp
    open Database

    let create (ingredient: {| recipeid: int; name: string; quantity: string |}): Async<Result<Ingredient, exn>> =
        async {
            let! inserts = defaultConnection
                           |> Sql.connect
                           |> Sql.query @"INSERT INTO ingredients(recipeid, name, quantity)
                                          VALUES (@recipeid, @name, @quantity)
                                          RETURNING id"
                           |> Sql.parameters
                               [ "recipeid", Sql.int ingredient.recipeid
                                 "name", Sql.string ingredient.name
                                 "quantity", Sql.string ingredient.quantity ]
                           |> Sql.executeAsync (fun read -> read.int "id")
            match inserts with
            | Result.Ok rows ->
                let query =
                    defaultConnection
                    |> Sql.connect
                    |> Sql.query @"SELECT * FROM ingredients WHERE id = @id"
                    |> Sql.parameters [ "id", Sql.int rows.Head ]
                    |> Sql.executeRowAsync (fun read ->
                        { id = read.int "id"
                          recipeid = read.int "recipeid"
                          name = read.string "name"
                          quantity = read.string "quantity" })
                return! query
            | Error ex -> return Error ex
        }

    let update (ingredient: Ingredient): Async<Result<bool, exn>> =
        async {
            let! result = defaultConnection
                          |> Sql.connect
                          |> Sql.query @"UPDATE ingredients
                                         SET name = @name,
                                             quantity = @quantity,
                                         WHERE recipeid = @recipeid"
                          |> Sql.parameters
                              [ "recipeid", Sql.int ingredient.recipeid
                                "name", Sql.string ingredient.name
                                "quantity", Sql.string ingredient.quantity ]
                          |> Sql.executeNonQueryAsync
            match result with
            | Result.Ok result -> return Result.Ok(result = 1)
            | Error err -> return Error err
        }

    let delete (recipeId: int): Async<Result<bool, exn>> =
        async {
            let! result = defaultConnection
                          |> Sql.connect
                          |> Sql.query @"DELETE FROM recipes WHERE id = @id"
                          |> Sql.parameters [ "id", Sql.int recipeId ]
                          |> Sql.executeNonQueryAsync
            match result with
            | Result.Ok results -> return Result.Ok(results = 1)
            | Error err -> return Error err
        }

    let extractUserId (ctx: IRemoteContext): int =
        let userNameItendifierClaim =
            ctx.HttpContext.User.Claims |> Seq.find (fun c -> c.Type = ClaimTypes.NameIdentifier)
        userNameItendifierClaim.Value |> int

    let userHasAccess (userid: int) (recipeid: int): Async<Result<bool, exn>> =
        defaultConnection
        |> Sql.connect
        |> Sql.query "SELECT EXISTS(SELECT COUNT(1) FROM recipes WHERE id = @id and userid = @userid)"
        |> Sql.parameters
            [ "id", Sql.int recipeid
              "userid", Sql.int userid ]
        |> Sql.executeRowAsync (fun read -> read.bool "exists")

type IngredientService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<Client.Services.IngredientService>()

    override __.Handler =
        { create =
              ctx.Authorize <| fun ingredient ->
                  async {
                      let userId = Ingredients.extractUserId ctx
                      let! hasAccess = Ingredients.userHasAccess userId ingredient.recipeid
                      match hasAccess with
                      | Result.Ok access ->
                          match access with
                          | false ->
                              ctx.HttpContext.Response.StatusCode <- 403
                              return Error
                                         { message = "You don't have access to that recipe"
                                           code = Some 403
                                           errors = List.empty }
                          | true ->
                              match validateAnonymusIngredient ingredient with
                              | ValidationState.Ok ->
                                  let! result = Ingredients.create
                                                    {| recipeid = ingredient.recipeid
                                                       name = ingredient.name
                                                       quantity = ingredient.quantity |}
                                  match result with
                                  | Result.Ok result ->
                                      ctx.HttpContext.Response.StatusCode <- 201
                                      return Result.Ok result
                                  | Error err ->
                                      eprintfn "%O" err
                                      ctx.HttpContext.Response.StatusCode <- 422
                                      return Error
                                                 { message = "Could not create ingredient"
                                                   code = Some 422
                                                   errors = List.empty }
                              | ValidationState.Errors errors ->
                                  ctx.HttpContext.Response.StatusCode <- 400
                                  return Error
                                             { message = stringifyErrors errors
                                               code = Some 400
                                               errors = objectifyList errors }
                      | Error err ->
                          eprintfn "%O" err
                          ctx.HttpContext.Response.StatusCode <- 422
                          return Error
                                     { message = "Can't get access status for resource"
                                       code = Some 422
                                       errors = List.empty }
                  }

          update =
              ctx.Authorize <| fun ingredient ->
                  async {
                      let userId = Ingredients.extractUserId ctx
                      let! hasAccess = Ingredients.userHasAccess userId ingredient.recipeid
                      match hasAccess with
                      | Result.Ok access ->
                          match access with
                          | false ->
                              ctx.HttpContext.Response.StatusCode <- 403
                              return Error
                                         { message = "You don't have access to that recipe"
                                           code = Some 403
                                           errors = List.empty }
                          | true ->
                              match validateIngredient ingredient with
                              | ValidationState.Ok ->
                                  let! result = Ingredients.update ingredient
                                  match result with
                                  | Result.Ok result -> return Result.Ok result
                                  | Error err ->
                                      eprintfn "%O" err
                                      ctx.HttpContext.Response.StatusCode <- 422
                                      return Error
                                                 { message = "Could not update ingredient"
                                                   code = Some 422
                                                   errors = List.empty }
                              | ValidationState.Errors errors ->
                                  ctx.HttpContext.Response.StatusCode <- 400
                                  return Error
                                             { message = stringifyErrors errors
                                               code = Some 400
                                               errors = objectifyList errors }
                      | Error err ->
                          eprintfn "%O" err
                          ctx.HttpContext.Response.StatusCode <- 422
                          return Error
                                     { message = "Can't get access status for resource"
                                       code = Some 422
                                       errors = List.empty }
                  }

          delete =
              ctx.Authorize <| fun ingredientId ->
                  async {
                      let userId = Ingredients.extractUserId ctx
                      let! hasAccess = Ingredients.userHasAccess userId ingredientId
                      match hasAccess with
                      | Result.Ok access ->
                          match access with
                          | false ->
                              ctx.HttpContext.Response.StatusCode <- 403
                              return Error
                                         { message = "You don't have access to that recipe"
                                           code = Some 403
                                           errors = List.empty }
                          | true ->
                              let! result = Ingredients.delete ingredientId
                              match result with
                              | Result.Ok result -> return Result.Ok result
                              | Error err ->
                                  eprintfn "%O" err
                                  ctx.HttpContext.Response.StatusCode <- 422
                                  return Error
                                             { message = "Could not update ingredient"
                                               code = Some 422
                                               errors = List.empty }
                      | Error err ->
                          eprintfn "%O" err
                          ctx.HttpContext.Response.StatusCode <- 422
                          return Error
                                     { message = "Can't get access status for resource"
                                       code = Some 422
                                       errors = List.empty }
                  } }
