namespace Frandadin.Server

module Database =
    open System
    open Npgsql.FSharp

    let private getEnv (name: string) = Environment.GetEnvironmentVariable name |> Option.ofObj

    let defaultConnection =
        let connection =
            match getEnv "DATABASE_URL" with
            | Some url -> url
            | None -> "postgresql://admin:Admin123@localhost:5432/frandadindb"
        Sql.fromUri (Uri connection)
