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
  member __.Configure () =
    OwinAppFunc.ofFreya router

[<EntryPoint>]
let main _ =
  Console.WriteLine "Starting"
  WebApp.Start<HelloWorld> "http://localhost;7000" |> ignore
  Console.ReadLine () |> ignore
  0
