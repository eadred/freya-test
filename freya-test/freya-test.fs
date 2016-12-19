module FreyaTest

open Freya.Core
open Freya.Machines.Http
open Freya.Machines.Http.Cors
open Freya.Routers.Uri.Template
open Freya.Types.Http
open System
open System.Text
open Microsoft.Owin.Hosting
open Chiron
open Chiron.Operators

// See https://neoeinstein.github.io/blog/2015/12-13-chiron-json-ducks-monads/index.html
// and https://neoeinstein.github.io/blog/2016/04-02-chiron-computation-expressions/index.html
type Message =
  { Message: String
    Importance: int }

  static member ToJson (m: Message) =
    json {
      do! Json.write "message" m.Message
      do! Json.write "importance" m.Importance
    }

  static member FromJson (_ : Message) =
    json {
      let! m = Json.read "message"
      let! i = Json.read "importance"
      return {Message = m; Importance = i }
    }

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

let encodeTextAsMessage (t: String) : Representation =
  { Data = { Message = t; Importance = 10 }
           |> Json.serialize
           |> Json.formatWith JsonFormattingOptions.Pretty
           |> Encoding.UTF8.GetBytes
    Description =
    { Charset = Some Charset.Utf8
      Encodings = Some [ContentCoding "identity"]
      MediaType = Some MediaType.Json
      Languages = None } }

let addressPerson address =
  freya {
    do! responseHeader
    let! name = name

    return encodeTextAsMessage (sprintf "%s %s!" address name)
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
