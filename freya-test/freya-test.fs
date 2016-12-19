module FreyaTest

open Freya.Core
open Freya.Machines.Http
open Freya.Machines.Http.Cors
open Freya.Routers.Uri.Template
open Freya.Types.Http
open System
open System.Text
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
    return! Freya.Optic.set (Freya.Optics.Http.Response.header_ "MyHeader") (Some "SomeValue")
  }

let encodeTextAsJson (t: String) : Representation =
  let json = sprintf "{\"message\":\"%s\"}" t
  { Data = Encoding.UTF8.GetBytes json
    Description =
    { Charset = Some Charset.Utf8
      Encodings = Some [ContentCoding "identity"]
      MediaType = Some MediaType.Json
      Languages = None } }

let addressPerson address =
  freya {
    do! responseHeader
    let! name = name

    return encodeTextAsJson (sprintf "%s %s!" address name)
  }

let unauth =
  freya {
    return Represent.text (sprintf "Unauthorized")
  }

let checkAuthorized =
  freya {
    return true;
  }
  //let isAuthorized h = "alakazam".Equals(h)
  //freya {
  //  let! authHeader = Freya.Optic.get (Freya.Optics.Http.Request.header_ "Authorization")
  //  match authHeader with
  //  | Some h -> return isAuthorized h
  //  | None -> return false
  //}

let authenticatedMachine f =
  freyaMachine {
    cors
    authorized checkAuthorized
    handleOk f
    handleUnauthorized unauth
  }

let router =
  freyaRouter {
    resource "/hello{/name}" ("Hello" |> addressPerson |> authenticatedMachine)
    resource "/goodbye{/name}" ("Goodbye" |> addressPerson |> authenticatedMachine)
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
