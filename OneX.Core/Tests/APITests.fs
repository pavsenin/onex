namespace OneX.Core.Tests

#if Test
open NUnit.Framework
open OneX.Core
open System

[<TestFixture>]
type APITests() =
    [<Test>]
    member this.receiveBets() =
        let (teams, matches, bets) = API.receiveBets DateTime.Now
        ()
#endif