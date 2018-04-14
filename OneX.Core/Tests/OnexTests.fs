namespace OneX.Core.Tests

open NUnit.Framework
open System
open OneX.Core.Onex
open OneX.Core.WebUtils
open FSharp.Data
open FSharp.Data.JsonExtensions

[<TestFixture>]
type OnexTests() =
    [<Test>]
    member this.leaguesIsAvailable() =
        leagues
        |> List.map (fun (_, id) ->
            id
            |> buildLeagueUrl
            |> fetchContentGet
            |> JsonValue.Parse
        )
        |> List.iter (fun content ->
            Assert.That(content?Success, Is.True)
        )