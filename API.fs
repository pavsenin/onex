module API

open Domain
open DBAdapter

let receiveAndInsertBets time =
    let leagues = champs |> List.map getLeague

    let (teams, matches, bets) =
        leagues |> List.fold (fun response (id, _, matches) -> toLeagueResponse id time matches response) ([], [], [])

    let x1 = teams |> filterTeams |> insertNewTeams
    let x2 = matches |> insertNewMatches
    let x3 = bets |> insertNewBets
    ()