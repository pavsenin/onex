module API

open Domain
open DBAdapter

let receiveAndInsertBets time =
    let leagues = champs |> List.map getLeague

    let (teams, matches, bets) =
        leagues |> List.fold (fun response (id, _, matches) -> toLeagueResponse id time matches response) ([], [], [])

    teams |> filterTeams |> insertNewTeams |> ignore
    matches |> insertNewMatches |> ignore
    bets |> insertNewBets |> ignore