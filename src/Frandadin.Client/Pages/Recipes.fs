namespace Frandadin.Client.Pages

open Frandadin.Client.Types
open Frandadin.Client

module Recipes =
    open Elmish
    open Bolero
    open Bolero.Html
    open Bolero.Remoting
    open Bolero.Remoting.Client
    open Bolero.Templating.Client
    open Frandadin.Client.Services
    open Frandadin.Client.Css

    type State =
        { pagination: PaginationParams
          recipes: PaginationResult<Recipe>
          error: Option<string> }

    type Msg =
        | FetchRecipes
        | RecvRecipes of Option<Result<PaginationResult<Recipe>, ErrorResponse>>
        | SaveRecipe of Components.RecipeForm.RecipeValue
        | Error of exn

    let private init _ =
        let recipes =
            { count = 0
              list = list.Empty }

        let pagination =
            { page = 1
              limit = 10 }

        { recipes = recipes
          pagination = pagination
          error = None }, Cmd.none

    let private update (msg: Msg) (state: State) (recipes: RecipeService) =
        match msg with
        | FetchRecipes -> state, Cmd.ofAuthorized recipes.find state.pagination RecvRecipes Error
        | RecvRecipes result ->
            match result with
            | Some result ->
                match result with
                | Ok paginatedResult -> { state with recipes = paginatedResult }, Cmd.none
                | Result.Error err ->
                    printfn "%A" err.code
                    { state with error = Some err.message }, Cmd.none
            | None -> state, Cmd.none
        | SaveRecipe value ->
            printfn "Saving: %A" value
            state, Cmd.none
        | Error RemoteUnauthorizedException -> { state with error = Some "You have been logged out" }, Cmd.none
        | Error exn ->
            eprintfn "Failed to execute command %O" exn
            { state with error = Some "There was an error with the performed action." }, Cmd.none


    let private recipeView (paginated: PaginationResult<Recipe>) dispatch =
        let descriptionStr description =
            let description = description |> Option.defaultValue ""
            description.[..15] + "..."

        let recipeTile (recipe: Recipe) =
            Html.div [ Classes [ css.franRecipeTile; Spectre.tile ] ]
                [ Html.div [ Classes [ Spectre.tileContent ] ]
                      [ Html.p [ Classes [ Spectre.tileTitle ] ] [ Html.text recipe.title ]
                        Html.p [ Classes [ Spectre.tileSubtitle ] ] [ Html.text (descriptionStr recipe.description) ] ]
                  Html.div [ Classes [ Spectre.tileAction ] ]
                      [ Html.button [ Classes [ Spectre.btn; Spectre.btnLg ] ] [] ] ]

        Html.section [ Classes [ css.franRecipesGrid ] ] [ Html.forEach paginated.list recipeTile ]

    let private recipesToolbar (state: State) (dispatch: Msg -> unit) =
        Html.nav [ Classes [ Spectre.navbar ] ]
            [ Html.section [ Classes [ Spectre.navbarSection ] ] []
              Html.section [ Classes [ Spectre.navbarCenter ] ] []
              Html.section [ Classes [ Spectre.navbarSection ] ] [] ]

    let private view (state: State) (dispatch: Msg -> unit) =
        Html.article [ Classes [ css.franPage ] ]
            [ recipesToolbar state dispatch
              if state.recipes.count = 0 then
                  let recipeValue =
                      Components.RecipeForm.New
                          {| title = ""
                             imageUrl = None
                             description = None
                             notes = None |}
                  Html.comp<Components.RecipeForm.Form>
                      [ "Recipe" => Some recipeValue
                        Html.attr.callback "OnSaveRecipe" (fun recipeValue -> dispatch (SaveRecipe recipeValue)) ] []
              recipeView state.recipes dispatch ]

    type RecipesPage() =
        inherit ProgramComponent<State, Msg>()

        override this.Program =
            let recipeService = this.Remote<RecipeService>()
            let update (msg: Msg) (state: State) = update msg state recipeService

            Program.mkProgram init update view
