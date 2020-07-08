namespace Frandadin.Client.Components

open Elmish
open Bolero
open Bolero.Templating.Client
open Bolero.Remoting
open Bolero.Remoting.Client
open Frandadin.Client.Services
open Microsoft.AspNetCore.Components
open Frandadin.Client.Types
open Frandadin.Client.Validators
open Frandadin.Client.Css
open AccidentalFish.FSharp.Validation

module RecipeForm =

    type RecipeValue =
        | Existing of recipe: Recipe
        | New of newRecipe: {| title: string; imageUrl: Option<string>; description: Option<string>; notes: Option<string> |}

    type PartialRecipe = {| title: string; imageUrl: string; description: string; notes: string |}


    type State =
        { recipeValue: Option<RecipeValue>
          title: string
          imageUrl: string
          description: string
          notes: string
          error: Option<string>
          validationErrors: Option<list<ValidationItem>> }

    type UpdateProperty =
        | Title
        | Description
        | Notes
        | ImageUrl

    type Msg =
        | SubmitRecipe
        | ValidateSubmission of RecipeValue
        | UpdateValue of UpdateProperty * string
        | SaveRecipe of RecipeValue
        | InvokeSuccess of unit
        | InvokeError of exn
        | ClearError

    let init (recipeValue: Option<RecipeValue>) =
        let values: PartialRecipe =
            match recipeValue with
            | Some recipe ->
                match recipe with
                | Existing existing ->
                    {| title = existing.title
                       imageUrl = existing.imageUrl |> Option.defaultValue ""
                       description = existing.description |> Option.defaultValue ""
                       notes = existing.notes |> Option.defaultValue "" |}
                | New newRecipe ->
                    {| title = newRecipe.title
                       imageUrl = newRecipe.imageUrl |> Option.defaultValue ""
                       description = newRecipe.description |> Option.defaultValue ""
                       notes = newRecipe.notes |> Option.defaultValue "" |}
            | None ->
                {| title = ""
                   imageUrl = ""
                   description = ""
                   notes = "" |}
        { recipeValue = recipeValue
          title = values.title
          imageUrl = values.imageUrl
          description = values.description
          notes = values.notes
          error = None
          validationErrors = None }, Cmd.none

    let update (msg: Msg) (state: State) (onSaveRecipe: EventCallback<RecipeValue>) =
        match msg with
        | ClearError -> { state with error = None }, Cmd.none
        | UpdateValue(prop, value) ->
            let state =
                match prop with
                | Title -> { state with title = value }
                | Description -> { state with description = value }
                | Notes -> { state with notes = value }
                | ImageUrl -> { state with imageUrl = value }
            state, Cmd.none
        | SubmitRecipe ->
            let valueOrNone (value: string) =
                if System.String.IsNullOrWhiteSpace value && System.String.IsNullOrEmpty value then
                    None 
                else 
                    Some value
            
            let recipe =
                match state.recipeValue with
                | Some recipe ->
                    match recipe with
                    | Existing recipe ->
                        Existing
                            { recipe with
                                  title = state.title
                                  imageUrl = valueOrNone state.imageUrl
                                  description = valueOrNone state.description
                                  notes = valueOrNone state.notes }
                    | New recipe ->
                        New
                            {| title = state.title
                               imageUrl = valueOrNone state.imageUrl
                               description = valueOrNone state.description
                               notes = valueOrNone state.notes |}
                | None ->
                    New
                        {| title = state.title
                           imageUrl = valueOrNone state.imageUrl
                           description = valueOrNone state.description
                           notes = valueOrNone state.notes |}

            state, Cmd.ofMsg (ValidateSubmission recipe)
        | ValidateSubmission recipe ->
            match recipe with
            | Existing recipe ->
                let result = validateRecipe recipe
                match result with
                | ValidationState.Ok -> state, Cmd.ofMsg (SaveRecipe(Existing recipe))
                | Errors errors -> { state with validationErrors = Some errors }, Cmd.none
            | New recipe ->
                let result = validateAnonymusRecipe recipe
                match result with
                | ValidationState.Ok -> state, Cmd.ofMsg (SaveRecipe(New recipe))
                | Errors errors -> { state with validationErrors = Some errors }, Cmd.none
        | SaveRecipe recipe ->
            let task() = onSaveRecipe.InvokeAsync(recipe) |> Async.AwaitTask
            state, Cmd.ofAsync task () InvokeSuccess InvokeError
        | InvokeSuccess _ -> state, Cmd.none
        | InvokeError ex ->
            eprintfn "RecipeForm Error: %O" ex
            state, Cmd.none


    let private form (recipe: PartialRecipe) (dispatch: Msg -> unit) =
        Html.form
            [ Classes []
              Html.on.submit (fun _ -> dispatch SubmitRecipe) ]
            [ Html.section [ Classes [ Spectre.formGroup ] ]
                  [ Html.label [ Classes [ Spectre.formLabel ] ] [ Html.text "Title" ]
                    Html.input
                        [ Classes [ Spectre.formInput ]
                          Html.attr.required true
                          Html.bind.input.string recipe.title (fun value -> dispatch (UpdateValue(Title, value))) ] ]
              Html.section [ Classes [ Spectre.formGroup ] ]
                  [ Html.label [ Classes [ Spectre.formLabel ] ] [ Html.text "Description" ]
                    Html.input
                        [ Classes [ Spectre.formInput ]
                          Html.bind.input.string recipe.description
                              (fun value -> dispatch (UpdateValue(Description, value))) ] ]
              Html.section [ Classes [ Spectre.formGroup ] ]
                  [ Html.label [ Classes [ Spectre.formLabel ] ] [ Html.text "Notes" ]
                    Html.input
                        [ Classes [ Spectre.formInput ]
                          Html.bind.input.string recipe.notes (fun value -> dispatch (UpdateValue(Notes, value))) ] ]
              Html.section [ Classes [ Spectre.formGroup ] ]
                  [ Html.label [ Classes [ Spectre.formLabel ] ] [ Html.text "Recipe Picture" ]
                    Html.input
                        [ Classes [ Spectre.formInput ]
                          Html.bind.input.string recipe.imageUrl (fun value -> dispatch (UpdateValue(ImageUrl, value))) ] ]
              Html.section [ Classes [ Spectre.formGroup ] ]
                  [ Html.button [ Classes [ Spectre.btn; Spectre.btnLg; Spectre.btnPrimary ] ]
                        [ Html.text "Save Recipe" ] ] ]

    let miscError error dispatch =
        Html.cond error <| function
        | Some error ->
            Html.div [ Classes [ Spectre.toast; Spectre.toastError ] ]
                [ Html.button
                    [ Classes [ Spectre.btn; Spectre.btnClear; Spectre.floatRight ]
                      Html.on.click (fun _ -> dispatch ClearError) ] []
                  Html.textf "We have this issue: %s" error ]
        | None -> Html.empty

    let validationErrors (errors: Option<list<ValidationItem>>) =
        Html.cond errors <| function
        | Some errors ->
            Html.div []
                [ Html.label [ Classes [ Spectre.labelWarning; Spectre.labelLg ] ]
                      [ Html.text "We found the following issues." ]
                  Html.ul []
                      [ for error in errors do
                          Html.li [ Classes [ Spectre.textError ] ]
                              [ Html.textf "%s %s" error.property error.message ] ] ]
        | None -> Html.empty

    let view (state: State) (dispatch: Msg -> unit) =
        Html.article [ Classes [ css.franRecipeForm ] ]
            [ miscError state.error dispatch
              validationErrors state.validationErrors
              form
                  ({| title = state.title
                      description = state.description
                      imageUrl = state.imageUrl
                      notes = state.notes |}) dispatch ]

    type Form() =
        inherit ProgramComponent<State, Msg>()

        [<Parameter>]
        member val Recipe: Option<RecipeValue> = None with get, set

        [<Parameter>]
        member val OnSaveRecipe: EventCallback<RecipeValue> = new EventCallback<RecipeValue>() with get, set

        override this.Program =
            let update (msg: Msg) (state: State) = update msg state this.OnSaveRecipe
            let init _ = init this.Recipe
            Program.mkProgram init update view
