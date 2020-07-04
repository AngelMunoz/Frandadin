namespace Frandadin.Client

module Main = 

    open System
    open Elmish
    open Bolero
    open Bolero.Html
    open Bolero.Json
    open Bolero.Remoting
    open Bolero.Remoting.Client
    open Bolero.Templating.Client

    open Views
    open Services

    /// Routing endpoints definition.
    type Page =
        | [<EndPoint "/">] Home
        | [<EndPoint "/auth">] Auth
        | [<EndPoint "/recipes">] Recipes
        | [<EndPoint "/recipes/{id}">] Recipe of id: int
        | [<EndPoint "/notes">] Notes

    /// The Elmish application's model.
    type State=
        { page: Page
          authState: Pages.Auth.State }

    let initModel _ =
        let (authState, authCmd) = Pages.Auth.init
        { page = Home
          authState = authState }, Cmd.batch [authCmd]

    /// The Elmish application's update messages.
    type Msg =
        | SetPage of Page
        | AuthMsg of Pages.Auth.Msg

    let update msg state authService =
        match msg with
        | SetPage page ->
            { state with page = page }, Cmd.none
        | AuthMsg authmsg ->
            let (authstate, cmd) = Pages.Auth.update state.authState authmsg authService
            { state with authState = authstate }, Cmd.map AuthMsg cmd

    /// Connects the routing system to the Elmish application.
    let router = Router.infer SetPage (fun model -> model.page)

    let view state dispatch =
        let body : Node =
            cond state.page <| function
            | Home -> Views.Home().Elt()
            | Auth -> 
                ecomp<
                    Pages.Auth.AuthPage,
                    Pages.Auth.State,
                    Pages.Auth.Msg> 
                    [] 
                    state.authState (AuthMsg >> dispatch)
            | Recipe recipeId -> Views.Recipe().Elt()
            | Recipes -> 
                comp<Pages.Recipes.RecipesPage> [] []
            | Notes -> Views.Notes().Elt()
        Shell()
            .Body(body)
            .Elt()

    type FrandadinApp() =
        inherit ProgramComponent<State, Msg>()

        override this.Program =
            let authService = this.Remote<AuthService>()
            let update msg state = update msg state authService

            Program.mkProgram initModel update view
            |> Program.withRouter router
    #if DEBUG
            |> Program.withHotReload
    #endif
