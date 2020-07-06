namespace Frandadin.Client.Pages


module Recipe = 
    open Elmish
    open Bolero
    open Bolero.Remoting
    open Bolero.Remoting.Client
    open Bolero.Templating.Client
    open Frandadin.Client.Services
    open Microsoft.AspNetCore.Components
    open Frandadin.Client.Types

    type State =
        { id: Option<int>; recipe: Option<Recipe>; error: Option<string> }

    type Msg =
        | GetRecipe of Option<int>
        | RecvRecipe of Option<Result<Recipe, ErrorResponse>>
        | Error of exn

    let private init recipeId = 
        { id = recipeId; recipe = None; error = None }, Cmd.none

    let private update (msg: Msg) (state: State) (recipes: RecipeService) : State * Cmd<Msg> = 
        match msg with 
        | GetRecipe id -> 
            let cmd = 
                match id with
                | Some id ->
                    Cmd.ofAuthorized recipes.findOne id RecvRecipe Error
                | None -> Cmd.none
            state, cmd
        | RecvRecipe recipe ->
            match recipe with 
            | Some result ->
                match result with
                | Result.Ok recipe ->
                    { state with recipe = Some recipe }, Cmd.none
                | Result.Error err -> { state with error = Some err.message }, Cmd.none
            | None -> state, Cmd.none
        | Error RemoteUnauthorizedException ->
            { state with error = Some "You have been logged out." }, Cmd.none
        | Error exn -> { state with error = Some exn.Message }, Cmd.none

    let private view (state: State) (dispatch: Msg -> unit) = 
        Html.article [ Classes [ "fran-page" ] ] [ Html.textf "Recipe %A!" state.id ]

    type RecipePage() =
        inherit ProgramComponent<State, Msg>()

        [<Parameter>]
        member val recipeId : Option<int> = None with get, set

        override this.Program =
            let recipeService = this.Remote<RecipeService>()
            let update (msg: Msg) (state: State) = update msg state recipeService
            let init _ = 
                init this.recipeId
            Program.mkProgram init update view