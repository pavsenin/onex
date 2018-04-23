namespace OneX.Core

open Onex
open Utils
open Domain
open DBAdapter
open FSharp.Data
open FSharp.Data.JsonExtensions
open FSharp.Data.HtmlAttribute
open System
open System.Linq

module API =

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

    let getMatchResult lid (json:JsonValue) =
        json.TryGetProperty("Head")
        ||> (fun head ->
            let scoreData = head.AsArray()
            let correctData =
                if scoreData.[22].AsString() |> isEmpty then None
                else if scoreData.[23].AsString() |> isEmpty then None
                else if scoreData.[33].AsString() |> isCorrectTeam |> not then None
                else if scoreData.[34].AsString() |> isCorrectTeam |> not then None
                else Some scoreData
            correctData ||> (fun scoreData ->
                let scoreString = scoreData.[6].AsString()
                let scoreOpt = getScoreAdv scoreString
                scoreOpt |>> (fun score ->
                    let team1ID = scoreData.[22].AsInteger()
                    let team2ID = scoreData.[23].AsInteger()
                    let seconds = scoreData.[7].AsFloat()
                    let time = fromUnixTimestamp seconds
                    (lid, team1ID, team2ID, time, score)
                )
            )
        )

    let getLeagueResult json =
        let id = json?ID.AsInteger()
        json?Elems.AsArray() |> Array.choose (getMatchResult id)

    let receiveBets time =
        leagues
        |> List.map getLeague
        |> List.fold (fun response (id, _, matches) -> toLeagueResponse id time matches response) ([], [], [])

    let receiveScores date =
        let jsonResults = getResults date |> JsonValue.Parse
        let footballElems = jsonResults?Data.AsArray() |> Array.find (fun i -> i?ID.AsInteger() = 1)
        footballElems?Elems.AsArray()
        |> Array.filter (fun l ->
            let lID = l?ID.AsInteger()
            leagues |> List.tryFind (fun (_, id) -> lID = id) |> Option.isSome
        )
        |> Array.map getLeagueResult
        |> Array.concat
        |> Array.toList

    let receiveAndInsertResults connectionString date =
        let scores = receiveScores date

        connectionStrings.["Onex"] <- connectionString

        scores |> insertNewScores

    let receiveAndInsertBets connectionString time =
        let (teams, matches, bets) = receiveBets time

        connectionStrings.["Onex"] <- connectionString

        teams |> filterTeams |> insertNewTeams |> ignore
        matches |> insertNewMatches |> ignore
        bets |> insertNewBets |> ignore