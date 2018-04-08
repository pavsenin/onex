module Domain

open System.Net
open System
open System.IO
open FSharp.Data
open FSharp.Data.JsonExtensions
open Newtonsoft.Json
open System.Text
open Utils
open System.Globalization

type BetType =
    | UnBet
    | P1 | X | P2 
    | D1X | D12 | DX2
    | TG of float
    | TL of float
    | IT1G of float
    | IT1L of float
    | IT2G of float
    | IT2L of float

type GameType =
    | UnGame
    | X12 | DX12
    | Total | IndTotal1 | IndTotal2

let dateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff"
let dateFormat = "yyyy-MM-dd"
let floatFormat = NumberFormatInfo(NumberDecimalSeparator=".")

let chl, eul, rfpl, apl, bundes, primera, serieA, fl1 =
    "UEFA-Champions-League", "UEFA-Europa-League", "Russia-Premier-League", "England-Premier-League",
    "Germany-Bundesliga", "Spain-Primera-Divisin", "Italy-Serie-A", "France-Ligue-1"
let champs =
    [rfpl, 225733; apl, 88637; bundes, 96463; primera, 127733; serieA, 110163; fl1, 12821; chl, 118587; eul, 118593]

let buildLigaUrl id =
    """https://1xstavka.ru/LineFeed/Get1x2_Zip?champs=""" +
    id.ToString() +
    """&count=50&tf=1000000&antisports=38&mode=4&country=1&partner=51"""
let buildMatchUrl id =
    """https://1xstavka.ru/LineFeed/GetGameZip?id=""" +
    id.ToString() +
    """&lng=ru&cfview=0&isSubGames=true&GroupEvents=true&countevents=250&partner=51&grMode=2"""
let getUrl id = buildLigaUrl id
let fetchContent adustRequest url =
    let req = url |> Uri |> WebRequest.Create
    adustRequest req
    use resp = req.GetResponse()
    use stream = resp.GetResponseStream()
    use reader = new StreamReader(stream)
    reader.ReadToEnd()
let fetchContentGet = fetchContent (fun _ -> ())

let fromUnixTimestamp secs =
    let origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
    let time = origin.AddSeconds secs
    let zone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time")
    TimeZoneInfo.ConvertTime(time, zone).ToString(dateTimeFormat)

let asF (jsonValue:JsonValue) = jsonValue.AsFloat()

type TreeItemViewModel = { text : string; children : TreeItemViewModel list }

let betTypeToString = function
    | P1 -> "Win1" 
    | X  -> "Draw"
    | P2 -> "Win2"
    | D1X -> "Win1+Draw"
    | D12 -> "Win1+Win2"
    | DX2 -> "Draw+Win2"
    | TG param -> sprintf "Total Great(%f)" param
    | TL param -> sprintf "Total Less(%f)" param
    | IT1G param -> sprintf "IndTotal1 Great(%f)" param
    | IT1L param -> sprintf "IndTotal1 Less(%f)" param
    | IT2G param -> sprintf "IndTotal2 Great(%f)" param
    | IT2L param -> sprintf "IndTotal2 Less(%f)" param
    | _ -> "Unknown"

let gameTypeToString = function
    | X12 -> "Outcome"
    | DX12 -> "DoubleChance"
    | Total -> "Total"
    | IndTotal1 -> "IndividualTotal1"
    | IndTotal2 -> "IndividualTotal2"
    | _ -> "Unknown"

let toBetViewModel betType bet =
    let title = sprintf "%s : %f" (betTypeToString betType) bet
    { text = title; children = [] }

let toGameViewModel gameType bets =
    let title = gameTypeToString gameType |> sprintf "%s"
    let children = bets |> List.map (fun (betType, bet) -> toBetViewModel betType bet)
    { text = title; children = children }

let toMatchViewModel (id, (id1, name1), (id2, name2), time, games) =
    let title = sprintf "(%d) %s(%d) - %s(%d) (%A)" id name1 id1 name2 id2 time
    let children = games |> List.map (fun (gameType, bets) -> toGameViewModel gameType bets)
    { text = title; children = children }

let toLeagueViewModel id name matches =
    let title = sprintf "(%d) %s" id name
    { text = title; children = matches |> List.map toMatchViewModel }

let toBetType param _type =
    let toBet v = map (fun f -> v f) UnBet
    match _type with
    | 1 -> P1 | 2 -> X | 3 -> P2
    | 4 -> D1X | 5 -> D12 | 6 -> DX2
    | 9 -> toBet TG param | 10 -> toBet TL param
    | 11 -> toBet IT1G param | 12 -> toBet IT1L param
    | 13 -> toBet IT2G param | 14 -> toBet IT2L param
    | _ -> UnBet

let toGameType _type =
    match _type with
    | 1 -> X12 | 2 -> DX12
    | 4 -> Total
    | 5 -> IndTotal1
    | 6 -> IndTotal2
    | _ -> UnGame

let getBets (bet:JsonValue) =
    bet.AsArray()
    |> Array.choose (fun b ->
        let c = b?C |> asF
        let _type = b?T.AsInteger()
        let param = b.TryGetProperty("P") |>> asF
        match toBetType param _type with
        | UnBet -> None
        | betType -> Some (betType, c)
    )

let getGame k =
    let _type = k?G.AsInteger()
    match toGameType _type with
    | UnGame -> None
    | gameType ->
        let bets =
            k?E.AsArray()
            |> Array.collect getBets
            |> Array.toList
        Some (gameType, bets)

let getMatch m =
    let id = m?CI.AsInteger()
    let team1 = (m?O1I.AsInteger(), m?O1E.AsString())
    let team2 = (m?O2I.AsInteger(), m?O2E.AsString())
    let seconds = m?S.AsFloat()
    let time = fromUnixTimestamp seconds
    let jsonContent =
        id
        |> buildMatchUrl
        |> fetchContentGet
        |> JsonValue.Parse
    let games =
        jsonContent?Value?GE.AsArray()
        |> Array.choose getGame
        |> Array.toList
    (id, team1, team2, time, games)

let getMatches json =
    json?Value.AsArray()
    |> Array.map getMatch
    |> Array.toList

let getLeague (name, id) =
    let matches =
        id
        |> getUrl
        |> fetchContentGet
        |> JsonValue.Parse
        |> getMatches
    (id, name, matches)

let getResults date =
    let url = "https://1xstavka.ru/getTranslate/ViewGameResultsGroup"
    let adjustRequest (request:WebRequest) =
        request.Method <- "POST"
        let postData = """{"Language":"ru"}{"Params":[""" + date + """, null, null, null, null, 180]}{"Vers":6}{"Adult": false}{"partner":51}"""
        let byteArray = Encoding.UTF8.GetBytes(postData)
        request.ContentType <- "application/json"
        request.ContentLength <- int64(byteArray.Length)
        use dataStream = request.GetRequestStream()
        dataStream.Write(byteArray, 0, byteArray.Length)
    fetchContent adjustRequest url

let isCorrectTeam (name:string) =
    (name.StartsWith("Home (") && name.EndsWith(")")) ||
    (name.StartsWith("Away (") && name.EndsWith(")"))
    |> not

let filterTeams teams = teams |> List.filter (fun (_, (name:string)) -> isCorrectTeam name)