namespace Frandadin.Client

module Validators =
    open System.Text.RegularExpressions
    open AccidentalFish.FSharp.Validation
    open Types

    let validateSignup = 
        createValidatorFor<SignUpPayload>()
            { validate (fun sp -> sp.name) [
                  isNotEmptyOrWhitespace
                  isNotNull
                  hasMaxLengthOf 100
              ]
              validate (fun sp -> sp.lastName) [
                  isNotEmptyOrWhitespace
                  isNotNull
                  hasMaxLengthOf 100
              ]
              validate (fun sp -> sp.email) [
                  isNotEmptyOrWhitespace
                  isNotNull
                  hasMaxLengthOf 100
              ]
              validate (fun sp -> sp.password) [
                  isNotEmptyOrWhitespace
                  isNotNull
                  hasMaxLengthOf 100
                  withFunction
                    (fun p -> 
                        match Regex.IsMatch(p, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,30}$") with 
                        | true -> Ok
                        | false -> 
                            Errors(
                                [{ errorCode = "Invalid password"
                                   message = "This password needs to contain A Lower case A Upper case letters and at least 8 characters"
                                   property = "password" }
                                ])
                    )
              ]
            }
    
    let validateLogin =
        createValidatorFor<LoginPayload>()
            { validate (fun lp -> lp.email)  [
                isNotEmptyOrWhitespace
                isNotNull
                hasMaxLengthOf 100
              ]
              validate (fun lp -> lp.password) [
                isNotEmptyOrWhitespace
                isNotNull
                hasMaxLengthOf 100
              ]
            }
