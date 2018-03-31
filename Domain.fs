module Domain

type BetType =
    | UnBet
    | P1 | X | P2 
    | D1X | D12 | DX2
    | TG of float
    | TL of float
    | IT1G of float
    | IT1L of float
    | IT2G of float
    | IT2L of float

type GameType =
    | UnGame
    | X12 | DX12
    | Total | IndTotal1 | IndTotal2