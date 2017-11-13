open System.Net
open System
open System.IO
open FSharp.Data
open FSharp.Data.JsonExtensions
open Newtonsoft.Json.FSharp
open Newtonsoft.Json
open System.Text


let inline (|>>) x f = x |> Option.map f
let inline (|?) def arg = defaultArg arg def
let inline map f d = function | Some v -> f v | None -> d

let rfpl = "Russia-Premier-League"
let champsIds = dict[rfpl, 225733]

let buildLigaUrl id =
    """https://1xstavka.ru/LineFeed/Get1x2_Zip?champs=""" +
    id.ToString() +
    """&count=50&tf=1000000&antisports=38&mode=4&country=1&partner=51"""
let buildMatchUrl id =
    """https://1xstavka.ru/LineFeed/GetGameZip?id=""" +
    id.ToString() +
    """&lng=ru&cfview=0&isSubGames=true&GroupEvents=true&countevents=250&partner=51&grMode=2"""
let getUrl name =
    match champsIds.TryGetValue name with
    | true, id -> Some (buildLigaUrl id)
    | _ -> None
let fetchContent url =
    let req = url |> Uri |> WebRequest.Create
    use resp = req.GetResponse()
    use stream = resp.GetResponseStream()
    use reader = new StreamReader(stream)
    reader.ReadToEnd()
let fromUnixTimestamp secs =
    let origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
    let time = origin.AddSeconds secs
    let zone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time")
    TimeZoneInfo.ConvertTime(time, zone)
let asF (jsonValue:JsonValue) = jsonValue.AsFloat()

type CoefType =
    | Un
    | P1 | X | P2 
    | D1X | D12 | DX2
    | TG of float
    | TL of float
    | IT1G of float
    | IT1L of float
    | IT2G of float
    | IT2L of float
    
type TreeItemViewModel = { text : string; children : string list }

let betTypeToString = function
    | P1 -> "Победа1" 
    | X  -> "Ничья"
    | P2 -> "Победа2"
    | D1X -> "Победа1+Ничья"
    | D12 -> "Победа1+Победа2"
    | DX2 -> "Ничья+Победа2"
    | TG param -> sprintf "Тотал Больше(%f)" param
    | TL param -> sprintf "Тотал Меньше(%f)" param
    | IT1G param -> sprintf "Инд.Тотал1 Больше(%f)" param
    | IT1L param -> sprintf "Инд.Тотал1 Меньше(%f)" param
    | IT2G param -> sprintf "Инд.Тотал2 Больше(%f)" param
    | IT2L param -> sprintf "Инд.Тотал2 Меньше(%f)" param
    | _ -> "Неизвестно"
let toBetViewModel betType bet =
    sprintf "%s : %f" (betTypeToString betType) bet

let toMatchViewModel (_, name1, name2, time, bets) =
    let title = sprintf "%s - %s (%A)" name1 name2 time
    let children = bets |> List.map (fun (betType, bet) -> toBetViewModel betType bet)
    { text = title; children = children }

let toCoefType param _type =
    let toCoef v = map (fun f -> v f) Un
    match _type with
    | 1 -> P1 | 2 -> X | 3 -> P2
    | 4 -> D1X | 5 -> D12 | 6 -> DX2
    | 9 -> toCoef TG param | 10 -> toCoef TL param
    | 11 -> toCoef IT1G param | 12 -> toCoef IT1L param
    | 13 -> toCoef IT2G param | 14 -> toCoef IT2L param
    | _ -> Un

[<EntryPoint>]
let main argv =
    let jsonContent =
        rfpl
        |> getUrl
        |>> fetchContent
        |>> JsonValue.Parse
    match jsonContent with
    | None -> 0
    | Some json ->
        let matches = 
            json?Value.AsArray()
            |> Array.map (fun m ->
                let id = m?CI.AsInteger()
                let name1 = m?O1.AsString()
                let name2 = m?O2.AsString()
                let seconds = m?S.AsFloat()
                let time = fromUnixTimestamp seconds
                let jsonContent =
                    id
                    |> buildMatchUrl
                    |> fetchContent
                    |> JsonValue.Parse
                let coefs =
                    jsonContent?Value?E.AsArray()
                    |> Array.choose (fun k ->
                        let coef = k?C.AsFloat()
                        let _type = k?T.AsInteger()
                        let param = k.TryGetProperty("P") |>> asF
                        let coefType = toCoefType param _type
                        match coefType with
                        | Un -> None
                        | _ -> Some (coefType, coef)
                    )
                    |> Array.toList
                (id, name1, name2, time, coefs)
            )
            |> Array.toList
        let viewModels = matches |> List.map toMatchViewModel
        let serialized = JsonConvert.SerializeObject(viewModels)
        let corrected = sprintf "var json_data = %s;" serialized
        let filePath = "data.js"
        use sw = new StreamWriter(path=filePath, append=false, encoding=Encoding.UTF8)
        sw.Write(corrected)
        0