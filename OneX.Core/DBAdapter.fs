module DBAdapter

open System.Data.SqlClient
open Utils
open Domain
open System.Collections.Generic
open System
open System.Linq

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

let toScoreInsertValue (mid, stype, st1, st2) =
    sprintf "(%d, %d, %d, %d)" mid stype st1 st2

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

let toScoresInsertCommand scores =
    toInsertCommand scores toScoreInsertValue "INSERT INTO Scores (MatchID, ScoreTypeID, ScoreTeam1, ScoreTeam2) VALUES "

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
let matchesInfoSelectCommand =
    "SELECT ID, LeagueID, Team1ID, Team2ID, StartedAt FROM Matches"
let matchIDsFromScoresSelectCommand =
    "SELECT MatchID FROM Scores"

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
let getDBMatchValues command =
    executeReader command (fun reader ->
        seq {
            while reader.Read() do
                let id = reader.GetInt32(0)
                let lid = reader.GetInt32(1)
                let t1id = reader.GetInt32(2)
                let t2id = reader.GetInt32(3)
                let started = reader.GetDateTime(4)
                yield (id, lid, t1id, t2id, started)
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
    insertNewValues toBetsInsertCommand existBets bets (fun existingSet (mid, bid, bp, bv, _) -> existingSet.Contains((mid, bid, bp, bv)))

let insertNewScores scores =
    let existScores = matchIDsFromScoresSelectCommand |> getDBIntValues |> defArgr []
    let matches = matchesInfoSelectCommand |> getDBMatchValues |> defArgr []
    let keySelector = Func<_, _>(fun (_, lid, t1id, t2id, (started:DateTime)) -> (lid, t1id, t2id, started.ToString(dateFormat)))
    let elemSelector = Func<_, _>(fun (id, _, _, _, _) -> id)
    let matchesDict = matches.ToDictionary(keySelector, elemSelector)
    let dbScores = scores |> List.choose (fun (lid, t1id, t2id, time, ((s1, s2), _)) ->
        let date = DateTime.Parse(time).ToString(dateFormat)
        match matchesDict.TryGetValue((lid, t1id, t2id, date)) with
        | false, _ -> None
        | true, mid -> Some(mid, 1, s1, s2)
    )
    insertNewValues toScoresInsertCommand existScores dbScores (fun existingSet (mid, _, _, _) -> existingSet.Contains mid)

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


(*
1536237-FIFA-World-Cup-2018
108319-England-FA-Cup
119235-Germany-DFB-Pokal
119243-Spain-Copa-del-Rey
127759-Coppa-Italia
119241-Coupe-de-France
176125-Russian-Cup

105759-England-Championship
13709-England-League-One
24637-England-League-Two
26031-Austria-Bundesliga
28787-Belgium-Jupiler-League
30037-Bulgaria-A-PFG
109313-Germany-2-Bundesliga
8777-Greece-SuperLeague
8773-Denmark-Superliga
27687-Spain-Segunda-Division
7067-Italy-Serie-B
27731-Poland-Ekstraklasa
118663-Portugal-Portuguese-Liga
118585-Russian-Championship-FNL
11121-Romania-Liga-1
30035-Serbia-SuperLiga
11113-Turkey-SuperLiga
29949-Ukraine-Premier-League
12829-France-Ligue-2
27707-Czech-Republic-Gambrinus-Liga
27735-Croatia-1-HNL
27695-Switzerland-SuperLeague
13521-Scotland-Premier-League
212425-Sweden-Allsvenskan

Голландия
Кипр
Израиль
Беларусь
Казахстан



119599-Argentina-Primera-Division
1268397-Brazil-Campeonato-Brasileiro
120507-Mexico-Primera-Division
214147-Colombia-Categora-Primera-A
55479-Paraguay-Primera-Division
120503-Peru-Primera-Division
828065-USA-MLS
52183-Uruguay-Primera-Division


104509-Australia-A-League
32887-Iran-Pro-League
58043-China-Super-League
30467-South-Korea-K-League-Classic
118737-Japan-J-League
*)