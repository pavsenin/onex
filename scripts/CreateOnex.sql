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
    (118587, 'UEFA-Champions-League'), (118593, 'UEFA-Europa-League'), (225733, 'Russia-Premier-League'),
    (88637, 'England-Premier-League'), (96463, 'Germany-Bundesliga'), (127733, 'Spain-Primera-Divisin'),
    (110163, 'Italy-Serie-A'), (12821, 'France-Ligue-1'), (1536237, 'FIFA-World-Cup-2018'),
    (108319, 'England-FA-Cup'), (119235, 'Germany-DFB-Pokal'), (119243, 'Spain-Copa-del-Rey'),
    (127759, 'Coppa-Italia'), (119241, 'Coupe-de-France'), (176125, 'Russian-Cup'),
    (105759, 'England-Championship'), (13709, 'England-League-One'), (24637, 'England-League-Two'),
    (26031, 'Austria-Bundesliga'), (28787, 'Belgium-Jupiler-League'), (30037, 'Bulgaria-A-PFG'),
    (109313, 'Germany-2-Bundesliga'), (8777, 'Greece-SuperLeague'), (8773, 'Denmark-Superliga'),
    (27687, 'Spain-Segunda-Division'), (7067, 'Italy-Serie-B'), (27731, 'Poland-Ekstraklasa'),
    (118663, 'Portugal-Portuguese-Liga'), (118585, 'Russian-Championship-FNL'), (11121, 'Romania-Liga-1'),
    (30035, 'Serbia-SuperLiga'), (11113, 'Turkey-SuperLiga'), (29949, 'Ukraine-Premier-League'),
    (12829, 'France-Ligue-2'), (27707, 'Czech-Republic-Gambrinus-Liga'), (27735, 'Croatia-1-HNL'),
    (27695, 'Switzerland-SuperLeague'), (13521, 'Scotland-Premier-League'), (212425, 'Sweden-Allsvenskan'),
    (119575, 'Netherlands-Eredivisie'), (12505, 'Cyprus-First-Division'), (41199, 'Israel-Ligat-haAl'),
    (33021, 'Kazakhstan-Premier-League'), (1015483, 'Belarus-Premier-League'),

    (119599, 'Argentina-Primera-Division'), (1268397, 'Brazil-Campeonato-Brasileiro'), (120507, 'Mexico-Primera-Division'),
    (214147, 'Colombia-Categora-Primera-A'), (55479, 'Paraguay-Primera-Division'), (120503, 'Peru-Primera-Division'),
    (828065, 'USA-MLS'), (52183, 'Uruguay-Primera-Division'),

    (104509, 'Australia-A-League'), (32887, 'Iran-Pro-League'), (58043, 'China-Super-League'),
    (30467, 'South-Korea-K-League-Classic'), (118737, 'Japan-J-League')
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
DROP TABLE IF EXISTS Scores
GO
CREATE TABLE Scores
(
  MatchID INT NOT NULL PRIMARY KEY,
  ScoreTypeID INT NOT NULL,
  ScoreTeam1 INT NOT NULL,
  ScoreTeam2 INT NOT NULL
)
GO