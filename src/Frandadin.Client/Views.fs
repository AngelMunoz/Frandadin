namespace Frandadin.Client


module Views =
    open Bolero

    type Shell = Template<"wwwroot/views/shell.html">
    type Home = Template<"wwwroot/views/home.html">
    type Auth = Template<"wwwroot/views/auth.html">
    type Recipe = Template<"wwwroot/views/recipe.html">
    type Recipes = Template<"wwwroot/views/recipes.html">
    type Notes = Template<"wwwroot/views/notes.html">