namespace Frandadin.Client.Pages

open System
open Elmish
open Bolero
open Bolero.Json
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client
open Frandadin.Client
open Frandadin.Client.Services


module Recipes = 

    type State =
        { count: int }

    type Msg =
        | Increment 
        | Decrement

    let private init _ = 
        { count = 0 }, Cmd.none

    let private update (msg: Msg) (state: State) (recipes: RecipeService) = 
        match msg with 
        | Increment -> { state with count = state.count + 1 }, Cmd.none
        | Decrement -> { state with count = state.count - 1 }, Cmd.none

    let private view (state: State) (dispatch: Msg -> unit) = 
        Views.Recipes()
            .Increment(fun _ -> dispatch Increment)
            .Decrement(fun _ -> dispatch Decrement)
            .Count(string state.count)
            .Elt()

    type RecipesPage() =
        inherit ProgramComponent<State, Msg>()

        override this.Program =
            let recipeService = this.Remote<RecipeService>()
            let update (msg: Msg) (state: State) = update msg state recipeService

            Program.mkProgram init update view
#if DEBUG
            |> Program.withHotReload
#endif