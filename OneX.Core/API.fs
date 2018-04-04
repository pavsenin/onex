module API

open Domain
open DBAdapter

let receiveAndInsertBets connectionString time =
    let leagues = champs |> List.map getLeague

    let (teams, matches, bets) =
        leagues |> List.fold (fun response (id, _, matches) -> toLeagueResponse id time matches response) ([], [], [])
    
    connectionStrings.["Onex"] <- connectionString

    bets |> insertNewBets |> ignore
    matches |> insertNewMatches |> ignore
    teams |> filterTeams |> insertNewTeams |> ignore