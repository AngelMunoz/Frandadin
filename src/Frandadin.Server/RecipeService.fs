namespace Frandadin.Server

open System
open Microsoft.AspNetCore.Hosting
open Bolero
open Bolero.Remoting
open Bolero.Remoting.Server
open Frandadin

type RecipeService(ctx: IRemoteContext, env: IWebHostEnvironment) =
    inherit RemoteHandler<Client.Services.RecipeService>()


    override __.Handler = 
        {
            find = fun pagination -> failwith "Not Implemented Exception"
            findOne = fun recipeId -> failwith "Not Implemented Exception"
            create = fun payload -> failwith "Not Implemented Exception"
            update = fun recipe -> failwith "Not Implemented Exception"
            destroy = fun recipeId ->  failwith "Not Implemented Exception"
        }