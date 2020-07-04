namespace Frandadin.Server

open System
open Microsoft.AspNetCore.Hosting
open Bolero
open Bolero.Remoting
open Bolero.Remoting.Server
open Frandadin

type AuthService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<Client.Services.AuthService>()


    override __.Handler = 
        {
            login = fun payload -> failwith "Not Implemented"
            signup = fun payload -> failwith "Not Implemented"
        }