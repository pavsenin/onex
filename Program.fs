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

let chl, eul, rfpl, apl, bundes, primera, serieA, fl1 =
    "UEFA-Champions-League", "UEFA-Europa-League", "Russia-Premier-League", "England-Premier-League",
    "Germany-Bundesliga", "Spain-Primera-Divisin", "Italy-Serie-A", "France-Ligue-1"
let champs =
    [chl, 118587; eul, 118593; rfpl, 225733; apl, 88637; bundes, 96463; primera, 127733; serieA, 110163; fl1, 12821]

let buildLigaUrl id =
    """https://1xstavka.ru/LineFeed/Get1x2_Zip?champs=""" +
    id.ToString() +
    """&count=50&tf=1000000&antisports=38&mode=4&country=1&partner=51"""
let buildMatchUrl id =
    """https://1xstavka.ru/LineFeed/GetGameZip?id=""" +
    id.ToString() +
    """&lng=ru&cfview=0&isSubGames=true&GroupEvents=true&countevents=250&partner=51&grMode=2"""
let getUrl id = buildLigaUrl id
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

type BetType =
    | Un
    | P1 | X | P2 
    | D1X | D12 | DX2
    | TG of float
    | TL of float
    | IT1G of float
    | IT1L of float
    | IT2G of float
    | IT2L of float
    
type TreeItemViewModel = { text : string; children : TreeItemViewModel list }

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
    let title = sprintf "%s : %f" (betTypeToString betType) bet
    { text = title; children = [] }

let toMatchViewModel (_, name1, name2, time, bets) =
    let title = sprintf "%s - %s (%A)" name1 name2 time
    let children = bets |> List.map (fun (betType, bet) -> toBetViewModel betType bet)
    { text = title; children = children }

let toLeagueViewModel name matches =
    { text = name; children = matches }

let toBetType param _type =
    let toBet v = map (fun f -> v f) Un
    match _type with
    | 1 -> P1 | 2 -> X | 3 -> P2
    | 4 -> D1X | 5 -> D12 | 6 -> DX2
    | 9 -> toBet TG param | 10 -> toBet TL param
    | 11 -> toBet IT1G param | 12 -> toBet IT1L param
    | 13 -> toBet IT2G param | 14 -> toBet IT2L param
    | _ -> Un

let getBet k =
    let bet = k?C.AsFloat()
    let _type = k?T.AsInteger()
    let param = k.TryGetProperty("P") |>> asF
    let betType = toBetType param _type
    match betType with
    | Un -> None
    | _ -> Some (betType, bet)
let getMatch m =
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
    let bets =
        jsonContent?Value?E.AsArray()
        |> Array.choose getBet
        |> Array.toList
    (id, name1, name2, time, bets)

let getMatches json =
    json?Value.AsArray()
    |> Array.map getMatch
    |> Array.toList
    |> List.map toMatchViewModel

let getLeague (name, id) =
    id
    |> getUrl
    |> fetchContent
    |> JsonValue.Parse
    |> getMatches
    |> toLeagueViewModel name


[<EntryPoint>]
let main argv =
    let jsonData = 
        champs
        |> List.map getLeague
        |> JsonConvert.SerializeObject
        |> sprintf "var json_data = %s;\r\n"
    use sw = new StreamWriter(path="data.js", append=false, encoding=Encoding.UTF8)
    sw.Write(jsonData)
    0