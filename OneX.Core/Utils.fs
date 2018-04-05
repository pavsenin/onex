module Utils
open System

type Result<'TSuccess, 'TFailure> = Success of 'TSuccess | Failure of 'TFailure
let inline defArgr d v = match v with | Success s -> s | _ -> d

let inline (|>>) x f = x |> Option.map f
let inline (|?) def arg = defaultArg arg def
let inline map f d = function | Some v -> f v | None -> d
let inline defArg d v = defaultArg v d
let inline cond v t f = if v then t else f
let inline showTime title func =
    let watch = System.Diagnostics.Stopwatch()
    watch.Start()
    let result = func()
    watch.Stop()
    Console.WriteLine(title + ": " + watch.ElapsedMilliseconds.ToString())
    result