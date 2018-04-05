module API

open Utils
open Domain
open DBAdapter

let receive time =
    champs
    |> List.map getLeague
    |> List.fold (fun response (id, _, matches) -> toLeagueResponse id time matches response) ([], [], [])

let receiveAndInsertBets connectionString time =
    let (teams, matches, bets) = receive time
    connectionStrings.["Onex"] <- connectionString
    teams |> filterTeams |> insertNewTeams |> ignore
    matches |> insertNewMatches |> ignore
    bets |> insertNewBets |> ignore