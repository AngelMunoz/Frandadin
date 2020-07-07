namespace Frandadin.Client.Pages

module Recipes =
    open Elmish
    open Bolero
    open Bolero.Remoting
    open Bolero.Remoting.Client
    open Bolero.Templating.Client
    open Frandadin.Client.Services

    type State =
        { count: int }

    type Msg =
        | Increment
        | Decrement

    let private init _ = { count = 0 }, Cmd.none

    let private update (msg: Msg) (state: State) (recipes: RecipeService) =
        match msg with
        | Increment -> { state with count = state.count + 1 }, Cmd.none
        | Decrement -> { state with count = state.count - 1 }, Cmd.none

    let private view (state: State) (dispatch: Msg -> unit) =
        Html.article [ Classes [ "fran-page" ] ] [ Html.text "Recipes!" ]

    type RecipesPage() =
        inherit ProgramComponent<State, Msg>()

        override this.Program =
            let recipeService = this.Remote<RecipeService>()
            let update (msg: Msg) (state: State) = update msg state recipeService

            Program.mkProgram init update view
