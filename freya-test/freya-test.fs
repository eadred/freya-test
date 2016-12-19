module FreyaTest

open Freya.Core
open Freya.Machines.Http
open Freya.Routers.Uri.Template
open System
open Microsoft.Owin.Hosting

let name =
  freya {
    let! name = Freya.Optic.get (Route.atom_ "name")

    match name with
    | Some name -> return name
    | _ -> return "World"
  }

let responseHeader =
  freya {
    return! Freya.Optic.set (Freya.Optics.Http.Response.header_ "MyHeader") (Some "Hello")
  }

let hello =
  freya {
    do! responseHeader
    let! name = name

    return Represent.text (sprintf "Hello %s!" name)
  }

let checkAuthorized =
  let isAuthorized h = "alakazam".Equals(h)
  freya {
    let! authHeader = Freya.Optic.get (Freya.Optics.Http.Request.header_ "Authorization")
    match authHeader with
    | Some h -> return isAuthorized h
    | None -> return false
  }

let machine =
  freyaMachine {
    authorized checkAuthorized
    handleOk hello
  }

let router =
  freyaRouter {
    resource "/hello{/name}" machine
  }

type HelloWorld () =
  member __.Configuration () =
    OwinAppFunc.ofFreya router

[<EntryPoint>]
let main _ =
  Console.WriteLine "Starting"
  use app = WebApp.Start<HelloWorld> ("http://localhost:7000")
  Console.ReadLine () |> ignore
  0
