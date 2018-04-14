namespace OneX.Core.Tests

open System
open OneX.Core.Onex
open OneX.Core.WebUtils
open FSharp.Data
open FSharp.Data.JsonExtensions
open Xunit
open OneX.Core.BenchmarkUtils

type OnexTests() =
    [<Fact>]
    member this.leaguesIsAvailable() =
        leagues
        |> List.iter (fun (_, id) ->
            let (content, time) =
                getExecutionTime (fun _ ->
                    id
                    |> buildLeagueUrl
                    |> fetchContentGet
                    |> JsonValue.Parse
                )

            Assert.True(content?Success.AsBoolean())
            Assert.Equal(content?Error.AsString(), "")
            Assert.Equal(content?ErrorCode.AsInteger(), 0)
            Assert.True(time < 1000L)
        )