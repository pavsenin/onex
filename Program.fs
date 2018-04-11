open System

open FSharp.Configuration
open System.IO
open Newtonsoft.Json

type Settings = AppSettings<"App.config">
let connectionString = Settings.ConnectionStrings.Onex

[<EntryPoint>]
let main argv =
    API.receiveAndInsertBets connectionString (DateTime.Now)
    //let x = API.receiveAndInsertResults connectionString "\"2018-04-07\""
    
    (*
    let leagueVMs =
        leagues |> List.map (fun(id, name, matches) -> toLeagueViewModel id name matches)

    let jsonData =
        leagueVMs
        |> JsonConvert.SerializeObject
        |> sprintf "var json_data = %s;\r\n"

    use sw = new StreamWriter(path="data.js", append=false, encoding=Encoding.UTF8)
    sw.Write(jsonData)
    *)
    0