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

module private RecipeSteps = 
    open Npgsql
    open Npgsql.FSharp
    open Database

    let create 
        (recipestep: {| recipeid: int; order: int; directions: string; imageUrl: Option<string> |})
        : Async<Result<RecipeStep, exn>> =
        async {
            let! inserts = 
                defaultConnection
                |> Sql.connect
                |> Sql.query
                    @"INSERT INTO recipesteps(recipeid, steporder, directions, imageurl)
                      VALUES (@recipeid, @steporder, @directions, @imageurl)
                      RETURNING id"
                |> Sql.parameters
                    [ "recipeid", Sql.int recipestep.recipeid
                      "steporder", Sql.int recipestep.order
                      "directions", Sql.string recipestep.directions
                      "imageurl", Sql.stringOrNone recipestep.imageUrl ]
                |> Sql.executeAsync(fun read -> read.int "id")
            match inserts with 
            | Result.Ok rows ->
                let query = 
                    defaultConnection
                    |> Sql.connect
                    |> Sql.query
                        @"SELECT * FROM recipesteps WHERE id = @id"
                    |> Sql.parameters [ "id", Sql.int rows.Head ]
                    |> Sql.executeRowAsync 
                        (fun read -> 
                            { id = read.int "id"
                              recipeid = read.int "recipeid"
                              order = read.int "steporder"
                              directions = read.string "directions"
                              imageUrl = read.stringOrNone "imageurl" })
                return! query
            | Error ex -> return Error ex
        }

    let update (step: RecipeStep) : Async<Result<bool, exn>> = 
        async  {
            let! result = 
                defaultConnection
                |> Sql.connect
                |> Sql.query
                    @"UPDATE recipesteps
                      SET steporder = @steporder,
                          directions = @directions,
                          imageurl = @imageurl
                      WHERE id = @id"
                |> Sql.parameters 
                    [ "id", Sql.int step.id
                      "steporder", Sql.int step.order
                      "directions", Sql.string step.directions
                      "imageurl", Sql.stringOrNone step.imageUrl ]
                |> Sql.executeNonQueryAsync
            match result with 
            | Result.Ok result -> 
                return Result.Ok (result = 1)
            | Error err ->
                return Error err
        }

    let delete (stepid: int) : Async<Result<bool, exn>> = 
        async  {
            let! result =
                defaultConnection
                |> Sql.connect
                |> Sql.query @"DELETE FROM recipesteps WHERE id = @id"
                |> Sql.parameters [ "id", Sql.int stepid ]
                |> Sql.executeNonQueryAsync
            match result with 
            | Result.Ok results ->
                return Result.Ok (results = 1)
            | Error err -> 
                return Error err
        }

    let extractUserId (ctx: IRemoteContext) : int =
        let userNameItendifierClaim = 
            ctx.HttpContext.User.Claims |> Seq.find (fun c -> c.Type = ClaimTypes.NameIdentifier)
        userNameItendifierClaim.Value |> int

    let userHasAccess (userid: int) (recipeid: int) : Async<Result<bool, exn>> = 
        defaultConnection
        |> Sql.connect
        |> Sql.query "SELECT EXISTS(SELECT COUNT(1) FROM recipes WHERE id = @id and userid = @userid)"
        |> Sql.parameters [ "id", Sql.int recipeid; "userid", Sql.int userid ]
        |> Sql.executeRowAsync (fun read -> read.bool "exists")

type RecipeStepService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<Client.Services.RecipeStepService>()

    override __.Handler = 
        { create = 
            ctx.Authorize
            <| fun step -> 
                async {
                    let userId = RecipeSteps.extractUserId ctx
                    let! hasAccess = RecipeSteps.userHasAccess userId step.recipeid
                    match hasAccess with 
                    | Result.Ok access ->
                        match access with 
                        | false ->
                            ctx.HttpContext.Response.StatusCode <- 403
                            return Error { message = "You don't have access to that recipe"
                                           code = Some 403
                                           errors = List.empty }
                        | true ->
                            match validateAnonymusRecipeStep step with 
                            | ValidationState.Ok ->
                                let! result = 
                                    RecipeSteps.create 
                                        {| recipeid = step.recipeid
                                           order =  step.order
                                           directions = step.directions
                                           imageUrl = step.imageUrl |}
                                match result with 
                                | Result.Ok result ->
                                    ctx.HttpContext.Response.StatusCode <- 201
                                    return Result.Ok result
                                | Error err ->
                                    eprintfn "%O" err
                                    ctx.HttpContext.Response.StatusCode <- 422
                                    return Error { message = "Could not create ingredient"
                                                   code = Some 422
                                                   errors = List.empty }
                            | ValidationState.Errors errors ->
                                ctx.HttpContext.Response.StatusCode <- 400
                                return Error { message = stringifyErrors errors
                                               code = Some 400
                                               errors = objectifyList errors }
                    | Error err ->
                        eprintfn "%O" err
                        ctx.HttpContext.Response.StatusCode <- 422
                        return Error { message = "Can't get access status for resource"
                                       code = Some 422
                                       errors = List.empty }
                }

          update = 
              ctx.Authorize
              <| fun step -> 
                async {
                    let userId = RecipeSteps.extractUserId ctx
                    let! hasAccess = RecipeSteps.userHasAccess userId step.recipeid
                    match hasAccess with 
                    | Result.Ok access ->
                        match access with 
                        | false ->
                            ctx.HttpContext.Response.StatusCode <- 403
                            return Error { message = "You don't have access to that recipe"
                                           code = Some 403
                                           errors = List.empty }
                        | true ->
                            match validateRecipeStep step with 
                            | ValidationState.Ok ->
                                let! result = RecipeSteps.update step
                                match result with 
                                | Result.Ok result ->
                                    return Result.Ok result
                                | Error err ->
                                    eprintfn "%O" err
                                    ctx.HttpContext.Response.StatusCode <- 422
                                    return Error { message = "Could not update Step"
                                                   code = Some 422
                                                   errors = List.empty }
                            | ValidationState.Errors errors ->
                                ctx.HttpContext.Response.StatusCode <- 400
                                return Error { message = stringifyErrors errors
                                               code = Some 400
                                               errors = objectifyList errors }
                    | Error err ->
                        eprintfn "%O" err
                        ctx.HttpContext.Response.StatusCode <- 422
                        return Error { message = "Can't get access status for resource"
                                       code = Some 422
                                       errors = List.empty }
                }

          delete = 
            ctx.Authorize
            <| fun stepid -> 
                async {
                    let userId = RecipeSteps.extractUserId ctx
                    let! hasAccess = RecipeSteps.userHasAccess userId stepid
                    match hasAccess with 
                    | Result.Ok access ->
                        match access with 
                        | false ->
                            ctx.HttpContext.Response.StatusCode <- 403
                            return Error { message = "You don't have access to that recipe"
                                           code = Some 403
                                           errors = List.empty }
                        | true ->
                            let! result = RecipeSteps.delete stepid
                            match result with 
                            | Result.Ok result -> 
                                return Result.Ok result
                            | Error err ->
                                eprintfn "%O" err
                                ctx.HttpContext.Response.StatusCode <- 422
                                return Error { message = "Could not update ingredient"
                                               code = Some 422
                                               errors = List.empty }
                    | Error err ->
                        eprintfn "%O" err
                        ctx.HttpContext.Response.StatusCode <- 422
                        return Error { message = "Can't get access status for resource"
                                       code = Some 422
                                       errors = List.empty }
                }
        }
