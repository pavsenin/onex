DROP TABLE IF EXISTS GameTypes
GO
CREATE TABLE GameTypes
(
  ID INT NOT NULL PRIMARY KEY,
  GameType VARCHAR(256) NOT NULL
)
GO
INSERT INTO GameTypes (ID, GameType) VALUES (1, 'Outcome'), (2, 'DoubleChance'), (4, 'Total'), (5, 'IndividualTotal1'), (6, 'IndividualTotal2')
GO
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
DROP TABLE IF EXISTS Teams
GO
CREATE TABLE Teams
(
  ID INT NOT NULL PRIMARY KEY,
  Name VARCHAR(256) NOT NULL
)
GO
-- INSERT INTO Teams
DROP TABLE IF EXISTS Matches
GO
CREATE TABLE Matches
(
  ID INT NOT NULL PRIMARY KEY,
  Team1ID INT NOT NULL,
  Team2ID INT NOT NULL,
  StartedAt DATETIME NOT NULL
)
GO
-- INSERT INTO Matches
DROP TABLE IF EXISTS Bets
GO
CREATE TABLE Bets
(
  ID INT NOT NULL IDENTITY(1, 1) PRIMARY KEY,
  MatchID INT NOT NULL,
  GameTypeID INT NOT NULL,
  BetTypeID INT NOT NULL,
  BetParam FLOAT NULL,
  Value FLOAT NOT NULL,
  ReceivedAt DATETIME NOT NULL
)
GO
-- INSERT INTO Bets