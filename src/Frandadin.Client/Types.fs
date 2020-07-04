namespace Frandadin.Client

module Types =

    type ImageUrl = ImageUrl of string

    type Ingredient = 
        { id: int
          name: string 
          quantity: string }

    type RecipeStep = 
        { id: int
          order: int
          directions: string
          imageUrl: Option<ImageUrl> }

    type Recipe = 
        { id: int
          userid: int
          title: string 
          imageUrl: Option<ImageUrl>
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
        { user: User
          token: string }

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

