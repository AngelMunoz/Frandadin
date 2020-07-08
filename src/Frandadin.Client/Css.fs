namespace Frandadin.Client

module internal Css =
    open Zanaptak.TypedCssClasses

    type Spectre = CssClasses<"https://unpkg.com/spectre.css/dist/spectre.min.css", Naming.CamelCase>

    type SpectreExp = CssClasses<"https://unpkg.com/spectre.css/dist/spectre-exp.min.css", Naming.CamelCase>

    type SpectreIcons = CssClasses<"https://unpkg.com/spectre.css/dist/spectre-icons.min.css", Naming.CamelCase>

    type css = CssClasses<"./wwwroot/css/index.css", Naming.CamelCase>
