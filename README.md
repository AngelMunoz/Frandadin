# Frandadin
[Recetron]: https://github.com/AngelMunoz/Recetron
[Bolero]: https://fsbolero.io
[Avalonia.FuncUI]: https://github.com/AvaloniaCommunity/Avalonia.FuncUI
[Elmish]: https://elmish.github.io/
[dev.to]: https://dev.to/tunaxor

[Pages]: https://github.com/AngelMunoz/Recetron/tree/master/Recetron/Pages
[Shared]: https://github.com/AngelMunoz/Recetron/tree/master/Recetron/Shared
[Auth]: https://github.com/AngelMunoz/Frandadin/blob/master/src/Frandadin.Client/Pages/Auth.fs
[RecipeForm]: https://github.com/AngelMunoz/Frandadin/blob/master/src/Frandadin.Client/Components/RecipeForm.fs
[Main]: https://github.com/AngelMunoz/Frandadin/blob/master/src/Frandadin.Client/Main.fs#L97-L123

You may have seen [Recetron] a Full-Stack C# project composed of a Blazor web app and an asp.net core web API this time I worked on a slightly different (yet with the same goals in mind) stack. **Frandadin** is a Full-Stack F# project composed of a server-side rendered [Bolero] App powered by asp.net core. I have some posts about [Elmish] on my [dev.to] profile although with [Avalonia.FuncUI] but the experience is consistent with this one, so I'll try to explain the differences and my thoughts while working on these two projects.

> This is still WIP but I have settled my opinions on both projects I don't expect to finish this project but I will try to finish it


## Differences

Out of the bat there are some differences which affect how the code is written and what is expected from each project

| Feature | Recetron | Frandadin |
| ------- | ---------| --------- |
| Language| C# | F# |
| SSR | ❎ | ✅ |
| Remoting | ❎ | ✅ |
| PostgreSQL | ❎ | ✅ |
| Cookies | ❎ | ✅ |
| JWT | ✅ | ❎ |
| HttpClient | ✅ | ❎ |
| MongoDB | ✅ | ❎ |


### Client

Blazor when it's server-side rendered its really fast! you can't notice it almost it is actually pretty nice to see while the Standalone SPA version is somewhat similar in rendering times of those other major frameworks so there is not a lot of difference here just what you would expect, also this doesn't affect your ability to code in one way or another you won't even feel the difference (at least I didn't)

### Auth 
using JWT vs Cookies is not the point here I'll just say that JWT requires a bit of more code on the client-side to ensure you're using a token that at least has a valid lifetime and that's something you always need to check in the backend. Using cookies was actually not bad, Bolero adds some extension helpers that helo you quite nicely to prevent that JWT extra code that you would have to have done for Cookies as well (but this time in the backend) both approaches are good, but it feels more natural using Bolero

### Database Access
F# has some friction with the MongoDB driver so I chose to go for NPGSQL + NpgSQL.FSharp which make it a breeze to work with having a relational database though requires more care in the backend with your types and models. Overall I felt it was easier to use C# + MongoDB but there are other factors you must take into account as well when choosing a database which is not something I will discuss here.

### Http
HttpClient vs Remoting...
This is one of the clear differences client-side speaking. Since the C# version is a full SPA application, that means even if I'm sharing Interfaces between the client and the backend I will have to code twice my services one for the backend (API <-> Database) and one for the frontend (Client <-> API ) and do a lot of DI, in the case of Remoting it was easier since I just needed to declare a type in my client code and then implement it on the backend and it was ready to use both front-end, back-end. Overall I preferred the remoting aspect of Bolero



### Components
This is where things are radically different. the use the same framework yet they are programmed in radically different ways

---
#### C#
For Blazor you need to use `.razor` files which is somewhat a mix of C#/Html It's not bad though I feel it helps you to "encapsulate" (mentally speaking) the code you are writing since it's pretty much obvious that the code you are writing is precisely for the HTML above.
A typical C# component will look like this

```razor
<!-- GreetGeneral -->
<div>
    <h1>Hello there!</h1>
    <button @onclick=@(_ = OnClick())>General @Name!</button>
</div>
@code {
    private string name = "Tux";

    [Parameter]
    public string Name { get => name; set => name = value; }

    private void OnClick() {
        Console.WriteLine("Meme Intensifies");
    }
}

<!-- Another Component -->
<div>
  <GreetGeneral Name="Kenobi"/>
</div>
```

You can check both [Pages] and [Shared] to have more code samples but overall it fits what you will see in different UI frameworks Components and more components with the rule of thumb

> Props (parameters) in Events (EventCallbacks) out

this will enforce you A one-way data binding which will make easier to reason about the data flow.

#### F#
Bolero uses the MVU architecture which is some times quite verbose but it has it's advantages it forces you to follow a predictable pattern to ensure your UI works as expected (which easily Unit testable by the way) I think F# has the upper hand in this aspect


One of the things I like that I have been tweeting recently is that Bolero components can be used in two ways

1. ElmishComponents inside A Parent-Child Hierarchy
2. Standalone ProgramComponents which are independent

And depending on your usage you are still enforcing correctness in your UI since even your UI is typed.

You can check [Auth] and [RecipeForm]  for examples on this.
both approaches (one is a component the other is part of the Parent-Child Hierarchy) can be seen in [Main] and how they interact with the rest of the main MVU loop.


Since Bolero Components are also Blazor components you can also use Fragments, Parameters, CascadingParameters, DI, JS and anything you can use in Blazor as well but with the benefits the language brings with it



## Dev Experience
What does it felt to me when working on both projects?

C# fits a traditional way to architecture Front-end Applications in a similar way you could do it with Angular and that is not a bad thing it's good it's somewhat predictable but it relies on you and your team doing the tings right in the same convention. C# might be faster to iterate in terms of code edit -> compile -> view results but even with `Nullable` types turned on, you are likely to see `NullReferenceException`s everywhere so you will have to add the Fix time to your dev time I think there's a good chance you will like authoring components in C#


F# on the other side has a clear pattern that enforces itself even if there's someone new on your team they won't have much room to make mistakes that are made when you code by convention Bolero provides a nice Elmish Commands that can improve the interop with Blazor like 

- Cmd.ofAuthorized
- Cmd.ofJS
- Cmd.ofAsync (different from the usual ofAsync elmish module)
- Cmd.performRemote 

which reduce some of the boilerplate you would need or the need of creating external functions/services if you use what has been provided to you

Hotreloading is provided for HTML templates as well I tried it without much success some times it worked sometimes it didn't and even then if I need to change non Html Code I still need to restart my server so I don't really care at al of using the Bolero DSL


# Conclusions

Blazor/Bolero feels like a solid choice if you don't need anything from the current JS/TS World and if you are using a backend that is not JS/TS could work nicely as well as a Full SPA.
I was skeptical at first but now that I have played enough and done some work testing features I have needed in other projects like 

- Components
- Intercomponent Communication
- Http Requests
- JWT
- Work with Complex Types
- Routing

I think these are the basic blocks that you would use for building a medium/large application and I feel confortable using these frameworks to build a webapp with hundreds of components

For the correctness though I'd go with F# + Bolero any day
