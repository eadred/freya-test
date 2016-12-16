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

let hello =
  freya {
    do! Freya.Optic.set (Freya.Optics.Http.Response.header_ "MyHeader") (Some "Hello")
    let! name = name

    return Represent.text (sprintf "Hello %s!" name)
  }

let machine =
  freyaMachine {
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
