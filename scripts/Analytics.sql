SELECT * FROM Bets
WHERE MatchID = 23035945 AND BetTypeID = 13 AND BetPAram = 5.5

SELECT l.Name, t1.Name, t2.Name, m.StartedAt FROM Matches m
INNER JOIN Leagues l ON m.LeagueID = l.ID
INNER JOIN Teams t1 ON m.Team1ID = t1.ID
INNER JOIN Teams t2 ON m.Team2ID = t2.ID
WHERE m.ID = 23291781

SELECT * FROM MATCHES WHERE StartedAt > GETDATE()


WITH Grouped AS (SELECT MatchID, BetTypeID, BetParam, MAX(ReceivedAt) AS ReceivedAt FROM Bets GROUP BY MatchID, BetTypeID, BetParam)
SELECT b.MatchID, b.BetTypeID, b.BetParam, b.Value FROM Grouped g
INNER JOIN Bets b ON g.MatchID = b.MatchID AND g.BetTypeID = b.BetTypeID AND ISNULL(g.BetParam, 0) = ISNULL(b.BetParam, 0) AND g.ReceivedAt = b.ReceivedAt
INNER JOIN Matches m ON b.MatchID = m.ID
WHERE m.StartedAt > GETDATE()

WITH Grouped AS (SELECT MatchID, BetTypeID, BetParam, MIN(Value) AS Min, MAX(Value) AS Max FROM Bets GROUP BY MatchID, BetTypeID, BetParam)
SELECT b.MatchID, b.BetTypeID, b.BetParam, b.Value FROM Grouped g
INNER JOIN Bets b ON g.MatchID = b.MatchID AND g.BetTypeID = b.BetTypeID AND ISNULL(g.BetParam, 0) = ISNULL(b.BetParam, 0)
INNER JOIN Matches m ON b.MatchID = m.ID
WHERE b.MatchID = 23034897 AND b.BetTypeID = 14 AND b.BetPAram = 2

WITH GroupedValues AS (SELECT MatchID, BetTypeID, BetParam, MIN(Value) AS Min, MAX(Value) AS Max FROM Bets GROUP BY MatchID, BetTypeID, BetParam),
GroupedTimes AS (SELECT MatchID, BetTypeID, BetParam, MIN(ReceivedAt) AS FirstTime, MAX(ReceivedAt) AS LastTime FROM Bets GROUP BY MatchID, BetTypeID, BetParam)
SELECT l.Name, t1.Name, t2.Name, m.StartedAt, gt.MatchID, gt.BetTypeID, gt.BetParam, gv.Min, gv.Max, (gv.Max - gv.Min) * 100.0 / gv.Max AS Diff,
(SELECT Value FROM Bets b WHERE gt.MatchID = b.MatchID AND gt.BetTypeID = b.BetTypeID AND ISNULL(gt.BetParam, 0) = ISNULL(b.BetParam, 0) AND gt.FirstTime = b.ReceivedAt) as First,
(SELECT Value FROM Bets b WHERE gt.MatchID = b.MatchID AND gt.BetTypeID = b.BetTypeID AND ISNULL(gt.BetParam, 0) = ISNULL(b.BetParam, 0) AND gt.LastTime = b.ReceivedAt) as Last,
gt.FirstTime, gt.LastTime
FROM GroupedValues gv
INNER JOIN GroupedTimes gt ON gv.MatchID = gt.MatchID AND gv.BetTypeID = gt.BetTypeID AND ISNULL(gv.BetParam, 0) = ISNULL(gt.BetParam, 0)
INNER JOIN Matches m ON m.ID = gt.MatchID
INNER JOIN Leagues l ON m.LeagueID = l.ID
INNER JOIN Teams t1 ON m.Team1ID = t1.ID
INNER JOIN Teams t2 ON m.Team2ID = t2.ID
WHERE m.ID = 23040785
ORDER BY Diff DESC