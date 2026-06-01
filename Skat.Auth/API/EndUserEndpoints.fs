module AuthAPI

open Domain
open Fabulous
open System.Net.Http
open System.Text.Json
open System.Text

let register (username: string, password: string) =
    Cmd.ofTaskMsg (task {
            use client = new HttpClient()
            let payload =
                JsonSerializer.Serialize({| username = username; password = password |})
            let content = new StringContent(payload, Encoding.UTF8, "application/json")
            let! response = client.PostAsync("http://localhost:5284/register", content)
            let! body = response.Content.ReadAsStringAsync()
            if response.IsSuccessStatusCode then
                let token = JsonSerializer.Deserialize<{| token: string |}>(body)
                let tokenGuid =
                    match System.Guid.TryParse(token.token) with
                    | true, guid -> guid
                    | false, _ -> failwith "Invalid token format"
                return LoginSuccess { Id = tokenGuid; Username = username; PasswordHash = "hashed-password" }
            else
                let error = JsonSerializer.Deserialize<{| error: string |}>(body)
                printfn "Registration error: %s" error.error
                return LoginError error.error
    })

let login (username: string, password: string) =
    //Cmd.ofAsyncMsg (async {
    Cmd.ofTaskMsg (task {
            use client = new HttpClient()
            let payload =
                JsonSerializer.Serialize({| username = username; password = password |})

            let content = new StringContent(payload, Encoding.UTF8, "application/json")

            let! response = client.PostAsync("http://localhost:5284/login", content)
            let! body = response.Content.ReadAsStringAsync()
            if response.IsSuccessStatusCode then
                let user = JsonSerializer.Deserialize<{| user: {| id: string; username: string; passwordHash: string |} |}>(body)
                let tokenGuid =
                    match System.Guid.TryParse(user.user.id) with
                    | true, guid -> guid
                    | false, _ -> failwith "Invalid token format"
                //let user = { Id = tokenGuid; Username = user.username; PasswordHash = user.passwordHash }
                let user = { Id = tokenGuid; Username = user.user.username; PasswordHash = user.user.passwordHash }
                printfn "Login successful: %A" user
                return LoginSuccess user
            else
                let error = JsonSerializer.Deserialize<{| error: string |}>(body)
                printfn "Login error: %s" error.error
                return LoginError error.error
        //if username = "admin" && password = "password" then
            //let user = { Id = System.Guid.NewGuid(); Username = username; PasswordHash = "hashed-password" }
            //return LoginSuccess user
        //else
        //    return LoginError "Invalid username or password"
    })