namespace Frandadin.Client

module Main =
    open Elmish
    open Bolero
    open Bolero.Html
    open Bolero.Remoting
    open Bolero.Remoting.Client
    open Bolero.Templating.Client
    open Microsoft.AspNetCore.Components.Routing

    open Services

    /// Routing endpoints definition.
    type Page =
        | [<EndPoint "/">] Recipes
        | [<EndPoint "/recipes/{id}">] Recipe of id: int

    /// The Elmish application's model.
    type State =
        { page: Page
          username: Option<string>
          error: Option<string>
          authState: Pages.Auth.State }

    /// The Elmish application's update messages.
    type Msg =
        | SetPage of Page
        | GetSignedInAs
        | Logout
        | RecvLogout of Option<unit>
        | RecvSignedInAs of Option<string>
        | Error of exn
        | AuthMsg of Pages.Auth.Msg

    let init _ =
        let (authState, authCmd) = Pages.Auth.init
        { page = Recipes
          username = None
          error = None
          authState = authState },
        Cmd.batch [ Cmd.ofMsg GetSignedInAs; authCmd ]

    /// Connects the routing system to the Elmish application.
    let router =
        Router.infer SetPage (fun state -> state.page)

    let handleExtrnAuth (extrnMsg: Option<Pages.Auth.ExternalMsg>) =
        match extrnMsg with
        | Some extrnMsg ->
            match extrnMsg with
            | Pages.Auth.ExternalMsg.Authenticated -> Cmd.ofMsg GetSignedInAs
        | None -> Cmd.none

    let update (msg: Msg) (state: State) (authService: AuthService) =
        match msg with
        | SetPage page -> { state with page = page }, Cmd.none
        | GetSignedInAs -> state, Cmd.ofAuthorized authService.getUser () RecvSignedInAs Error
        | RecvSignedInAs username ->
            let cmd =
                match username with
                | Some username ->
                    match router.setRoute "recipes" with
                    | Some msg -> Cmd.ofMsg msg
                    | None -> Cmd.none
                | None -> Cmd.none

            { state with username = username }, cmd
        | Logout ->
            state, Cmd.ofAuthorized authService.logout () RecvLogout Error
        | RecvLogout -> 
            { state with username = None; error = Some "You have ended your session" }, Cmd.none
        | AuthMsg authmsg ->
            let (authstate, cmd, extrnMsg) =
                Pages.Auth.update state.authState authmsg authService

            let handled = handleExtrnAuth extrnMsg
            { state with authState = authstate }, Cmd.batch [ Cmd.map AuthMsg cmd; handled ]
        | Error RemoteUnauthorizedException ->
            { state with
                  error = Some "You have been logged out."
                  username = None },
            Cmd.none
        | Error exn -> { state with error = Some exn.Message }, Cmd.none

    let private rightAuthMenu =
        a [ Classes [ "btn btn-link" ] ] [ text "Welcome" ]

    let private recipesMenuContent =
        navLink NavLinkMatch.All [attr.href "/" ; Classes [ "btn btn-link" ] ] [text "Recipes"]

    let private authView (authState: Pages.Auth.State) dispatch =
        ecomp<Pages.Auth.AuthPage, Pages.Auth.State, Pages.Auth.Msg> [] authState (AuthMsg >> dispatch)

    let private leftAuthMenu dispatch = 
        a [ Classes [ "btn btn-link" ]; Html.on.click(fun _ -> dispatch Logout) ] [ text "Log out" ]

    let private body (page : Page) : Node =
        cond page <| function
        | Recipes -> comp<Pages.Recipes.RecipesPage> [] []
        | Recipe recipeId -> comp<Pages.Recipe.RecipePage> [ "recipeId" => Some recipeId ] []

    let view (state: State) (dispatch: Msg -> unit) =

        let (body, leftMenu, rightMenu): Node * Node * Node =
            match state.username with
            | Some _ -> body state.page, recipesMenuContent, leftAuthMenu dispatch
            | None -> authView state.authState dispatch, rightAuthMenu, Html.empty

        Html.article [ Classes ["fran-content"] ]
            [ Html.header [ Classes ["navbar"; "fran-nav"; "fran-bg" ]] 
                [ Html.section [ Classes ["navbar-section"] ] [ leftMenu ]
                  Html.section [ Classes ["navbar-center"] ] []
                  Html.section [ Classes ["navbar-section"] ] [ rightMenu ]
                ]
              Html.main [ Classes ["fran-main"] ] [ body ]
              Html.footer [ Classes ["fran-footer"] ] [ Html.text "Some Footer" ] 
            ]

    type FrandadinApp() =
        inherit ProgramComponent<State, Msg>()

        override this.Program =
            let authService = this.Remote<AuthService>()
            let update msg state = update msg state authService

            Program.mkProgram init update view
            |> Program.withRouter router

