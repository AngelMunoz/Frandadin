namespace Frandadin.Client.Pages

module Auth =
    open Elmish
    open Bolero
    open Bolero.Remoting
    open AccidentalFish.FSharp.Validation
    open Frandadin.Client.Types
    open Frandadin.Client.Services
    open Frandadin.Client.Validators
    open Frandadin.Client.Css

    type CurrentForm =
        | LoginForm
        | SignUpForm

    type SettableField =
        | Email
        | Password
        | RepeatPassword
        | FirstName
        | LastName

    type State =
        { currentForm: CurrentForm
          repeatPassword: string
          loginPayload: Option<LoginPayload>
          signupPayload: Option<SignUpPayload>
          error: Option<string> }

    type ExternalMsg = | Authenticated

    type Msg =
        | SetForm of CurrentForm
        | SetField of SettableField * string
        | SubmitLogin
        | SubmitSignUp
        | ClearError
        | RecvAuthentication of Result<AuthResponse, ErrorResponse>
        | Error of exn

    let initSignPayload() =
        Some
            { email = ""
              password = ""
              name = ""
              lastName = "" }

    let initLoginPayload() =
        Some
            { email = ""
              password = "" }

    let init =
        { currentForm = SignUpForm
          loginPayload = None
          error = None
          repeatPassword = ""
          signupPayload =
              Some
                  { email = ""
                    password = ""
                    name = ""
                    lastName = "" } }, Cmd.none

    let update (state: State) (msg: Msg) (auth: AuthService): State * Cmd<Msg> * Option<ExternalMsg> =
        match msg with
        | SetForm form ->
            let (lp, sp) =
                match form with
                | LoginForm -> initLoginPayload(), None
                | SignUpForm -> None, initSignPayload()
            { state with
                  currentForm = form
                  loginPayload = lp
                  signupPayload = sp }, Cmd.none, None
        | SetField(field, value) ->
            let state =
                match state.currentForm with
                | LoginForm ->
                    let formValues =
                        match field with
                        | Password -> Some { state.loginPayload.Value with password = value }
                        | Email -> Some { state.loginPayload.Value with email = value }
                        | _ -> failwith "Unexpected state"
                    { state with loginPayload = formValues }
                | SignUpForm ->
                    let formValues =
                        match field with
                        | FirstName -> Some { state.signupPayload.Value with name = value }
                        | LastName -> Some { state.signupPayload.Value with lastName = value }
                        | Password -> Some { state.signupPayload.Value with password = value }
                        | Email -> Some { state.signupPayload.Value with email = value }
                        | _ -> state.signupPayload

                    let prestate = { state with signupPayload = formValues }
                    match field with
                    | RepeatPassword -> { prestate with repeatPassword = value }
                    | _ -> prestate
            state, Cmd.none, None
        | SubmitLogin ->
            match state.loginPayload with
            | Some payload ->
                match validateLogin payload with
                | ValidationState.Ok -> state, Cmd.ofAsync auth.login payload RecvAuthentication Error, None
                | ValidationState.Errors errors ->
                    let err =
                        errors
                        |> List.map (fun err -> sprintf "[%s - %s]" err.property err.message)
                        |> String.concat ""
                    { state with error = Some err }, Cmd.none, None
            | None -> state, Cmd.none, None
        | SubmitSignUp ->
            match state.signupPayload with
            | Some payload ->
                if payload.password <> state.repeatPassword then
                    { state with error = Some "Passwords do not match" }, Cmd.none, None
                else
                    match validateSignup payload with
                    | ValidationState.Ok -> state, Cmd.ofAsync auth.signup payload RecvAuthentication Error, None
                    | ValidationState.Errors errors ->
                        let err =
                            errors
                            |> List.map (fun err -> sprintf "[%s - %s]" err.property err.message)
                            |> String.concat ""
                        { state with error = Some err }, Cmd.none, None
            | None -> state, Cmd.none, None
        | RecvAuthentication result ->
            match result with
            | Result.Ok auth ->
                let (state, cmd) = init

                state, cmd, Some Authenticated
            | Result.Error err -> { state with error = Some err.message }, Cmd.none, None
        | ClearError -> { state with error = None }, Cmd.none, None
        | Error RemoteUnauthorizedException -> { state with error = Some "Failed to authenticate" }, Cmd.none, None
        | Error exn -> { state with error = Some exn.Message }, Cmd.none, None

    let private signUpForm (state: State) dispatch =
        let payload = state.signupPayload.Value
        Html.form
            [ Classes [ css.franBg; css.franAuthForm ]
              Html.on.submit (fun _ -> dispatch SubmitSignUp) ]
            [ Html.section [ Classes [ Spectre.formGroup ] ]
                  [ Html.label
                      [ Classes [ Spectre.formLabel ]
                        Html.attr.``for`` "firstName" ] [ Html.text "First Name" ]
                    Html.input
                        [ Classes [ Spectre.formInput ]
                          Html.attr.id "firstName"
                          Html.attr.name "firstName"
                          Html.attr.``type`` "text"
                          Html.attr.required true
                          Html.bind.input.string payload.name (fun v -> dispatch (SetField(FirstName, v))) ] ]
              Html.section [ Classes [ Spectre.formGroup ] ]
                  [ Html.label
                      [ Classes [ Spectre.formLabel ]
                        Html.attr.``for`` "lastName" ] [ Html.text "Last Name" ]
                    Html.input
                        [ Classes [ Spectre.formInput ]
                          Html.attr.id "lastName"
                          Html.attr.name "lastName"
                          Html.attr.``type`` "text"
                          Html.attr.required true
                          Html.bind.input.string payload.lastName (fun v -> dispatch (SetField(LastName, v))) ] ]
              Html.section [ Classes [ Spectre.formGroup ] ]
                  [ Html.label
                      [ Classes [ Spectre.formLabel ]
                        Html.attr.``for`` "email" ] [ Html.text "Email" ]
                    Html.input
                        [ Classes [ Spectre.formInput ]
                          Html.attr.id "email"
                          Html.attr.name "email"
                          Html.attr.``type`` "email"
                          Html.attr.required true
                          Html.bind.input.string payload.email (fun v -> dispatch (SetField(Email, v))) ] ]
              Html.section [ Classes [ Spectre.formGroup ] ]
                  [ Html.label
                      [ Classes [ Spectre.formLabel ]
                        Html.attr.``for`` "password" ] [ Html.text "Password" ]
                    Html.input
                        [ Classes [ Spectre.formInput ]
                          Html.attr.id "password"
                          Html.attr.name "password"
                          Html.attr.``type`` "password"
                          Html.attr.required true
                          Html.bind.input.string payload.password (fun v -> dispatch (SetField(Password, v))) ] ]
              Html.section [ Classes [ Spectre.formGroup ] ]
                  [ Html.label
                      [ Classes [ Spectre.formLabel ]
                        Html.attr.``for`` "repeatPassword" ] [ Html.text "Repeat your Password" ]
                    Html.input
                        [ Classes [ Spectre.formInput ]
                          Html.attr.id "repeatPassword"
                          Html.attr.name "repeatPassword"
                          Html.attr.``type`` "password"
                          Html.attr.required true
                          Html.bind.input.string state.repeatPassword (fun v -> dispatch (SetField(RepeatPassword, v))) ] ]
              Html.section
                  [ Classes [ Spectre.formGroup ]
                    Html.attr.style "margin: 0.5em auto" ]
                  [ Html.button
                      [ Classes [ Spectre.btn; Spectre.btnPrimary ]
                        Html.attr.``type`` "submit" ] [ Html.text "Submit" ] ] ]

    let private loginForm (state: State) dispatch =
        let payload = state.loginPayload.Value
        Html.form
            [ Classes [ css.franBg; css.franAuthForm ]
              Html.on.submit (fun _ -> dispatch SubmitLogin) ]
            [ Html.section [ Classes [ Spectre.formGroup ] ]
                  [ Html.label
                      [ Classes [ Spectre.formLabel ]
                        Html.attr.``for`` "email" ] [ Html.text "Email" ]
                    Html.input
                        [ Classes [ Spectre.formInput ]
                          Html.attr.id "email"
                          Html.attr.name "email"
                          Html.attr.``type`` "email"
                          Html.attr.required true
                          Html.bind.input.string payload.email (fun v -> dispatch (SetField(Email, v))) ] ]
              Html.section [ Classes [ Spectre.formGroup ] ]
                  [ Html.label
                      [ Classes [ Spectre.formLabel ]
                        Html.attr.``for`` "password" ] [ Html.text "Password" ]
                    Html.input
                        [ Classes [ Spectre.formInput ]
                          Html.attr.id "password"
                          Html.attr.name "password"
                          Html.attr.``type`` "password"
                          Html.attr.required true
                          Html.bind.input.string payload.password (fun v -> dispatch (SetField(Password, v))) ] ]
              Html.section
                  [ Classes [ Spectre.formGroup ]
                    Html.attr.style "margin: 0.5em auto" ]
                  [ Html.button
                      [ Classes [ "btn btn-primary" ]
                        Html.attr.``type`` "submit" ] [ Html.text "Submit" ] ] ]

    let private view (state: State) (dispatch: Msg -> unit) =
        let form =
            match state.currentForm with
            | LoginForm -> loginForm state dispatch
            | SignUpForm -> signUpForm state dispatch

        let (loginClass, signupClass) =
            match state.currentForm with
            | LoginForm -> Spectre.active, ""
            | SignUpForm -> "", Spectre.active

        Html.article [ Classes [ css.franPage ] ]
            [ Html.ul [ Classes [ Spectre.tab; Spectre.tabBlock ] ]
                  [ Html.li
                      [ Classes [ Spectre.tabItem; Spectre.cHand; loginClass ]
                        Html.on.click (fun _ -> dispatch (SetForm LoginForm)) ] [ Html.a [] [ Html.text "Log in" ] ]
                    Html.li
                        [ Classes [ Spectre.tabItem; Spectre.cHand; signupClass ]
                          Html.on.click (fun _ -> dispatch (SetForm SignUpForm)) ]
                        [ Html.a [] [ Html.text "Sign up" ] ] ]
              Html.cond state.error <| function
              | Some err ->
                  Html.div [ Classes [ Spectre.toast; Spectre.toastError ] ]
                      [ Html.button
                          [ Classes [ Spectre.btn; Spectre.btnClear; Spectre.floatRight ]
                            Html.on.click (fun _ -> dispatch ClearError) ] []
                        Html.textf "We have this issue: %s" err ]
              | None -> Html.empty
              form ]

    type AuthPage() =
        inherit ElmishComponent<State, Msg>()

        override __.View state dispatch = view state dispatch
