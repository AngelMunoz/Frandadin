namespace Frandadin.Client.Pages

open Elmish
open Bolero
open Frandadin.Client

module Auth = 

    type State =
        { Noop: bool }

    type Msg =
        | Noop

    let init = 
        { Noop = true }, Cmd.none

    let update state msg auth = 
        match msg with 
        | Noop -> state, Cmd.none

    let private view state dispatch = 
        Views.Auth().Elt()

    type AuthPage() =
        inherit ElmishComponent<State, Msg>()

        override __.View state dispatch = view state dispatch