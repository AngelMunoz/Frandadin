namespace Frandadin.Server


open System
open System.Security.Claims
open Microsoft.AspNetCore.Hosting
open Bolero.Remoting
open Bolero.Remoting.Server
open AccidentalFish.FSharp.Validation
open Frandadin
open Frandadin.Client.Types
open Frandadin.Client

module private Auth =
    open Npgsql
    open Npgsql.FSharp
    open Database

    type BCryptNet = BCrypt.Net.BCrypt

    let checkExists (email: string): Async<bool> =
        async {
            let! result = defaultConnection
                          |> Sql.connect
                          |> Sql.query "SELECT EXISTS(SELECT 1 FROM users WHERE email = @email)"
                          |> Sql.parameters [ "email", Sql.string email ]
                          |> Sql.executeAsync (fun read -> read.bool "exists")
            return match result with
                   | Result.Ok list -> list.Head
                   | Result.Error ex ->
                       eprintfn "%O" ex
                       false
        }

    let findUser (email: string): Async<Option<{| id: int; name: string; lastName: string; email: string; password: string |}>>
        =
        async {
            let! result = defaultConnection
                          |> Sql.connect
                          |> Sql.query "SELECT id, name, lastname, email, password FROM users where email = @email"
                          |> Sql.parameters [ "email", Sql.string email ]
                          |> Sql.executeAsync (fun read ->
                              {| id = read.int "id"
                                 name = read.string "name"
                                 lastName = read.string "lastname"
                                 email = read.string "email"
                                 password = read.string "password" |})
            return match result with
                   | Result.Ok list -> list |> List.tryHead
                   | Result.Error ex ->
                       eprintfn "%O" ex
                       None
        }

    let signupUser (payload: {| name: string; lastName: string; email: string; password: string |}): Async<Result<User, exn>> =
        async {
            let! result = defaultConnection
                          |> Sql.connect
                          |> Sql.query @"INSERT INTO users(name, lastName, email, password)
                                         VALUES(@name, @lastname, @email, @password) RETURNING id"
                          |> Sql.parameters
                              [ "name", Sql.string payload.name
                                "lastname", Sql.string payload.lastName
                                "email", Sql.string payload.email
                                "password", Sql.string payload.password ]
                          |> Sql.executeAsync (fun read -> read.int "id")
            match result with
            | Result.Ok list ->
                let! users = defaultConnection
                             |> Sql.connect
                             |> Sql.query "SELECT id, name, lastname, email FROM users where id = @id"
                             |> Sql.parameters [ "id", Sql.int list.Head ]
                             |> Sql.executeAsync (fun read ->
                                 { id = read.int "id"
                                   name = read.string "name"
                                   lastName = read.string "lastname"
                                   email = read.string "email" })

                return match users with
                       | Result.Ok users -> Result.Ok users.Head
                       | Result.Error ex -> Result.Error ex
            | Result.Error ex -> return Result.Error ex
        }

type AuthService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<Client.Services.AuthService>()

    override __.Handler =
        { getUser = ctx.Authorize <| fun () -> async { return ctx.HttpContext.User.Identity.Name }
          login =
              fun payload ->
                  async {
                      let! result = Auth.findUser payload.email
                      match result with
                      | Some user ->
                          match Auth.BCryptNet.EnhancedVerify(payload.password, user.password) with
                          | true ->
                              do! ctx.HttpContext.AsyncSignIn
                                      (user.email, TimeSpan.FromDays(1.),
                                       [ Claim(ClaimTypes.NameIdentifier, string user.id) ])
                              return Result.Ok
                                         { user =
                                               { id = user.id
                                                 name = user.name
                                                 lastName = user.lastName
                                                 email = user.email } }
                          | false ->
                              ctx.HttpContext.Response.StatusCode <- 401
                              return Result.Error
                                         { message = "Invalid credentials"
                                           code = Some 401
                                           errors = List.empty }
                      | None ->
                          ctx.HttpContext.Response.StatusCode <- 404
                          return Result.Error
                                     { message = "Not Found"
                                       code = Some 404
                                       errors = List.empty }
                  }
          logout = fun () -> ctx.HttpContext.AsyncSignOut()
          signup =
              fun payload ->
                  async {
                      let valResult = Validators.validateSignup payload
                      match valResult with
                      | ValidationState.Ok ->
                          let! exists = Auth.checkExists payload.email
                          if not exists then
                              let! signedUp = Auth.signupUser
                                                  {| email = payload.email
                                                     password = Auth.BCryptNet.EnhancedHashPassword(payload.password)
                                                     name = payload.name
                                                     lastName = payload.lastName |}

                              match signedUp with
                              | Result.Ok user ->
                                  do! ctx.HttpContext.AsyncSignIn
                                          (user.email, TimeSpan.FromDays(1.),
                                           [ Claim(ClaimTypes.NameIdentifier, string user.id) ])
                                  return Result.Ok { user = user }
                              | Result.Error err ->
                                  eprintfn "%O" err
                                  ctx.HttpContext.Response.StatusCode <- 400
                                  return Result.Error
                                             { message = "Failed to signup"
                                               code = Some 400
                                               errors = List.empty }
                          else
                              ctx.HttpContext.Response.StatusCode <- 409
                              return Result.Error
                                         { message = "This email is already taken"
                                           code = Some 409
                                           errors = list.Empty }
                      | ValidationState.Errors errors ->
                          let err =
                              errors
                              |> List.map (fun err -> sprintf "[%s - %s]" err.property err.message)
                              |> String.concat ""
                          ctx.HttpContext.Response.StatusCode <- 400
                          return Result.Error
                                     { message = err
                                       code = Some 400
                                       errors = errors |> List.map (fun item -> item :> obj) }
                  } }
