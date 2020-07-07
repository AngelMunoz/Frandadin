namespace Frandadin.Client

module Types =

    type Ingredient =
        { id: int
          recipeid: int
          name: string
          quantity: string }

    type RecipeStep =
        { id: int
          recipeid: int
          order: int
          directions: string
          imageUrl: Option<string> }

    type Recipe =
        { id: int
          userid: int
          title: string
          imageUrl: Option<string>
          description: Option<string>
          notes: Option<string>
          ingredients: list<Ingredient>
          steps: list<RecipeStep> }

    type User =
        { id: int
          email: string
          name: string
          lastName: string }

    type AuthResponse =
        { user: User }

    type LoginPayload =
        { email: string
          password: string }

    type SignUpPayload =
        { email: string
          password: string
          name: string
          lastName: string }

    type ErrorResponse =
        { message: string
          code: Option<int>
          errors: list<obj> }

    type PaginationResult<'T> =
        { count: int
          list: list<'T> }

    type PaginationParams =
        { page: int
          limit: int }
