module Domain

open System
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

let fromUnixTimestamp secs =
    let origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
    let time = origin.AddSeconds secs
    let zone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time")
    TimeZoneInfo.ConvertTime(time, zone).ToString(dateTimeFormat)

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