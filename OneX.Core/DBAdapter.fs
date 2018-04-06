module DBAdapter

open System.Data.SqlClient
open Utils
open Domain
open System.Collections.Generic
open System

let connectionStrings = Dictionary()

let executeMulti commands exec =
    let connectionString = connectionStrings.["Onex"]
    use conn = new SqlConnection(connectionString)
    try
        try
            conn.Open()
            commands
            |> List.map (fun command ->
                try
                    let cmd = new SqlCommand(command, conn)
                    cmd.CommandTimeout <- 0
                    Success(exec cmd)
                with
                   ex -> Failure ex.Message
            )
        with
            ex -> [Failure ex.Message]
    finally
        conn.Close()

let execute command exec =
    match executeMulti [command] exec with
    | [] -> Failure "Unknown error"
    | h::_ -> h

let executeNonQuery command func =
    execute command (fun cmd -> func(cmd.ExecuteNonQuery()))

let executeReader command func =
    execute command (fun cmd -> func(cmd.ExecuteReader()))

let executeNonQueryMulti commands func =
    commands |> mapl (fun cmds -> executeMulti cmds (fun cmd -> func(cmd.ExecuteNonQuery())))

type DBTeam = int * string
type DBMatch = int * int * int * int * string
type DBBet = int * int * float option * float * string
type LeagueResponse = DBTeam list * DBMatch list * DBBet list

let toTeamInsertValue (id, name) =
    sprintf "(%d, '%s')" id name

let toMatchInsertValue (id, leagueID, team1ID, team2ID, time) =
    sprintf "(%d, %d, %d, %d, '%s')" id leagueID team1ID team2ID time

let toBetInsertValue (matchID, betID, param, value, received) =
    let paramString = param |>> (fun (p:float) -> p.ToString(floatFormat)) |> defArg "NULL"
    sprintf "(%d, %d, %s, %f, '%s')" matchID betID paramString value received

let toInsertCommand vs map start =
    let values = vs |> Seq.map map
    if Seq.isEmpty values then None
    else
        let valuesString = System.String.Join(", ", values)
        Some (start + valuesString)

let toTeamsInsertCommand teams =
    toInsertCommand teams toTeamInsertValue "INSERT INTO Teams (ID, Name) VALUES "

let toMatchesInsertCommand matches =
    toInsertCommand matches toMatchInsertValue "INSERT INTO Matches (ID, LeagueID, Team1ID, Team2ID, StartedAt) VALUES "

let toBetsInsertCommand bets =
    toInsertCommand bets toBetInsertValue "INSERT INTO Bets (MatchID, BetTypeID, BetParam, Value, ReceivedAt) VALUES "

let teamIDsSelectCommand =
    "SELECT ID FROM Teams"
let matchesIDsSelectCommand =
    "SELECT ID FROM Matches"
let betsInfoSelectCommand =
    """WITH Grouped AS (SELECT MatchID, BetTypeID, BetParam, MAX(ReceivedAt) AS ReceivedAt FROM Bets GROUP BY MatchID, BetTypeID, BetParam)
    SELECT b.MatchID, b.BetTypeID, b.BetParam, b.Value FROM Grouped g INNER JOIN Bets b
    ON g.MatchID = b.MatchID AND g.BetTypeID = b.BetTypeID AND ISNULL(g.BetParam, 0) = ISNULL(b.BetParam, 0) AND g.ReceivedAt = b.ReceivedAt
    INNER JOIN Matches m ON b.MatchID = m.ID
    WHERE m.StartedAt > GETDATE()"""

let getDBIntValues command =
    executeReader command (fun reader -> seq { while reader.Read() do yield reader.GetInt32(0) } |> Seq.toList)
let getDBBetValues command =
    executeReader command (fun reader ->
        seq {
            while reader.Read() do
                let mid = reader.GetInt32(0)
                let bid = reader.GetInt32(1)
                let bp =
                    if reader.IsDBNull(2) then None
                    else Some(reader.GetDouble(2))
                let bv = reader.GetDouble(3)
                yield (mid, bid, bp, bv)
        }
        |> Seq.toList
    )

let insertValues insertCommand set =
    let commands =
        set
        |> Seq.chunkBySize 1000
        |> Seq.toList
        |> List.choose insertCommand
    executeNonQueryMulti commands id

let insertNewValues insertCommand (existValues:'T1 list) (values:'T2 list) contains =
    let set = HashSet(values)
    let existingSet = HashSet(existValues)
    set.RemoveWhere (fun value -> contains existingSet value) |> ignore
    insertValues insertCommand set

let insertNewTeams (teams:DBTeam list) =
    let existTeams = teamIDsSelectCommand |> getDBIntValues |> defArgr []
    insertNewValues toTeamsInsertCommand existTeams teams (fun existingSet (id, _) -> existingSet.Contains id)

let insertNewMatches (matches:DBMatch list) =
    let existMatches = matchesIDsSelectCommand |> getDBIntValues |> defArgr []
    insertNewValues toMatchesInsertCommand existMatches matches (fun existingSet (id, _, _, _, _) -> existingSet.Contains id)

let insertNewBets (bets:DBBet list) =
    let existBets = betsInfoSelectCommand |> getDBBetValues |> defArgr []
    insertNewValues toBetsInsertCommand existBets bets (fun existingSet (mid, bid, bp, bv, _) -> existingSet.Contains (mid, bid, bp, bv))

let toDBBet = function
    | P1 -> Some (1, None)
    | X  -> Some (2, None)
    | P2 -> Some (3, None)
    | D1X -> Some (4, None)
    | D12 -> Some (5, None)
    | DX2 -> Some (6, None)
    | TG param -> Some (9, Some param)
    | TL param -> Some (10, Some param)
    | IT1G param -> Some (11, Some param)
    | IT1L param -> Some (12, Some param)
    | IT2G param -> Some (13, Some param)
    | IT2L param -> Some (14, Some param)
    | _ -> None

let toDBMatch id (now:DateTime) games (teams, matches, bets) =
    let nowString = now.ToString(dateTimeFormat)
    let newBets =
        games
        |> List.collect (fun (_, bets) -> bets)
        |> List.choose (fun (betType, v) ->
            let dbBet = toDBBet betType
            dbBet |>> (fun (t, p) -> (t, p, v))
        )
        |> List.map (fun (t, p, v) -> (id, t, p, v, nowString))
        |> List.append bets
    (teams, matches, newBets)

let toLeagueResponse leagueID now matches response =
    matches
    |> List.fold(
        fun acc (id, ((team1ID, _) as team1), ((team2ID, _) as team2), time, games) ->
            let (teams, matches, bets) = acc
            let dbMatch = (id, leagueID, team1ID, team2ID, time.ToString())
            toDBMatch id now games (team1::team2::teams, dbMatch::matches, bets)
        ) response