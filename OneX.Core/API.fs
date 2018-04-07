module API

open Utils
open Domain
open DBAdapter
open FSharp.Data
open FSharp.Data.JsonExtensions
open FSharp.Data.HtmlAttribute
open System

let private receive time =
    champs
    |> List.map getLeague
    |> List.fold (fun response (id, _, matches) -> toLeagueResponse id time matches response) ([], [], [])

let receiveAndInsertBets connectionString time =
    let (teams, matches, bets) = receive time
    connectionStrings.["Onex"] <- connectionString
    teams |> filterTeams |> insertNewTeams |> ignore
    matches |> insertNewMatches |> ignore
    bets |> insertNewBets |> ignore

type Score = int * int
type ScoreAdv = Score * Score list

let getScore score =
    let values = score |> splitNoEmpty [|':'|]
    (Int32.Parse(values.[0]), Int32.Parse(values.[1]))

let getScoreAdv scoreAdvString =
    if String.IsNullOrEmpty scoreAdvString then None
    else
        let scoreSplitted = scoreAdvString |> splitNoEmpty [|'('; ')'|]
        let score = scoreSplitted.[0]
        let scoreTimes = scoreSplitted.[1] |> splitNoEmpty [|','|] |> Array.toList
        Some (getScore score, scoreTimes |> List.map getScore)

let getMatchResult (json:JsonValue) =
    json.TryGetProperty("Head")
    ||> (fun head ->
        let scoreData = head.AsArray()
        let scoreString = scoreData.[6].AsString()
        let scoreOpt = getScoreAdv scoreString
        scoreOpt |>> (fun score ->
            let team1ID = scoreData.[22].AsInteger()
            let team2ID = scoreData.[23].AsInteger()
            let seconds = scoreData.[7].AsFloat()
            let time = fromUnixTimestamp seconds
            (team1ID, team2ID, time, score)
        )
    )

let getLeagueResult json =
    let id = json?ID.AsInteger()
    let matches = json?Elems.AsArray() |> Array.choose getMatchResult
    (id, matches)

let receiveAndInsertResults connectionString date =
    let response = getResults date
    let json = response |> JsonValue.Parse
    let data = json?Data.AsArray()
    let footballElems = data |> Array.find (fun i -> i?ID.AsInteger() = 1)
    let allLeagues = footballElems?Elems.AsArray()
    let filteredLeagues = allLeagues |> Array.filter (fun l ->
        let lID = l?ID.AsInteger()
        champs |> List.tryFind (fun (_, id) -> lID = id) |> Option.isSome
    )
    let leagues = filteredLeagues |> Array.map getLeagueResult
    //let results = leagues |> 
    0