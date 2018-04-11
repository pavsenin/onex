SELECT * FROM Bets
WHERE MatchID = 23035945 AND BetTypeID = 13 AND BetPAram = 5.5

SELECT l.Name, t1.Name, t1.ID, t2.Name, t2.ID, m.StartedAt FROM Matches m
INNER JOIN Leagues l ON m.LeagueID = l.ID
INNER JOIN Teams t1 ON m.Team1ID = t1.ID
INNER JOIN Teams t2 ON m.Team2ID = t2.ID
WHERE l.ID = 176125

select * from Leagues

SELECT * FROM MATCHES WHERE StartedAt < GETDATE()
SELECT * FROM Teams

WITH Grouped AS (SELECT MatchID, BetTypeID, BetParam, MAX(ReceivedAt) AS ReceivedAt FROM Bets GROUP BY MatchID, BetTypeID, BetParam)
SELECT b.MatchID, b.BetTypeID, b.BetParam, b.Value FROM Grouped g
INNER JOIN Bets b ON g.MatchID = b.MatchID AND g.BetTypeID = b.BetTypeID AND ISNULL(g.BetParam, 0) = ISNULL(b.BetParam, 0) AND g.ReceivedAt = b.ReceivedAt
INNER JOIN Matches m ON b.MatchID = m.ID
WHERE m.StartedAt > GETDATE()

WITH Grouped AS (SELECT MatchID, BetTypeID, BetParam, MIN(Value) AS Min, MAX(Value) AS Max FROM Bets GROUP BY MatchID, BetTypeID, BetParam)
SELECT b.MatchID, b.BetTypeID, b.BetParam, b.Value FROM Grouped g
INNER JOIN Bets b ON g.MatchID = b.MatchID AND g.BetTypeID = b.BetTypeID AND ISNULL(g.BetParam, 0) = ISNULL(b.BetParam, 0)
INNER JOIN Matches m ON b.MatchID = m.ID
WHERE b.MatchID = 24081997 AND b.BetTypeID = 14

WITH GroupedValues AS (SELECT MatchID, BetTypeID, BetParam, MIN(Value) AS Min, MAX(Value) AS Max FROM Bets GROUP BY MatchID, BetTypeID, BetParam),
GroupedTimes AS (SELECT MatchID, BetTypeID, BetParam, MIN(ReceivedAt) AS FirstTime, MAX(ReceivedAt) AS LastTime FROM Bets GROUP BY MatchID, BetTypeID, BetParam),
GroupedAll AS
(
SELECT l.Name AS League, t1.Name AS Team1, t2.Name AS Team2, m.StartedAt, gt.MatchID, bt.BetType, gt.BetTypeID, gt.BetParam, gv.Min, gv.Max, (gv.Max - gv.Min) * 100.0 / gv.Max AS Diff,
(SELECT Value FROM Bets b WHERE gt.MatchID = b.MatchID AND gt.BetTypeID = b.BetTypeID AND ISNULL(gt.BetParam, 0) = ISNULL(b.BetParam, 0) AND gt.FirstTime = b.ReceivedAt) as First,
(SELECT Value FROM Bets b WHERE gt.MatchID = b.MatchID AND gt.BetTypeID = b.BetTypeID AND ISNULL(gt.BetParam, 0) = ISNULL(b.BetParam, 0) AND gt.LastTime = b.ReceivedAt) as Last,
gt.FirstTime, gt.LastTime
FROM GroupedValues gv
INNER JOIN GroupedTimes gt ON gv.MatchID = gt.MatchID AND gv.BetTypeID = gt.BetTypeID AND ISNULL(gv.BetParam, 0) = ISNULL(gt.BetParam, 0)
INNER JOIN Matches m ON m.ID = gt.MatchID
INNER JOIN Leagues l ON m.LeagueID = l.ID
INNER JOIN Teams t1 ON m.Team1ID = t1.ID
INNER JOIN Teams t2 ON m.Team2ID = t2.ID
INNER JOIN BetTypes bt ON bt.ID = gt.BetTypeID
where l.ID = 176125
)
SELECT *, (Last - First) * 100.0 / Last AS LDiff FROM GroupedAll
ORDER BY LDiff, Diff

SELECT ID FROM Matches
WHERE LeagueID = 225733 AND Team1ID = 16919 AND Team2ID = 731729 AND CAST(StartedAt AS Date) = '2018-04-18' -- 22591255


SELECT l.Name, t1.Name, t2.Name, m.StartedAt, CONCAT(s.ScoreTeam1, ':', s.ScoreTeam2) AS Score FROM Matches m
INNER JOIN Leagues l ON m.LeagueID = l.ID
INNER JOIN Teams t1 ON m.Team1ID = t1.ID
INNER JOIN Teams t2 ON m.Team2ID = t2.ID
INNER JOIN Scores s ON s.MatchID = m.ID

--delete from scores


