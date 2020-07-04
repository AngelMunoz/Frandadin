namespace Frandadin.Client


module Views =
    open Bolero

    type Shell = Template<"views/shell.html">
    type Home = Template<"views/home.html">
    type Auth = Template<"views/auth.html">
    type Recipe = Template<"views/recipe.html">
    type Recipes = Template<"views/recipes.html">
    type Notes = Template<"views/notes.html">