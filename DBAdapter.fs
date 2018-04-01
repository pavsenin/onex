module DBAdapter

open FSharp.Configuration
open System.Data.SqlClient
open Utils
open Domain

type Settings = AppSettings<"App.config">

let connectionString = Settings.ConnectionStrings.ConnectionString

let execute command exec =
    use conn = new SqlConnection(connectionString)
    let cmd = new SqlCommand(command, conn)
    cmd.CommandTimeout <- 0
    try
        try
            conn.Open()
            exec cmd
        with
            _ -> None
    finally
        conn.Close()

let executeNonQuery command =
    execute command (fun cmd -> Some (cmd.ExecuteNonQuery()))

let executeReader command =
    execute command (fun cmd -> Some (cmd.ExecuteReader()))

let toTeamInsertValue (id, name) =
    sprintf "(%d, '%s')" id name

let toMatchInsertValue (id, leagueID, team1ID, team2ID, time) =
    sprintf "(%d, %d, %d, %d, '%s')" id leagueID team1ID team2ID time

let toBetInsertValue (matchID, betID, param, value, received) =
    let paramString = param |>> (fun p -> p.ToString()) |> defArg "NULL"
    sprintf "(%d, %d, %s, %f, '%s')" matchID betID paramString value received

let toInsertCommand vs map start =
    let values = vs |> Set.map map
    if values.IsEmpty then None
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

type DBTeam = int * string
type DBMatch = int * int * int * int * string
type DBBet = int * int * float option * float * string
type LeagueResponse = DBTeam list * DBMatch list * DBBet list

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

let toDBMatch id now games (teams, matches, bets) =
    let newBets =
        games
        |> List.collect (fun (_, bets) -> bets)
        |> List.choose (fun (betType, v) ->
            let dbBet = toDBBet betType
            dbBet |>> (fun (t, p) -> (t, p, v))
        )
        |> List.map (fun (t, p, v) -> (id, t, p, v, now))
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