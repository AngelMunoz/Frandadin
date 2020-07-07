namespace Frandadin.Client

module Validators =
    open System.Text.RegularExpressions
    open AccidentalFish.FSharp.Validation
    open Types

    let stringifyErrors (errors: list<ValidationItem>) =
        errors
        |> List.map (fun err -> sprintf "[%s - %s]" err.property err.message)
        |> String.concat ""

    let objectifyList (errors: list<ValidationItem>) = errors |> List.map (fun item -> item :> obj)

    let validateSignup =
        createValidatorFor<SignUpPayload> () {
            validate (fun sp -> sp.name)
                [ isNotEmptyOrWhitespace
                  isNotNull
                  hasMaxLengthOf 100 ]
            validate (fun sp -> sp.lastName)
                [ isNotEmptyOrWhitespace
                  isNotNull
                  hasMaxLengthOf 100 ]
            validate (fun sp -> sp.email)
                [ isNotEmptyOrWhitespace
                  isNotNull
                  hasMaxLengthOf 100 ]
            validate (fun sp -> sp.password)
                [ isNotEmptyOrWhitespace
                  isNotNull
                  hasMaxLengthOf 100
                  withFunction (fun p ->
                      match Regex.IsMatch(p, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,30}$") with
                      | true -> Ok
                      | false ->
                          Errors
                              ([ { errorCode = "password:failed"
                                   message = "This password needs to contain A Lower case letter."
                                   property = "password" }
                                 { errorCode = "password:failed"
                                   message = "This password needs to contain A Upper case letter."
                                   property = "password" }
                                 { errorCode = "password:failed"
                                   message = "This password needs to contain at least 8 characters"
                                   property = "password" } ])) ]
        }

    let validateLogin =
        createValidatorFor<LoginPayload> () {
            validate (fun lp -> lp.email)
                [ isNotEmptyOrWhitespace
                  isNotNull
                  hasMaxLengthOf 100 ]
            validate (fun lp -> lp.password)
                [ isNotEmptyOrWhitespace
                  isNotNull
                  hasMaxLengthOf 100 ]
        }

    let validateIngredient =
        createValidatorFor<Ingredient> () {
            validate (fun i -> i.name)
                [ isNotNull
                  isNotEmptyOrWhitespace
                  hasMaxLengthOf 100 ]
            validate (fun i -> i.quantity)
                [ isNotNull
                  isNotEmptyOrWhitespace
                  hasMaxLengthOf 30 ]
        }

    let validateAnonymusIngredient =
        createValidatorFor<{| recipeid: int; name: string; quantity: string |}> () {
            validate (fun i -> i.name)
                [ isNotNull
                  isNotEmptyOrWhitespace
                  hasMaxLengthOf 100 ]
            validate (fun i -> i.quantity)
                [ isNotNull
                  isNotEmptyOrWhitespace
                  hasMaxLengthOf 30 ]
        }

    let validateRecipeStep =
        createValidatorFor<RecipeStep> () {
            validate (fun i -> i.order) [ isGreaterThan 0 ]
            validate (fun i -> i.directions)
                [ isNotNull
                  isNotEmptyOrWhitespace
                  hasMaxLengthOf 500 ]
            validateUnrequired (fun i -> i.imageUrl)
                [ isNotEmptyOrWhitespace
                  hasMaxLengthOf 254 ]
        }

    let validateAnonymusRecipeStep =
        createValidatorFor<{| recipeid: int; order: int; directions: string; imageUrl: Option<string> |}> () {
            validate (fun i -> i.order) [ isGreaterThan 0 ]
            validate (fun i -> i.directions)
                [ isNotNull
                  isNotEmptyOrWhitespace
                  hasMaxLengthOf 500 ]
            validateUnrequired (fun i -> i.imageUrl)
                [ isNotEmptyOrWhitespace
                  hasMaxLengthOf 254 ]
        }

    /// Used to validate Existing recipes (recipes with id and userid)
    let validateRecipe =
        createValidatorFor<Recipe> () {
            validate (fun r -> r.title)
                [ isNotNull
                  isNotEmptyOrWhitespace
                  hasMaxLengthOf 100 ]
            validateUnrequired (fun r -> r.imageUrl)
                [ isNotEmptyOrWhitespace
                  hasMaxLengthOf 254 ]
            validateUnrequired (fun r -> r.description)
                [ isNotNull
                  isNotEmptyOrWhitespace
                  hasMaxLengthOf 500 ]
            validateUnrequired (fun r -> r.notes)
                [ isNotNull
                  isNotEmptyOrWhitespace
                  hasMaxLengthOf 240 ]
            validate (fun r -> r.ingredients) [ eachItemWith validateIngredient ]
            validate (fun r -> r.steps) [ eachItemWith validateRecipeStep ]
        }

    /// Used to validate new recipes (recipes without id and userid)
    /// hence why the usage of an anonymus record
    let validateAnonymusRecipe =
        createValidatorFor<{| title: string; imageUrl: Option<string>; description: Option<string>; notes: Option<string> |}>
            () {
            validate (fun r -> r.title)
                [ isNotNull
                  isNotEmptyOrWhitespace
                  hasMaxLengthOf 100 ]
            validateUnrequired (fun r -> r.imageUrl)
                [ isNotEmptyOrWhitespace
                  hasMaxLengthOf 254 ]
            validateUnrequired (fun r -> r.description)
                [ isNotNull
                  isNotEmptyOrWhitespace
                  hasMaxLengthOf 500 ]
            validateUnrequired (fun r -> r.notes)
                [ isNotNull
                  isNotEmptyOrWhitespace
                  hasMaxLengthOf 240 ]
        }
