DROP TABLE IF EXISTS BetTypes
GO
CREATE TABLE BetTypes
(
  ID INT NOT NULL PRIMARY KEY,
  BetType VARCHAR(256) NOT NULL
)
GO
INSERT INTO BetTypes (ID, BetType) VALUES
  (1, 'Win1'), (2, 'Draw'), (3, 'Win2'), (4, 'Win1+Draw'), (5, 'Win1+Win2'), (6, 'Draw+Win2'),
  (9, 'TotalGreat'), (10, 'TotalLess'), (11, 'IndTotal1Great'), (12, 'IndTotal1Less'), (13, 'IndTotal2Great'), (14, 'IndTotal2Less')
GO
DROP TABLE IF EXISTS Leagues
GO
CREATE TABLE Leagues
(
  ID INT NOT NULL PRIMARY KEY,
  Name VARCHAR(256) NOT NULL
)
GO
INSERT INTO Leagues (ID, Name) VALUES
  (225733, 'Russia-Premier-League'), (88637, 'England-Premier-League'), (96463, 'Germany-Bundesliga'),
  (127733, 'Spain-Primera-Divisin'), (110163, 'Italy-Serie-A'), (12821, 'France-Ligue-1'),
  (118587, 'UEFA-Champions-League'), (118593, 'UEFA-Europa-League')
GO
DROP TABLE IF EXISTS Teams
GO
CREATE TABLE Teams
(
  ID INT NOT NULL PRIMARY KEY,
  Name VARCHAR(256) NOT NULL
)
GO
DROP TABLE IF EXISTS Matches
GO
CREATE TABLE Matches
(
  ID INT NOT NULL PRIMARY KEY,
  LeagueID INT NOT NULL,
  Team1ID INT NOT NULL,
  Team2ID INT NOT NULL,
  StartedAt DATETIME NOT NULL
)
GO
DROP TABLE IF EXISTS Bets
GO
CREATE TABLE Bets
(
  ID INT NOT NULL IDENTITY(1, 1) PRIMARY KEY,
  MatchID INT NOT NULL,
  BetTypeID INT NOT NULL,
  BetParam FLOAT NULL,
  Value FLOAT NOT NULL,
  ReceivedAt DATETIME NOT NULL
)
GO
-- INSERT INTO Teams
-- INSERT INTO Matches
-- INSERT INTO Bets

INSERT INTO Teams (ID, Name) VALUES
(1, 'Team1'), (2, 'Team2')

INSERT INTO Matches (ID, LeagueID, Team1ID, Team2ID, StartedAt) VALUES
(1, 2, 3, 4, '2018-03-31 11:30:00.000'), (2, 2, 3, 4, '2018-03-31 11:30:00.000') 

INSERT INTO Bets (MatchID, BetTypeID, BetParam, Value, ReceivedAt) VALUES
(1, 2, 3.0, 4.0, '2018-03-31 11:30:00.000'), (1, 2, 3.0, 4.0, '2018-03-31 11:30:00.000')
